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

		public static CameraDelegates GetCachedCameraDelegate(Pawn pawn)
		{
			var type = pawn.GetType();
			if (cachedCameraDelegates.TryGetValue(type, out var result) == false)
			{
				result = new CameraDelegates(pawn);
				cachedCameraDelegates[type] = result;
			}
			return result;
		}
	}
}
