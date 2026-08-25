using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	readonly struct MarkerDecision
	{
		public readonly Thing markerTarget;
		public readonly Vector3 drawPos;
		public readonly DotConfig dotConfig;
		public readonly bool hidden;
		public readonly bool defaultShow;
		public readonly bool edgeEnabled;
		public readonly bool drawInside;
		public readonly bool canDrawInsideMarker;
		public readonly bool suppressVanilla;
		public readonly bool revealLabel;
		public readonly bool hasMarkerColors;
		public readonly DotStyle mode;

		MarkerDecision(
			Thing markerTarget,
			Vector3 drawPos,
			DotConfig dotConfig,
			bool hidden,
			bool defaultShow,
			bool edgeEnabled,
			bool drawInside,
			bool canDrawInsideMarker,
			bool suppressVanilla,
			bool revealLabel,
			bool hasMarkerColors,
			DotStyle mode)
		{
			this.markerTarget = markerTarget;
			this.drawPos = drawPos;
			this.dotConfig = dotConfig;
			this.hidden = hidden;
			this.defaultShow = defaultShow;
			this.edgeEnabled = edgeEnabled;
			this.drawInside = drawInside;
			this.canDrawInsideMarker = canDrawInsideMarker;
			this.suppressVanilla = suppressVanilla;
			this.revealLabel = revealLabel;
			this.hasMarkerColors = hasMarkerColors;
			this.mode = mode;
		}

		public static MarkerDecision For(Pawn pawn, DotConfig dotConfig)
			=> For(pawn, dotConfig, Tools.MarkerTargetFor(pawn));

		internal static MarkerDecision For(Pawn pawn, DotConfig dotConfig, Thing markerTarget)
		{
			var animalPolicy = AnimalMarkerPolicy.For(pawn);
			var settings = Settings;
			var mode = DotConfig.NormalizeMode(dotConfig?.mode ?? settings?.dotStyle ?? DotStyle.VanillaDefault);
			var drawPos = markerTarget?.DrawPos ?? default;

			if (pawn == null || markerTarget == null || Tools.IsHiddenFromPlayer(pawn))
				return new MarkerDecision(markerTarget, drawPos, dotConfig, true, false, false, false, false, false, false, false, mode);

			var defaultShow = animalPolicy.included;

			var cellSize = FastUI.CurUICellSize;
			var showBelowPixels = dotConfig?.showBelowPixels ?? settings?.dotSize ?? 0;
			if (showBelowPixels == -1)
				showBelowPixels = settings?.dotSize ?? 0;

			var mouseReveals = dotConfig?.mouseReveals ?? settings?.mouseOverShowsLabels ?? false;
			var revealLabel = mouseReveals && Tools.MouseDistanceSquared(drawPos, true) <= 2.25f;

			var drawInside = mode > DotStyle.VanillaDefault
				&& (dotConfig?.useInside ?? true)
				&& defaultShow
				&& cellSize <= showBelowPixels
				&& revealLabel == false;
			var hasMarkerColors = defaultShow;
			var canDrawInsideMarker = drawInside && CanDrawInsideMarker(mode, dotConfig);
			var suppressVanilla = canDrawInsideMarker && hasMarkerColors;
			var edgeEnabled = defaultShow
				&& mode != DotStyle.Off
				&& (dotConfig?.useEdge ?? settings?.edgeIndicators ?? false);

			return new MarkerDecision(markerTarget, drawPos, dotConfig, false, defaultShow, edgeEnabled, drawInside, canDrawInsideMarker, suppressVanilla, revealLabel, hasMarkerColors, mode);
		}

		static bool CanDrawInsideMarker(DotStyle mode, DotConfig dotConfig)
		{
			if (Assets.BorderedShader == null)
				return false;

			if (mode != DotStyle.Custom)
				return true;

			return HasCustomMarker(dotConfig);
		}

		static bool HasCustomMarker(DotConfig dotConfig)
			=> dotConfig != null
			&& string.IsNullOrEmpty(dotConfig.customDotStyle) == false
			&& Assets.customMarkers.TryGetValue(dotConfig.customDotStyle, out var texture)
			&& texture != null;
	}

	static class MarkerDecisionCache
	{
		static readonly Dictionary<Pawn, MarkerDecision> cache = [];
		static int frame = -1;

		public static MarkerDecision Get(Pawn pawn)
		{
			if (pawn == null)
				return MarkerDecision.For(null, null);

			RefreshFrame();
			var markerTarget = Tools.MarkerTargetFor(pawn);
			if (cache.TryGetValue(pawn, out var decision) && object.ReferenceEquals(decision.markerTarget, markerTarget))
			{
				PerfMetrics.Count("marker_decision.cache_hits");
				return decision;
			}

			PerfMetrics.Count("marker_decision.cache_misses");
			decision = MarkerDecision.For(pawn, Caches.dotConfigCache.Get(pawn), markerTarget);
			cache[pawn] = decision;
			return decision;
		}

		public static MarkerDecision Get(Pawn pawn, DotConfig dotConfig)
		{
			if (pawn == null)
				return MarkerDecision.For(null, dotConfig);

			if (dotConfig == null)
				return Get(pawn);

			RefreshFrame();
			var markerTarget = Tools.MarkerTargetFor(pawn);
			if (cache.TryGetValue(pawn, out var decision)
				&& object.ReferenceEquals(decision.markerTarget, markerTarget)
				&& object.ReferenceEquals(decision.dotConfig, dotConfig))
			{
				PerfMetrics.Count("marker_decision.cache_hits");
				return decision;
			}

			PerfMetrics.Count("marker_decision.cache_misses");
			decision = MarkerDecision.For(pawn, dotConfig, markerTarget);
			// Do not let a caller-supplied override contaminate the shared per-frame
			// decision used by the vanilla rendering patches.
			if (object.ReferenceEquals(Caches.dotConfigCache.Get(pawn), dotConfig))
				cache[pawn] = decision;
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
