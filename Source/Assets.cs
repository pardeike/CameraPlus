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

		private static Material borderedMaterial;
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

			borderedMaterial = assets.LoadAsset<Material>("Silhouette");
			borderedShader = assets.LoadAsset<Shader>("Bordered");

			initialized = true;
		}

		public static Material BorderedMaterial => borderedMaterial;
		public static Shader BorderedShader => borderedShader;
	}
}