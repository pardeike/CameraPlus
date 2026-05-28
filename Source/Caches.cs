using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	class Caches
	{
		public static readonly Dictionary<string, Color> cachedMainColors = [];
		public static readonly Dictionary<Type, CameraDelegates> cachedCameraDelegates = [];
		static readonly object markerStateClearLock = new();
		static bool markerStateClearQueued;

		public static readonly QuotaCache<Pawn, int, DotConfig> dotConfigCache
			= new(60, pawn => pawn.thingIDNumber, pawn => pawn.GetDotConfig());

		public static readonly QuotaCache<Pawn, int, bool> shouldShowLabelCache
			= new(60, pawn => pawn.thingIDNumber, pawn =>
			{
				var len = FastUI.CurUICellSize;
				if (len <= Settings.hidePawnLabelBelow)
					return false;

				if (Tools.IsHiddenFromPlayer(pawn))
					return false;

				if (pawn != null && Settings.customNameStyle == LabelStyle.HideAnimals && pawn.RaceProps.Animal)
					return true;

				if (pawn != null && len <= Settings.dotSize)
					return false;

				return true;
			});

		public static void ClearMarkerState()
		{
			if (UnityData.IsInMainThread)
			{
				ClearMarkerStateNow();
				return;
			}

			lock (markerStateClearLock)
			{
				if (markerStateClearQueued)
					return;
				markerStateClearQueued = true;
			}

			// World.FinalizeInit can run on RimWorld's async long-event thread.
			// MarkerCache owns Unity materials/textures, so destruction must wait
			// until LongEventHandler returns to the main Unity thread.
			LongEventHandler.ExecuteWhenFinished(ClearQueuedMarkerState);
		}

		static void ClearQueuedMarkerState()
		{
			lock (markerStateClearLock)
				markerStateClearQueued = false;
			ClearMarkerState();
		}

		static void ClearMarkerStateNow()
		{
			cachedMainColors.Clear();
			dotConfigCache.Clear();
			shouldShowLabelCache.Clear();
			MarkerDecisionCache.Clear();
			MarkerCache.Clear();
		}

		public static CameraDelegates GetCachedCameraDelegate(Pawn pawn)
		{
			using var measure = PerfMetrics.Measure("Caches.GetCachedCameraDelegate");
			var type = pawn.GetType();
			if (cachedCameraDelegates.TryGetValue(type, out var result) == false)
			{
				PerfMetrics.Count("camera_delegate.cache_misses");
				result = new CameraDelegates(pawn);
				cachedCameraDelegates[type] = result;
			}
			return result;
		}
	}
}
