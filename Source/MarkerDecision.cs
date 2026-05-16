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

		MarkerDecision(
			Pawn pawn,
			DotConfig dotConfig,
			bool hidden,
			bool defaultShow,
			bool edgeEnabled,
			bool drawInside,
			bool suppressVanilla,
			bool hasMarkerColors,
			DotStyle mode)
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
			if (pawn == null || Tools.IsHiddenFromPlayer(pawn))
				return new MarkerDecision(pawn, dotConfig, true, false, false, false, false, false, dotConfig?.mode ?? Settings.dotStyle);

			var mode = dotConfig?.mode ?? Settings.dotStyle;
			var isAnimal = pawn.RaceProps.Animal;
			var isNamedAnimal = isAnimal && pawn.Name != null;
			var defaultShow = true;
			if (isAnimal)
			{
				defaultShow = Settings.customNameStyle != LabelStyle.HideAnimals;
				if (Settings.includeNotTamedAnimals == false && pawn.Name == null && dotConfig == null)
					defaultShow = false;
			}

			var cellSize = FastUI.CurUICellSize;
			var showBelowPixels = dotConfig?.showBelowPixels ?? Settings.dotSize;
			if (showBelowPixels == -1)
				showBelowPixels = Settings.dotSize;

			var mouseReveals = dotConfig?.mouseReveals ?? Settings.mouseOverShowsLabels;
			var mouseSuppressesMarker = mouseReveals && Tools.MouseDistanceSquared(pawn.DrawPos, true) <= 2.25f;

			var suppressVanilla = ShouldSuppressVanilla(pawn, dotConfig, mode, cellSize, showBelowPixels, mouseSuppressesMarker, isAnimal, isNamedAnimal);
			var drawInside = mode > DotStyle.VanillaDefault
				&& (dotConfig?.useInside ?? true)
				&& defaultShow
				&& cellSize <= showBelowPixels
				&& mouseSuppressesMarker == false;
			var edgeEnabled = dotConfig?.useEdge ?? (Settings.edgeIndicators && defaultShow);
			var hasMarkerColors = HasMarkerColors(dotConfig, isNamedAnimal);

			return new MarkerDecision(pawn, dotConfig, false, defaultShow, edgeEnabled, drawInside, suppressVanilla, hasMarkerColors, mode);
		}

		static bool ShouldSuppressVanilla(Pawn pawn, DotConfig dotConfig, DotStyle mode, float cellSize, int showBelowPixels, bool mouseSuppressesMarker, bool isAnimal, bool isNamedAnimal)
		{
			if (dotConfig != null)
			{
				if (mode == DotStyle.VanillaDefault)
					return false;

				if (cellSize > showBelowPixels)
					return false;

				if (mouseSuppressesMarker)
					return false;

				return dotConfig.useInside;
			}

			if (Settings.dotStyle == DotStyle.VanillaDefault)
				return false;

			if (cellSize > Settings.dotSize)
				return false;

			if (Settings.customNameStyle == LabelStyle.HideAnimals && isAnimal)
				return false;

			if (mouseSuppressesMarker)
				return false;

			return Settings.includeNotTamedAnimals || isAnimal == false || isNamedAnimal;
		}

		static bool HasMarkerColors(DotConfig dotConfig, bool isNamedAnimal)
		{
			if (dotConfig != null)
				return true;

			return isNamedAnimal == false || Settings.customNameStyle != LabelStyle.HideAnimals;
		}
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
	}
}
