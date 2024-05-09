﻿using HarmonyLib;
using System.IO;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	[HarmonyPatch]
	[StaticConstructorOnStartup]
	public static class Assets
	{
		public static readonly Texture2D dummyTexture = new(1, 1);
		public static readonly Texture2D innerColonistTexture = ContentFinder<Texture2D>.Get("InnerColonistMarker", true);
		public static readonly Texture2D outerColonistTexture = ContentFinder<Texture2D>.Get("OuterColonistMarker", true);
		public static readonly Texture2D innerAnimalTexture = ContentFinder<Texture2D>.Get("InnerAnimalMarker", true);
		public static readonly Texture2D outerAnimalTexture = ContentFinder<Texture2D>.Get("OuterAnimalMarker", true);
		public static readonly Texture2D innerEntityTexture = ContentFinder<Texture2D>.Get("InnerEntityMarker", true);
		public static readonly Texture2D outerEntityTexture = ContentFinder<Texture2D>.Get("OuterEntityMarker", true);
		public static readonly Texture2D colorMarkerTexture = ContentFinder<Texture2D>.Get("ColorMarker", true);
		public static readonly Texture2D colorBackgroundPattern = ContentFinder<Texture2D>.Get("ColorBackgroundPattern", true);
		public static readonly Texture2D editoBackgroundPattern = ContentFinder<Texture2D>.Get("EditorBackgroundPattern", true);
		public static readonly Texture2D swatchBackgroundPattern = ContentFinder<Texture2D>.Get("SwatchBackgroundPattern", true);
		public static readonly Texture2D deleteColorButton = ContentFinder<Texture2D>.Get("DeleteColorButton", true);

		static bool initialized = false;
		static Material colorBedMaterial, huesMaterial;
		static Shader borderedShader;

		[HarmonyPatch(typeof(UIRoot_Entry), nameof(UIRoot_Entry.Init))]
		[HarmonyPostfix]
		public static void LoadAssetBundle()
		{
			if (initialized)
				return;

			var arch = "Win64";
			var platform = Application.platform;
			if (platform == RuntimePlatform.LinuxEditor || platform == RuntimePlatform.LinuxPlayer)
				arch = "Linux";
			if (platform == RuntimePlatform.OSXEditor || platform == RuntimePlatform.OSXPlayer)
				arch = "MacOS";

			var me = LoadedModManager.GetMod<CameraPlusMain>();
			var path = Path.Combine(me.Content.RootDir, "Resources", arch, "effects");
			var assets = AssetBundle.LoadFromFile(path);

			colorBedMaterial = assets.LoadAsset<Material>("ColorBed");
			if (colorBedMaterial == null)
				Log.Error("Cannot load ColorBed material from asset bundle.");

			huesMaterial = assets.LoadAsset<Material>("Hues");
			if (huesMaterial == null)
				Log.Error("Cannot load Hues material from asset bundle.");

			borderedShader = assets.LoadAsset<Shader>("Bordered");
			if (borderedShader == null)
				Log.Error("Cannot load Bordered shader from asset bundle.");

			initialized = true;
		}

		public static Material ColorBedMaterial => colorBedMaterial;
		public static Material HuesMaterial => huesMaterial;
		public static Shader BorderedShader => borderedShader;
	}
}