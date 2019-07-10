using Harmony;
using System;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class CameraPlusSettings : ModSettings
	{
		public float zoomedOutPercent = 65;
		public float zoomedInPercent = 1;
		public int exponentiality = 1;
		public float zoomedOutDollyPercent = 1;
		public float zoomedInDollyPercent = 1;
		public float zoomedOutDollyFrictionPercent = 0.15f;
		public float zoomedInDollyFrictionPercent = 0.15f;
		public bool zoomToMouse = true;
		public float soundNearness = 0;
		public bool hideNamesWhenZoomedOut = true;
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
			Scribe_Values.Look(ref zoomToMouse, "zoomToMouse", true);
			Scribe_Values.Look(ref soundNearness, "soundNearness", 0);
			Scribe_Values.Look(ref hideNamesWhenZoomedOut, "hideNamesWhenZoomedOut", true);
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

			list.Label("ZoomedInPercent".Translate() + ": " + Math.Round(zoomedInPercent, 1) + "%");
			previous = zoomedInPercent;
			zoomedInPercent = list.Slider(zoomedInPercent, 0.1f, 20f);
			minRootResult = zoomedInPercent * 2;
			if (previous != zoomedInPercent && map != null)
			{
				var val = Traverse.Create(Find.CameraDriver).Field("rootSize").GetValue<float>();
				if (val != minRootInput)
					Find.CameraDriver.SetRootPosAndSize(map.rememberedCameraPos.rootPos, minRootInput);
			}

			list.Gap(12f);

			list.Label("ZoomedOutPercent".Translate() + ": " + Math.Round(zoomedOutPercent, 1) + "%");
			previous = zoomedOutPercent;
			zoomedOutPercent = list.Slider(zoomedOutPercent, 50f, 100f);
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

			list.Gap(12f);

			list.Label("SoundNearness".Translate() + ": " + Math.Round(soundNearness * 100, 1) + "%");
			soundNearness = list.Slider(soundNearness, 0f, 1f);

			list.Gap(12f);

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

			// cannot preview here because this value is not showing differences in either
			// min or max setting (that is when you have changed any of the above sliders)

			list.NewColumn();
			list.Gap(12f);

			list.Label("DollyPercentLabel".Translate());
			list.Gap(4f);
			list.Label("ZoomedIn".Translate() + ": " + Math.Round(zoomedInDollyPercent * 100, 1) + "%", -1f);
			zoomedInDollyPercent = Mathf.Round(100f * list.Slider(zoomedInDollyPercent, 0f, 4f)) / 100f;
			list.Gap(4f);
			list.Label("ZoomedOut".Translate() + ": " + Math.Round(zoomedOutDollyPercent * 100, 1) + "%", -1f);
			zoomedOutDollyPercent = Mathf.Round(100f * list.Slider(zoomedOutDollyPercent, 0f, 4f)) / 100f;

			list.Gap(12f);

			list.Label("DollyFrictionLabel".Translate());
			list.Gap(4f);
			list.Label("ZoomedIn".Translate() + ": " + Math.Round(zoomedInDollyFrictionPercent * 100, 1) + "%", -1f);
			zoomedInDollyFrictionPercent = Mathf.Round(100f * list.Slider(zoomedInDollyFrictionPercent, 0f, 1f)) / 100f;
			list.Gap(4f);
			list.Label("ZoomedOut".Translate() + ": " + Math.Round(zoomedOutDollyFrictionPercent * 100, 1) + "%", -1f);
			zoomedOutDollyFrictionPercent = Mathf.Round(100f * list.Slider(zoomedOutDollyFrictionPercent, 0f, 1f)) / 100f;

			list.Gap(12f);

			list.CheckboxLabeled("HideNamesWhenZoomedOut".Translate(), ref hideNamesWhenZoomedOut);
			list.CheckboxLabeled("ZoomToMouse".Translate(), ref zoomToMouse);

			list.Gap(12f);

			if (list.ButtonText("RestoreToDefaultSettings".Translate()))
			{
				zoomedOutPercent = 65;
				zoomedInPercent = 1;
				exponentiality = 1;
				zoomedOutDollyPercent = 1;
				zoomedInDollyPercent = 1;
				zoomedOutDollyFrictionPercent = 0.15f;
				zoomedInDollyFrictionPercent = 0.15f;
				soundNearness = 0;
				hideNamesWhenZoomedOut = true;
				cameraSettingsMod1 = KeyCode.LeftShift;
				cameraSettingsMod2 = KeyCode.None;
				cameraSettingsOption = KeyCode.LeftAlt;
				cameraSettingsKey = KeyCode.Tab;
			}

			list.End();
		}
	}
}