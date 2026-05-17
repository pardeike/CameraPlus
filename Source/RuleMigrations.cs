using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CameraPlus
{
	static class RuleMigrations
	{
		public static bool MigrateKnownDefaults(List<DotConfig> dotConfigs)
		{
			if (dotConfigs == null)
				return false;

			var changed = false;
			foreach (var dotConfig in dotConfigs)
				if (IsLegacyBroadAnimalRule(dotConfig))
				{
					dotConfig.fillSelectedColor = Color.clear;
					changed = true;
				}
			return changed;
		}

		static bool IsLegacyBroadAnimalRule(DotConfig dotConfig)
		{
			if (dotConfig == null)
				return false;

			return HasOnlyAnimalCondition(dotConfig)
				&& dotConfig.mode == DotStyle.BetterSilhouettes
				&& dotConfig.useInside
				&& dotConfig.useEdge
				&& dotConfig.showBelowPixels == -1
				&& dotConfig.mouseReveals == false
				&& SameColor(dotConfig.lineColor, Color.black)
				&& SameColor(dotConfig.fillColor, Color.clear)
				&& SameColor(dotConfig.lineSelectedColor, Color.white)
				&& SameColor(dotConfig.fillSelectedColor, Color.white);
		}

		static bool HasOnlyAnimalCondition(DotConfig dotConfig)
			=> dotConfig.conditions?.Count == 1
				&& dotConfig.conditions.Single() is AnimalTag animalTag
				&& animalTag.Negated == false;

		static bool SameColor(Color left, Color right)
		{
			var left32 = (Color32)left;
			var right32 = (Color32)right;
			return left32.r == right32.r
				&& left32.g == right32.g
				&& left32.b == right32.b
				&& left32.a == right32.a;
		}
	}
}
