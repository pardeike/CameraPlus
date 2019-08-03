using Harmony;
using System;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public enum LabelStyle
	{
		IncludeAnimals = 0,
		AnimalsDifferent = 1,
		HideAnimals = 2
	}

	public class SavedViews : MapComponent
	{
		public RememberedCameraPos[] views = new RememberedCameraPos[9];

		public SavedViews(Map map) : base(map)
		{
		}

		public override void ExposeData()
		{
			for (var i = 0; i < 9; i++)
				Scribe_Deep.Look(ref views[i], "view" + (i + 1), new object[] { map });
		}
	}

	public class CameraPlusSettings : ModSettings
	{
		public float zoomedOutPercent = 65;
		public float zoomedInPercent = 1;
		public int exponentiality = 1;
		public float zoomedOutDollyPercent = 1;
		public float zoomedInDollyPercent = 1;
		public float zoomedOutDollyFrictionPercent = 0.15f;
		public float zoomedInDollyFrictionPercent = 0.15f;
		public bool stickyMiddleMouse = false;
		public bool zoomToMouse = true;
		public float soundNearness = 0;
		public bool hideNamesWhenZoomedOut = true;
		public int dotSize = 9;
		public LabelStyle customNameStyle = LabelStyle.AnimalsDifferent;
		public KeyCode cameraSettingsMod1 = KeyCode.LeftShift;
		public KeyCode cameraSettingsMod2 = KeyCode.None;
		public KeyCode cameraSettingsOption = KeyCode.LeftAlt;
		public KeyCode cameraSettingsKey = KeyCode.Tab;

		public static float minRootResult = 2;
		public static float maxRootResult = 130;

		public static readonly float minRootInput = 11;
		public static readonly float maxRootInput = 60;

		public static readonly float minRootOutput = 15;
		public static readonly float maxRootOutput = 65;

		public static readonly float nearestHeight = 32;
		public static readonly float farOutHeight = 256;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref zoomedOutPercent, "zoomedOutPercent", 65);
			Scribe_Values.Look(ref zoomedInPercent, "zoomedInPercent", 1);
			Scribe_Values.Look(ref exponentiality, "exponentiality", 1);
			Scribe_Values.Look(ref zoomedOutDollyPercent, "zoomedOutDollyPercent", 1);
			Scribe_Values.Look(ref zoomedInDollyPercent, "zoomedInDollyPercent", 1);
			Scribe_Values.Look(ref zoomedOutDollyFrictionPercent, "zoomedOutDollySpeedDecayPercent", 0.15f);
			Scribe_Values.Look(ref zoomedInDollyFrictionPercent, "zoomedInDollySpeedDecayPercent", 0.15f);
			Scribe_Values.Look(ref stickyMiddleMouse, "stickyMiddleMouse", false);
			Scribe_Values.Look(ref zoomToMouse, "zoomToMouse", true);
			Scribe_Values.Look(ref soundNearness, "soundNearness", 0);
			Scribe_Values.Look(ref hideNamesWhenZoomedOut, "hideNamesWhenZoomedOut", true);
			Scribe_Values.Look(ref dotSize, "dotSize", 9);
			Scribe_Values.Look(ref customNameStyle, "customNameStyle", LabelStyle.AnimalsDifferent);
			Scribe_Values.Look(ref cameraSettingsMod1, "cameraSettingsMod1", KeyCode.LeftShift);
			Scribe_Values.Look(ref cameraSettingsMod2, "cameraSettingsMod2", KeyCode.None);
			Scribe_Values.Look(ref cameraSettingsOption, "cameraSettingsOption", KeyCode.LeftAlt);
			Scribe_Values.Look(ref cameraSettingsKey, "cameraSettingsKey", KeyCode.Tab);

			if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
			{
				minRootResult = zoomedInPercent * 2;
				maxRootResult = zoomedOutPercent * 2;
			}
		}

		public void DoWindowContents(Rect inRect)
		{
			float previous;
			Rect rect;
			var map = Current.Game?.CurrentMap;
			const float buttonWidth = 90f;

			var list = new Listing_Standard { ColumnWidth = (inRect.width - 34f) / 2f };
			list.Begin(inRect);

			list.Gap(16f);

			list.Label("Zoom".Translate());

			previous = zoomedInPercent;
			list.Slider(ref zoomedInPercent, 0.1f, 20f, () => "Near".Translate() + ": " + Math.Round(zoomedInPercent, 1) + "%");
			minRootResult = zoomedInPercent * 2;
			if (previous != zoomedInPercent && map != null)
			{
				var val = Traverse.Create(Find.CameraDriver).Field("rootSize").GetValue<float>();
				if (val != minRootInput)
					Find.CameraDriver.SetRootPosAndSize(map.rememberedCameraPos.rootPos, minRootInput);
			}

			previous = zoomedOutPercent;
			list.Slider(ref zoomedOutPercent, 50f, 100f, () => "Far".Translate() + ": " + Math.Round(zoomedOutPercent, 1) + "%");
			maxRootResult = zoomedOutPercent * 2;
			if (previous != zoomedOutPercent && map != null)
			{
				var val = Traverse.Create(Find.CameraDriver).Field("rootSize").GetValue<float>();
				if (val != maxRootInput)
					Find.CameraDriver.SetRootPosAndSize(map.rememberedCameraPos.rootPos, maxRootInput);
			}

			list.Gap(12f);

			list.Label("Exponentiality".Translate());
			if (list.RadioButton("Off", exponentiality == 0, 8f)) exponentiality = 0;
			for (var i = 1; i <= 3; i++)
				if (list.RadioButton(i + "x", exponentiality == i, 8f)) exponentiality = i;

			list.Gap(16f);

			list.Label("SoundNearness".Translate() + ": " + Math.Round(soundNearness * 100, 1) + "%");
			list.Slider(ref soundNearness, 0f, 1f, null);

			list.Gap(6f);

			list.Label("CameraKeys".Translate());
			list.Gap(6f);

			rect = list.GetRect(28f);
			GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
			Widgets.Label(rect, "ModifierKeys".Translate());
			GenUI.ResetLabelAlign();
			rect.xMin = rect.xMax - buttonWidth;
			Tools.KeySettingsButton(rect, false, cameraSettingsMod1, code => cameraSettingsMod1 = code);
			rect.xMin -= buttonWidth + 12f;
			rect.xMax = rect.xMin + buttonWidth;
			Tools.KeySettingsButton(rect, false, cameraSettingsMod2, code => cameraSettingsMod2 = code);
			list.Gap(6f);

			rect = list.GetRect(28f);
			GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
			Widgets.Label(rect, "SaveModifier".Translate());
			GenUI.ResetLabelAlign();
			rect.xMin = rect.xMax - buttonWidth;
			Tools.KeySettingsButton(rect, false, cameraSettingsOption, code => cameraSettingsOption = code);
			list.Gap(6f);

			rect = list.GetRect(28f);
			GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
			Widgets.Label(rect, "SettingsKey".Translate());
			GenUI.ResetLabelAlign();
			rect.xMin = rect.xMax - buttonWidth;
			Tools.KeySettingsButton(rect, true, cameraSettingsKey, code => cameraSettingsKey = code);

			list.NewColumn();
			list.Gap(16f);

			list.Label("DollyPercentLabel".Translate());
			list.Slider(ref zoomedInDollyPercent, 0f, 4f, () => "Near".Translate() + ": " + Math.Round(zoomedInDollyPercent * 100, 1) + "%");
			list.Slider(ref zoomedOutDollyPercent, 0f, 4f, () => "Far".Translate() + ": " + Math.Round(zoomedOutDollyPercent * 100, 1) + " % ");

			list.Gap(12f);

			list.Label("DollyFrictionLabel".Translate());
			list.Slider(ref zoomedInDollyFrictionPercent, 0f, 1f, () => "Near".Translate() + ": " + Math.Round(zoomedInDollyFrictionPercent * 100, 1) + "%");
			list.Slider(ref zoomedOutDollyFrictionPercent, 0f, 1f, () => "Far".Translate() + ": " + Math.Round(zoomedOutDollyFrictionPercent * 100, 1) + "%");
			list.Gap(-2);
			list.CheckboxLabeled("StickyMiddleMouseDragging".Translate(), ref stickyMiddleMouse);

			list.Gap(24f);

			list.CheckboxLabeled("HideNamesWhenZoomedOut".Translate(), ref hideNamesWhenZoomedOut);
			if (hideNamesWhenZoomedOut)
			{
				list.Slider(ref dotSize, 1, 32, () => "Marker".Translate() + ": " + dotSize + " " + "Pixel".Translate());
				foreach (var label in Enum.GetNames(typeof(LabelStyle)))
				{
					var val = (LabelStyle)Enum.Parse(typeof(LabelStyle), label);
					if (list.RadioButton(label.Translate(), customNameStyle == val, 8f)) customNameStyle = val;
				}
			}

			list.Gap(24f);

			list.CheckboxLabeled("ZoomToMouse".Translate(), ref zoomToMouse);

			list.Gap(28f);

			if (list.ButtonText("RestoreToDefaultSettings".Translate()))
			{
				zoomedOutPercent = 65;
				zoomedInPercent = 1;
				exponentiality = 1;
				zoomedOutDollyPercent = 1;
				zoomedInDollyPercent = 1;
				zoomedOutDollyFrictionPercent = 0.15f;
				zoomedInDollyFrictionPercent = 0.15f;
				stickyMiddleMouse = false;
				soundNearness = 0;
				hideNamesWhenZoomedOut = true;
				dotSize = 9;
				customNameStyle = LabelStyle.AnimalsDifferent;
				cameraSettingsMod1 = KeyCode.LeftShift;
				cameraSettingsMod2 = KeyCode.None;
				cameraSettingsOption = KeyCode.LeftAlt;
				cameraSettingsKey = KeyCode.Tab;
			}

			list.End();
		}
	}
}