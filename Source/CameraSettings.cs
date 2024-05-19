using HarmonyLib;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	[HarmonyPatch(typeof(World))]
	[HarmonyPatch(nameof(World.FinalizeInit))]
	static class World_FinalizeInit_Patch
	{
		public static void Postfix(World __instance)
		{
			CameraSettings.settings = __instance.GetComponent<CameraSettings>();
		}
	}

	public class CameraSettings(World world) : WorldComponent(world)
	{
		public static CameraSettings settings;
		public List<DotConfig> dotConfigs = [.. defaultConfig];

		static readonly List<DotConfig> defaultConfig = [
			new DotConfig()
			{
				conditions = [new ColonistTag()]
			},
			new DotConfig()
			{
				conditions = [new ColonistTag(), new DraftedTag()],
				lineColor = new Color(0, 0.5f, 0),
				lineSelectedColor = new Color(0, 1, 0),
			},
			new DotConfig()
			{
				conditions = [new ColonistTag(), new MentalTag()],
				fillColor = new Color(1, 0.75f, 0),
				fillSelectedColor = new Color(1, 0.75f, 0),
			},
			new DotConfig()
			{
				conditions = [new ColonistTag(), new DownedTag()],
				lineColor = new Color(0.5f, 0, 0),
				lineSelectedColor = new Color(1, 0, 0),
			}
		];

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref dotConfigs, "dotConfigs", LookMode.Deep);
			dotConfigs ??= [.. defaultConfig];
		}
	}
}