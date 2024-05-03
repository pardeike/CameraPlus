using HarmonyLib;
using System.IO;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	[HarmonyPatch]
	[StaticConstructorOnStartup]
	public static class Assets
	{
		public static bool initialized = false;
		private static Material colorBedMaterial, huesMaterial;
		private static Shader borderedShader;

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