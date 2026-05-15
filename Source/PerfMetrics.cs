using System;

#if CAMERAPLUS_PERF
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Verse;
#endif

namespace CameraPlus
{
	static class PerfMetrics
	{
#if CAMERAPLUS_PERF
		const int flushFrameInterval = 120;
		static readonly object gate = new();
		static readonly Dictionary<string, SectionStats> sections = [];
		static readonly Dictionary<string, long> counters = [];
		static readonly Dictionary<string, SampleStats> samples = [];
		static string filePath;
		static bool failed;
		static int nextFlushFrame = -1;

		sealed class SectionStats
		{
			public long calls;
			public long totalTicks;
			public long maxTicks;
		}

		sealed class SampleStats
		{
			public long count;
			public long total;
			public long latest;
			public long max;
		}

		public readonly struct Scope : IDisposable
		{
			readonly string name;
			readonly long startTicks;

			public Scope(string name)
			{
				this.name = name;
				startTicks = Stopwatch.GetTimestamp();
			}

			public void Dispose()
			{
				RecordSection(name, Stopwatch.GetTimestamp() - startTicks);
			}
		}

		public static Scope Measure(string name) => new(name);

		public static void Count(string name, long delta = 1)
		{
			lock (gate)
			{
				counters.TryGetValue(name, out var current);
				counters[name] = current + delta;
			}
		}

		public static void Sample(string name, long value)
		{
			lock (gate)
			{
				if (samples.TryGetValue(name, out var sample) == false)
				{
					sample = new SampleStats();
					samples[name] = sample;
				}

				sample.count++;
				sample.total += value;
				sample.latest = value;
				if (sample.count == 1 || value > sample.max)
					sample.max = value;
			}
		}

		public static void FlushIfNeeded()
		{
			EnsureInitialized();
			if (filePath == null || failed)
				return;

			var frame = Time.frameCount;
			if (frame < nextFlushFrame)
				return;

			nextFlushFrame = frame + flushFrameInterval;
			Flush(frame);
		}

		static void RecordSection(string name, long elapsedTicks)
		{
			lock (gate)
			{
				if (sections.TryGetValue(name, out var section) == false)
				{
					section = new SectionStats();
					sections[name] = section;
				}

				section.calls++;
				section.totalTicks += elapsedTicks;
				if (elapsedTicks > section.maxTicks)
					section.maxTicks = elapsedTicks;
			}
		}

		static void EnsureInitialized()
		{
			if (filePath != null || failed)
				return;

			try
			{
				var directory = Path.Combine(GenFilePaths.ConfigFolderPath, "CameraPlusPerf");
				Directory.CreateDirectory(directory);
				filePath = Path.Combine(directory, $"cameraplus-perf-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
				File.WriteAllText(filePath, "utc,frame,kind,name,count,total_ms,avg_us,max_ms,latest,max\n");
				nextFlushFrame = Time.frameCount + flushFrameInterval;
				Log.Message($"CameraPlusPerf: writing metrics to {filePath}");
			}
			catch (Exception ex)
			{
				failed = true;
				Log.Warning($"CameraPlusPerf: metrics disabled after initialization failure: {ex}");
			}
		}

		static void Flush(int frame)
		{
			try
			{
				var utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
				var builder = new StringBuilder();

				lock (gate)
				{
					foreach (var pair in sections)
					{
						var section = pair.Value;
						var totalMs = TicksToMilliseconds(section.totalTicks);
						var avgUs = section.calls == 0 ? 0 : totalMs * 1000 / section.calls;
						var maxMs = TicksToMilliseconds(section.maxTicks);
						AppendRow(builder, utc, frame, "section", pair.Key, section.calls, totalMs, avgUs, maxMs, "", "");
					}

					foreach (var pair in counters)
						AppendRow(builder, utc, frame, "counter", pair.Key, pair.Value, 0, 0, 0, pair.Value.ToString(CultureInfo.InvariantCulture), "");

					foreach (var pair in samples)
					{
						var sample = pair.Value;
						var average = sample.count == 0 ? 0 : (double)sample.total / sample.count;
						AppendRow(builder, utc, frame, "sample", pair.Key, sample.count, sample.total, average, sample.max, sample.latest.ToString(CultureInfo.InvariantCulture), sample.max.ToString(CultureInfo.InvariantCulture));
					}
				}

				File.AppendAllText(filePath, builder.ToString());
			}
			catch (Exception ex)
			{
				failed = true;
				Log.Warning($"CameraPlusPerf: metrics disabled after flush failure: {ex}");
			}
		}

		static double TicksToMilliseconds(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

		static void AppendRow(StringBuilder builder, string utc, int frame, string kind, string name, long count, double totalMs, double avgUs, double maxMs, string latest, string max)
		{
			builder
				.Append(utc).Append(',')
				.Append(frame).Append(',')
				.Append(kind).Append(',')
				.Append(name).Append(',')
				.Append(count.ToString(CultureInfo.InvariantCulture)).Append(',')
				.Append(totalMs.ToString("F6", CultureInfo.InvariantCulture)).Append(',')
				.Append(avgUs.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
				.Append(maxMs.ToString("F6", CultureInfo.InvariantCulture)).Append(',')
				.Append(latest).Append(',')
				.Append(max).AppendLine();
		}
#else
		public readonly struct Scope : IDisposable
		{
			public void Dispose()
			{
			}
		}

		public static Scope Measure(string name) => default;
		public static void Count(string name, long delta = 1)
		{
		}

		public static void Sample(string name, long value)
		{
		}

		public static void FlushIfNeeded()
		{
		}
#endif
	}
}
