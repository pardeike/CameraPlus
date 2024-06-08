using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	[HarmonyPatch]
	[StaticConstructorOnStartup]
	public static class Assets
	{
		public static string CameraPlusFolderPath => GenFilePaths.FolderUnderSaveData("CameraPlus");
		static FileSystemWatcher watcher;

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
		public static readonly Texture2D[] steppers = [ContentFinder<Texture2D>.Get("StepperUp", true), ContentFinder<Texture2D>.Get("StepperDown", true)];
		public static readonly Texture2D bracket = ContentFinder<Texture2D>.Get("Bracket", true);
		public static readonly Texture2D columnHeaderPreview = ContentFinder<Texture2D>.Get("ColumnHeaderPreview", true);
		public static readonly Texture2D columnHeader = ContentFinder<Texture2D>.Get("ColumnHeader", true);
		public static readonly Texture2D columnHeaderSelected = ContentFinder<Texture2D>.Get("ColumnHeaderSelected", true);
		public static readonly Texture2D deleteTagButton = ContentFinder<Texture2D>.Get("TagDelete", true);
		public static readonly Texture2D deleteColorButton = ContentFinder<Texture2D>.Get("DeleteColorButton", true);
		public static readonly Texture2D valueChangerMouseAttachment = ContentFinder<Texture2D>.Get("ValueChanger", true);
		public static readonly Texture2D rowDragMouseAttachment = ContentFinder<Texture2D>.Get("RowDrag", true);
		public static readonly Texture2D colorDragMouseAttachment = ContentFinder<Texture2D>.Get("ColorDrag", true);
		public static readonly Dictionary<string, Texture2D> customMarkers = [];

		public static Material previewMaterial;

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

			previewMaterial = MaterialAllocator.Create(borderedShader);
			previewMaterial.SetTexture("_MainTex", outerColonistTexture);
			previewMaterial.renderQueue = (int)RenderQueue.Overlay;

			var newestVersion = new CameraPlusSettings().currentVersion;
			if (Settings.currentVersion < newestVersion)
			{
				Find.WindowStack.Add(new Dialog_NewVersion());
				Settings.currentVersion = newestVersion;
				LongEventHandler.ExecuteWhenFinished(Settings.Write);
			}

			LoadCustomMarkers();

			watcher = new()
			{
				Path = CameraPlusFolderPath,
				Filter = "*.png",
				NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
			};
			watcher = new(CameraPlusFolderPath, "*.png");
			watcher.Created += (_, _) => ScheduleLoadCustomMarkers();
			watcher.Changed += (_, _) => ScheduleLoadCustomMarkers();
			watcher.Renamed += (_, _) => ScheduleLoadCustomMarkers();
			watcher.Deleted += (_, _) => ScheduleLoadCustomMarkers();
			watcher.EnableRaisingEvents = true;

			initialized = true;
		}

		static void ScheduleLoadCustomMarkers() => LongEventHandler.QueueLongEvent(LoadCustomMarkers, "Loading custom markers", false, null);

		static void LoadCustomMarkers()
		{
			var directoryInfo = new DirectoryInfo(CameraPlusFolderPath);
			if (!directoryInfo.Exists)
				directoryInfo.Create();
			customMarkers.Clear();
			var items = directoryInfo.GetFiles().Where(f => f.Extension.ToLower() == ".png");
			foreach (var item in items)
			{
				var texture = new Texture2D(2, 2);
				texture.LoadImage(File.ReadAllBytes(item.FullName));
				customMarkers[item.Name] = texture;
			}
		}

		public static Material ColorBedMaterial => colorBedMaterial;
		public static Material HuesMaterial => huesMaterial;
		public static Shader BorderedShader => borderedShader;
	}
}