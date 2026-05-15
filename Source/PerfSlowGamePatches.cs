#if CAMERAPLUS_PERF
using HarmonyLib;
using RimWorld;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	static class PerfSlowGameConfig
	{
		const int reloadFrameInterval = 120;
		const int maxDelayMicroseconds = 100000;
		static readonly char[] separator = ['='];
		static SlowSettings settings;
		static string filePath;
		static bool loggedPath;
		static bool failed;
		static int nextReloadFrame = -1;

		struct SlowSettings
		{
			public int dynamicDrawUs;
			public int pawnRenderInternalUs;
			public int pawnGuiOverlayUs;
			public int pawnLabelUs;
		}

		public static void DelayDynamicDraw() => Delay("dynamic_draw", Current.dynamicDrawUs);
		public static void DelayPawnRenderInternal() => Delay("pawn_render_internal", Current.pawnRenderInternalUs);
		public static void DelayPawnGuiOverlay() => Delay("pawn_gui_overlay", Current.pawnGuiOverlayUs);
		public static void DelayPawnLabel() => Delay("pawn_label", Current.pawnLabelUs);

		static SlowSettings Current
		{
			get
			{
				ReloadIfNeeded();
				return settings;
			}
		}

		static void ReloadIfNeeded()
		{
			if (failed)
				return;

			var frame = Time.frameCount;
			if (frame < nextReloadFrame)
				return;

			nextReloadFrame = frame + reloadFrameInterval;
			try
			{
				var directory = Path.Combine(GenFilePaths.ConfigFolderPath, "CameraPlusPerf");
				Directory.CreateDirectory(directory);
				filePath ??= Path.Combine(directory, "slow-core.txt");

				if (loggedPath == false)
				{
					Log.Message($"CameraPlusPerf: slow-core config at {filePath}");
					loggedPath = true;
				}

				var loaded = new SlowSettings();
				if (File.Exists(filePath))
				{
					foreach (var line in File.ReadAllLines(filePath))
						ApplyLine(ref loaded, line);
				}

				settings = loaded;
			}
			catch (Exception ex)
			{
				failed = true;
				Log.Warning($"CameraPlusPerf: slow-core simulation disabled after config failure: {ex}");
			}
		}

		static void ApplyLine(ref SlowSettings loaded, string line)
		{
			var commentStart = line.IndexOf('#');
			if (commentStart >= 0)
				line = line.Substring(0, commentStart);

			line = line.Trim();
			if (line.Length == 0)
				return;

			var parts = line.Split(separator, 2);
			if (parts.Length != 2)
				return;

			var key = parts[0].Trim();
			if (int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) == false)
				return;

			value = Math.Max(0, Math.Min(maxDelayMicroseconds, value));
			switch (key)
			{
				case "dynamicDrawUs":
					loaded.dynamicDrawUs = value;
					break;
				case "pawnRenderInternalUs":
					loaded.pawnRenderInternalUs = value;
					break;
				case "pawnGuiOverlayUs":
					loaded.pawnGuiOverlayUs = value;
					break;
				case "pawnLabelUs":
					loaded.pawnLabelUs = value;
					break;
			}
		}

		static void Delay(string metricName, int microseconds)
		{
			if (microseconds <= 0)
				return;

			PerfMetrics.Count($"slow_core.{metricName}.calls");
			PerfMetrics.Sample($"slow_core.{metricName}.configured_us", microseconds);

			var targetTicks = microseconds * Stopwatch.Frequency / 1000000L;
			if (targetTicks <= 0)
				return;

			var startTicks = Stopwatch.GetTimestamp();
			while (Stopwatch.GetTimestamp() - startTicks < targetTicks)
			{
			}
		}
	}

	[HarmonyPatch(typeof(DynamicDrawManager), nameof(DynamicDrawManager.DrawDynamicThings))]
	static class PerfSlow_DynamicDrawManager_DrawDynamicThings_Patch
	{
		[HarmonyPriority(Priority.Last)]
		public static bool Prefix()
		{
			PerfSlowGameConfig.DelayDynamicDraw();
			return true;
		}
	}

	[HarmonyPatch]
	static class PerfSlow_PawnRenderer_RenderPawnInternal_Patch
	{
		public static MethodBase TargetMethod() => AccessTools.Method(typeof(PawnRenderer), "RenderPawnInternal", [typeof(PawnDrawParms)]);

		[HarmonyPriority(Priority.Last)]
		public static bool Prefix()
		{
			PerfSlowGameConfig.DelayPawnRenderInternal();
			return true;
		}
	}

	[HarmonyPatch(typeof(PawnUIOverlay), nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
	static class PerfSlow_PawnUIOverlay_DrawPawnGUIOverlay_Patch
	{
		[HarmonyPriority(Priority.Last)]
		public static bool Prefix()
		{
			PerfSlowGameConfig.DelayPawnGuiOverlay();
			return true;
		}
	}

	[HarmonyPatch(typeof(GenMapUI), nameof(GenMapUI.DrawPawnLabel))]
	[HarmonyPatch([typeof(Pawn), typeof(Vector2), typeof(float), typeof(float), typeof(System.Collections.Generic.Dictionary<string, string>), typeof(GameFont), typeof(bool), typeof(bool)])]
	static class PerfSlow_GenMapUI_DrawPawnLabel_Patch
	{
		[HarmonyPriority(Priority.Last)]
		public static bool Prefix()
		{
			PerfSlowGameConfig.DelayPawnLabel();
			return true;
		}
	}
}
#endif
