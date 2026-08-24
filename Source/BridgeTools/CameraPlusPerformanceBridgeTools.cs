using HarmonyLib;
using RimBridgeServer.Sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace CameraPlus.BridgeTools
{
	public sealed class CameraPlusPerformanceBridgeTools
	{
		const string performanceHarmonyId = "net.pardeike.rimworld.mod.camera+.performance-fixture";
		static readonly SemaphoreSlim benchmarkGate = new(1, 1);
		static readonly string[] speeds = ["Normal", "Fast", "Superfast", "Ultrafast"];

		sealed class BenchmarkStageException(string stage, string message) : Exception(message)
		{
			public readonly string stage = stage;
		}

		[Tool(
			"cameraplus/run_performance_benchmark",
			Description = "Compare CameraPlus marker rendering with vanilla pawn rendering at RimWorld speed levels 1 through 4 under a repeatable fluctuating tick load, using Dubs Performance Analyzer.",
			ResultDescription = "Returns paired TPS and DPA samples for CameraPlus and its vanilla-rendering bypass at Normal, Fast, Superfast, and Ultrafast speeds.")]
		public static async Task<object> RunPerformanceBenchmark(
			IRimBridgeContext ctx,
			CancellationToken cancellationToken,
			[ToolParameter(Description = "Save name without .rws.", DefaultValue = "CameraPlusPerf_962Pawns_EdgeDots")] string saveName = "CameraPlusPerf_962Pawns_EdgeDots",
			[ToolParameter(Description = "Real-time duration of each measured sample in milliseconds.", DefaultValue = 4000)] int durationMs = 4000,
			[ToolParameter(Description = "Warm-up duration before each measured sample in milliseconds.", DefaultValue = 1000)] int warmupMs = 1000,
			[ToolParameter(Description = "Minimum synthetic CPU delay per game tick in microseconds.", DefaultValue = 500)] int minimumTickDelayUs = 500,
			[ToolParameter(Description = "Maximum synthetic CPU delay per game tick in microseconds.", DefaultValue = 2500)] int maximumTickDelayUs = 2500,
			[ToolParameter(Description = "Length of one deterministic low-to-high-to-low load wave in game ticks.", DefaultValue = 300)] int wavePeriodTicks = 300)
		{
			if (string.IsNullOrWhiteSpace(saveName))
				return new { success = false, stage = "validate", error = "saveName is required." };
			if (durationMs < 500 || durationMs > 30000)
				return new { success = false, stage = "validate", error = "durationMs must be between 500 and 30000." };
			if (warmupMs < 0 || warmupMs > 10000)
				return new { success = false, stage = "validate", error = "warmupMs must be between 0 and 10000." };
			if (minimumTickDelayUs < 0 || minimumTickDelayUs > 20000 || maximumTickDelayUs < minimumTickDelayUs || maximumTickDelayUs > 20000)
				return new { success = false, stage = "validate", error = "Tick delays must satisfy 0 <= minimum <= maximum <= 20000 microseconds." };
			if (wavePeriodTicks < 2 || wavePeriodTicks > 60000)
				return new { success = false, stage = "validate", error = "wavePeriodTicks must be between 2 and 60000." };

			await benchmarkGate.WaitAsync(cancellationToken);
			var originalSkipCustomRendering = CameraPlusMain.skipCustomRendering;
			var stage = "initialize";
			try
			{
				await RequireCallAsync(ctx, cancellationToken, "dpa.status", "rimworld/dpa_status", new { includePresets = false });
				await TryCallAsync(ctx, CancellationToken.None, "rimworld/dpa_cleanup");
				await ctx.MainThread.InvokeAsync(SyntheticTickLoad.Install, cancellationToken);
				await RequireCallAsync(ctx, cancellationToken, "dpa.patch", "rimworld/dpa_patch_methods", new
				{
					category = "Update",
					inputMode = "Method",
					targets = "Verse.Root_Play:Update;Verse.DynamicDrawManager:DrawDynamicThings;CameraPlus.DotDrawer:DrawDots;Verse.TickManager:DoSingleTick",
					initialize = true,
					resetAfterPatch = true,
					previewLimit = 4
				});

				var results = new List<object>();
				for (var speedIndex = 0; speedIndex < speeds.Length; speedIndex++)
				{
					var speed = speeds[speedIndex];
					stage = $"{speed.ToLowerInvariant()}.pair";
					var cameraFirst = (speedIndex & 1) != 0;
					object cameraPlus;
					object vanilla;

					if (cameraFirst)
					{
						cameraPlus = await RunSampleAsync(ctx, cancellationToken, saveName, speed, true, durationMs, warmupMs, minimumTickDelayUs, maximumTickDelayUs, wavePeriodTicks);
						vanilla = await RunSampleAsync(ctx, cancellationToken, saveName, speed, false, durationMs, warmupMs, minimumTickDelayUs, maximumTickDelayUs, wavePeriodTicks);
					}
					else
					{
						vanilla = await RunSampleAsync(ctx, cancellationToken, saveName, speed, false, durationMs, warmupMs, minimumTickDelayUs, maximumTickDelayUs, wavePeriodTicks);
						cameraPlus = await RunSampleAsync(ctx, cancellationToken, saveName, speed, true, durationMs, warmupMs, minimumTickDelayUs, maximumTickDelayUs, wavePeriodTicks);
					}

					results.Add(new { speedLevel = speedIndex + 1, speed, cameraPlus, vanillaRendering = vanilla });
				}

				stage = "complete";
				return new
				{
					success = true,
					stage,
					modVersion = typeof(CameraPlusMain).Assembly.GetName().Version?.ToString() ?? string.Empty,
					saveName,
					pawnCount = Find.CurrentMap?.mapPawns?.AllPawnsSpawned?.Count ?? 0,
					load = new { minimumTickDelayUs, maximumTickDelayUs, wavePeriodTicks },
					durationMs,
					warmupMs,
					results
				};
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (BenchmarkStageException exception)
			{
				return new { success = false, stage = exception.stage, error = exception.Message };
			}
			catch (Exception exception)
			{
				return new { success = false, stage, error = $"{exception.GetType().Name}: {exception.Message}" };
			}
			finally
			{
				await TryCallAsync(ctx, CancellationToken.None, "rimworld/dpa_cleanup");
				try
				{
					await ctx.MainThread.InvokeAsync(() =>
					{
						SyntheticTickLoad.Uninstall();
						CameraPlusMain.skipCustomRendering = originalSkipCustomRendering;
					}, CancellationToken.None);
				}
				finally
				{
					benchmarkGate.Release();
				}
			}
		}

		static async Task<object> RunSampleAsync(
			IRimBridgeContext ctx,
			CancellationToken cancellationToken,
			string saveName,
			string speed,
			bool cameraPlusRendering,
			int durationMs,
			int warmupMs,
			int minimumTickDelayUs,
			int maximumTickDelayUs,
			int wavePeriodTicks)
		{
			var prefix = $"{speed.ToLowerInvariant()}.{(cameraPlusRendering ? "cameraplus" : "vanilla")}";
			await RequireCallAsync(ctx, cancellationToken, $"{prefix}.load", "rimworld/load_game_ready", new
			{
				saveName,
				readiness = "visual",
				pauseIfNeeded = true,
				timeoutMs = 120000
			});

			await ctx.MainThread.InvokeAsync(() =>
			{
				CameraPlusMain.skipCustomRendering = cameraPlusRendering == false;
				SyntheticTickLoad.Configure(minimumTickDelayUs, maximumTickDelayUs, wavePeriodTicks);
			}, cancellationToken);

			if (warmupMs > 0)
				await RequireCallAsync(ctx, cancellationToken, $"{prefix}.warmup", "rimworld/play_for", new
				{
					durationMs = warmupMs,
					speed,
					forceRequestedSpeed = true,
					pollIntervalMs = 25
				});

			await RequireCallAsync(ctx, cancellationToken, $"{prefix}.reset", "rimworld/dpa_reset");
			await ctx.MainThread.InvokeAsync(SyntheticTickLoad.RestartWave, cancellationToken);
			var play = await RequireCallAsync(ctx, cancellationToken, $"{prefix}.play", "rimworld/play_for", new
			{
				durationMs,
				speed,
				forceRequestedSpeed = true,
				pollIntervalMs = 25
			});
			var snapshot = await RequireCallAsync(ctx, cancellationToken, $"{prefix}.snapshot", "rimworld/dpa_snapshot", new
			{
				sortBy = "total",
				limit = 12
			});

			var advancedTicks = play.ReadResult<int>("advancedTicks");
			var elapsedMs = play.ReadResult<int>("elapsedMs");
			return new
			{
				cameraPlusRendering,
				advancedTicks,
				elapsedMs,
				ticksPerSecond = elapsedMs <= 0 ? 0d : advancedTicks * 1000d / elapsedMs,
				dpa = snapshot.Result
			};
		}

		static async Task<RimBridgeToolCallResult<object>> RequireCallAsync(
			IRimBridgeContext ctx,
			CancellationToken cancellationToken,
			string stage,
			string tool,
			object arguments = null)
		{
			var call = await ctx.Tools.CallAsync(tool, arguments, cancellationToken: cancellationToken);
			if (call.Succeeded())
				return call;

			var message = call?.Error == null
				? $"{tool} returned status '{call?.Status ?? "unknown"}'."
				: $"{call.Error.Code}: {call.Error.Message}";
			throw new BenchmarkStageException(stage, message);
		}

		static async Task TryCallAsync(IRimBridgeContext ctx, CancellationToken cancellationToken, string tool)
		{
			try
			{
				await ctx.Tools.CallAsync(tool, cancellationToken: cancellationToken);
			}
			catch
			{
			}
		}

		static class SyntheticTickLoad
		{
			static readonly MethodBase tickMethod = AccessTools.Method(typeof(TickManager), nameof(TickManager.DoSingleTick));
			static Harmony harmony;
			static bool enabled;
			static int minimumDelayUs;
			static int maximumDelayUs;
			static int periodTicks;
			static int originTick;

			public static void Install()
			{
				if (harmony != null)
					return;
				if (tickMethod == null)
					throw new MissingMethodException(typeof(TickManager).FullName, nameof(TickManager.DoSingleTick));

				harmony = new Harmony(performanceHarmonyId);
				harmony.Patch(tickMethod, postfix: new HarmonyMethod(typeof(SyntheticTickLoad), nameof(Postfix)));
			}

			public static void Configure(int minimumTickDelayUs, int maximumTickDelayUs, int wavePeriodTicks)
			{
				minimumDelayUs = minimumTickDelayUs;
				maximumDelayUs = maximumTickDelayUs;
				periodTicks = wavePeriodTicks;
				enabled = maximumDelayUs > 0;
				RestartWave();
			}

			public static void RestartWave()
			{
				originTick = Find.TickManager?.TicksGame ?? 0;
			}

			public static void Uninstall()
			{
				enabled = false;
				if (harmony == null)
					return;
				harmony.Unpatch(tickMethod, HarmonyPatchType.Postfix, harmony.Id);
				harmony = null;
			}

			static void Postfix()
			{
				if (enabled == false)
					return;

				var tick = Find.TickManager?.TicksGame ?? originTick;
				var phase = Math.Abs(tick - originTick) % periodTicks;
				var halfPeriod = periodTicks / 2d;
				var ratio = phase <= halfPeriod
					? phase / halfPeriod
					: (periodTicks - phase) / halfPeriod;
				var delayUs = minimumDelayUs + (int)Math.Round((maximumDelayUs - minimumDelayUs) * ratio);
				if (delayUs <= 0)
					return;

				var targetTicks = delayUs * Stopwatch.Frequency / 1_000_000L;
				var startTicks = Stopwatch.GetTimestamp();
				while (Stopwatch.GetTimestamp() - startTicks < targetTicks)
					Thread.SpinWait(16);
			}
		}
	}
}
