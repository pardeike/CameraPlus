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
		public static readonly Dictionary<Pawn, Color> cachedPawnMainColors = [];
		public static readonly Dictionary<Type, CameraDelegates> cachedCameraDelegates = [];
		static readonly object markerStateClearLock = new();
		static bool markerStateClearQueued;

		public static readonly QuotaCache<Pawn, int, DotConfig> dotConfigCache
			= new(60, pawn => pawn.thingIDNumber, pawn => pawn.GetDotConfig());

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
			cachedPawnMainColors.Clear();
			dotConfigCache.Clear();
			MarkerDecisionCache.Clear();
			MarkerCache.Clear();
		}

		public static void RemovePawnMainColor(Pawn pawn)
		{
			if (pawn != null)
				cachedPawnMainColors.Remove(pawn);
		}

		public static void ClearPawnMainColors()
			=> cachedPawnMainColors.Clear();

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
