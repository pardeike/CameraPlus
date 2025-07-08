using HarmonyLib;
using RimWorld.Planet;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	[HarmonyPatch(typeof(World), nameof(World.FinalizeInit))]
	static class World_FinalizeInit_Patch
	{
		public static void Postfix(World __instance)
		{
			CameraSettings.settings = __instance.GetComponent<CameraSettings>();
		}
	}

	public class CameraSettings(World world) : WorldComponent(world)
	{
		public List<DotConfig> dotConfigs = [.. defaultConfig];

		public static CameraSettings settings;
		public static List<DotConfig> defaultConfig = [];
		public static readonly List<DotConfig> defaultDefaultConfig = [
			new()
			{
				conditions = [new AnimalTag(), new TameTag() { _negated = true }],
				lineColor = new(0, 1, 1),
				fillColor = Color.clear,
				lineSelectedColor = new(0, 0.5f, 0.5f),
				fillSelectedColor = new(0, 1, 1, 0.5f),
				useEdge = false,
				mouseReveals = false,
			},
			new()
			{
				conditions = [new AnimalTag()],
				lineColor = Color.black,
				fillColor = Color.clear,
				lineSelectedColor = Color.white,
				fillSelectedColor = Color.white,
				mouseReveals = false,
				outlineFactor = 0.05f,
			}
		];

		public static void InitDefaultDefaults()
		{
			var filePath = Tools.DefaultRulesFilePath;
			if (File.Exists(filePath) == false)
				Tools.SaveDotConfigs(filePath, defaultDefaultConfig);
			defaultConfig = Tools.LoadDotConfigs(filePath);
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref dotConfigs, "dotConfigs", LookMode.Deep);
			dotConfigs ??= [.. defaultConfig];
		}
	}
}