using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	readonly struct MarkerDecision
	{
		public readonly Pawn pawn;
		public readonly DotConfig dotConfig;
		public readonly bool hidden;
		public readonly bool defaultShow;
		public readonly bool edgeEnabled;
		public readonly bool drawInside;
		public readonly bool suppressVanilla;
		public readonly bool hasMarkerColors;
		public readonly DotStyle mode;
		public readonly AnimalMarkerPolicy animalPolicy;

		MarkerDecision(
			Pawn pawn,
			DotConfig dotConfig,
			bool hidden,
			bool defaultShow,
			bool edgeEnabled,
			bool drawInside,
			bool suppressVanilla,
			bool hasMarkerColors,
			DotStyle mode,
			AnimalMarkerPolicy animalPolicy)
		{
			this.pawn = pawn;
			this.dotConfig = dotConfig;
			this.hidden = hidden;
			this.defaultShow = defaultShow;
			this.edgeEnabled = edgeEnabled;
			this.drawInside = drawInside;
			this.suppressVanilla = suppressVanilla;
			this.hasMarkerColors = hasMarkerColors;
			this.mode = mode;
			this.animalPolicy = animalPolicy;
		}

		public bool canDrawInsideMarker
		{
			get
			{
				if (drawInside == false)
					return false;

				if (mode != DotStyle.Custom)
					return true;

				return dotConfig != null
					&& dotConfig.customDotStyle != null
					&& Assets.customMarkers.ContainsKey(dotConfig.customDotStyle);
			}
		}

		public static MarkerDecision For(Pawn pawn, DotConfig dotConfig)
		{
			var animalPolicy = AnimalMarkerPolicy.For(pawn);
			var mode = dotConfig?.mode ?? Settings.dotStyle;

			if (pawn == null || Tools.IsHiddenFromPlayer(pawn))
				return new MarkerDecision(pawn, dotConfig, true, false, false, false, false, false, mode, animalPolicy);

			var defaultShow = animalPolicy.included;

			var cellSize = FastUI.CurUICellSize;
			var showBelowPixels = dotConfig?.showBelowPixels ?? Settings.dotSize;
			if (showBelowPixels == -1)
				showBelowPixels = Settings.dotSize;

			var mouseReveals = dotConfig?.mouseReveals ?? Settings.mouseOverShowsLabels;
			var mouseSuppressesMarker = mouseReveals && Tools.MouseDistanceSquared(pawn.DrawPos, true) <= 2.25f;

			var drawInside = mode > DotStyle.VanillaDefault
				&& (dotConfig?.useInside ?? true)
				&& defaultShow
				&& cellSize <= showBelowPixels
				&& mouseSuppressesMarker == false;
			var hasMarkerColors = HasMarkerColors(animalPolicy);
			var suppressVanilla = drawInside && hasMarkerColors && CanDrawInsideMarker(mode, dotConfig);
			var edgeEnabled = defaultShow
				&& mode != DotStyle.Off
				&& (dotConfig?.useEdge ?? Settings.edgeIndicators);

			return new MarkerDecision(pawn, dotConfig, false, defaultShow, edgeEnabled, drawInside, suppressVanilla, hasMarkerColors, mode, animalPolicy);
		}

		static bool CanDrawInsideMarker(DotStyle mode, DotConfig dotConfig)
		{
			if (mode != DotStyle.Custom)
				return true;

			return dotConfig != null
				&& dotConfig.customDotStyle != null
				&& Assets.customMarkers.ContainsKey(dotConfig.customDotStyle);
		}

		static bool HasMarkerColors(AnimalMarkerPolicy animalPolicy)
			=> animalPolicy.included;
	}

	static class MarkerDecisionCache
	{
		static readonly Dictionary<int, MarkerDecision> cache = [];
		static int frame = -1;

		public static MarkerDecision Get(Pawn pawn)
		{
			if (pawn == null)
				return MarkerDecision.For(null, null);

			RefreshFrame();
			var key = pawn.thingIDNumber;
			if (cache.TryGetValue(key, out var decision))
			{
				PerfMetrics.Count("marker_decision.cache_hits");
				return decision;
			}

			PerfMetrics.Count("marker_decision.cache_misses");
			decision = MarkerDecision.For(pawn, Caches.dotConfigCache.Get(pawn));
			cache[key] = decision;
			return decision;
		}

		public static MarkerDecision Get(Pawn pawn, DotConfig dotConfig)
		{
			if (pawn == null)
				return MarkerDecision.For(null, dotConfig);

			if (dotConfig == null)
				return Get(pawn);

			RefreshFrame();
			var key = pawn.thingIDNumber;
			if (cache.TryGetValue(key, out var decision))
			{
				PerfMetrics.Count("marker_decision.cache_hits");
				return decision;
			}

			PerfMetrics.Count("marker_decision.cache_misses");
			decision = MarkerDecision.For(pawn, dotConfig);
			cache[key] = decision;
			return decision;
		}

		static void RefreshFrame()
		{
			var currentFrame = Time.frameCount;
			if (frame == currentFrame)
				return;

			frame = currentFrame;
			cache.Clear();
		}

		public static void Clear()
		{
			frame = -1;
			cache.Clear();
		}
	}
}
