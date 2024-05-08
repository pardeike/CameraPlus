﻿using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public enum DotStyle
	{
		VanillaDefault = 0,
		ClassicDots = 1,
		BetterSilhouettes = 2
	}

	public enum LabelStyle
	{
		IncludeAnimals = 0,
		AnimalsDifferent = 1,
		HideAnimals = 2
	}

	public class SavedViews(Map map) : MapComponent(map)
	{
		public RememberedCameraPos[] views = new RememberedCameraPos[9];

		public override void ExposeData()
		{
			for (var i = 0; i < 9; i++)
				Scribe_Deep.Look(ref views[i], "view" + (i + 1), [map]);
		}
	}

	public class CameraPlusSettings : ModSettings
	{
		public float zoomedOutPercent = 65;
		public float zoomedInPercent = 1;
		public float exponentiality = 0.5f;
		public float zoomedOutDollyPercent = 1;
		public float zoomedInDollyPercent = 1;
		public float zoomedOutScreenEdgeDollyFactor = 0.5f;
		public float zoomedInScreenEdgeDollyFactor = 0.5f;
		public bool stickyMiddleMouse = false;
		public bool zoomToMouse = true;
		public bool disableCameraShake = false;
		public float soundNearness = 0;
		public DotStyle dotStyle = DotStyle.BetterSilhouettes;
		public int dotSize = 9;
		public int hidePawnLabelBelow = 9;
		public int hideThingLabelBelow = 32;
		public bool mouseOverShowsLabels = true;
		public bool edgeIndicators = true;
		public LabelStyle customNameStyle = LabelStyle.AnimalsDifferent;
		public bool includeNotTamedAnimals = true;
		public float dotRelativeSize = 1f;
		public float clippedRelativeSize = 1f;
		public float outlineFactor = 0.15f;

		public KeyCode[] cameraSettingsMod = [KeyCode.LeftShift, KeyCode.LeftControl];
		public KeyCode cameraSettingsKey = KeyCode.Tab;
		public KeyCode[] cameraSettingsLoad = [KeyCode.LeftShift, KeyCode.None];
		public KeyCode[] cameraSettingsSave = [KeyCode.LeftAlt, KeyCode.None];

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
			Scribe_Values.Look(ref exponentiality, "exponentiality", 0.5f);
			Scribe_Values.Look(ref zoomedOutDollyPercent, "zoomedOutDollyPercent", 1);
			Scribe_Values.Look(ref zoomedInDollyPercent, "zoomedInDollyPercent", 1);
			Scribe_Values.Look(ref zoomedOutScreenEdgeDollyFactor, "zoomedOutScreenEdgeDollyFactor", 0.5f);
			Scribe_Values.Look(ref zoomedInScreenEdgeDollyFactor, "zoomedInScreenEdgeDollyFactor", 0.5f);
			Scribe_Values.Look(ref stickyMiddleMouse, "stickyMiddleMouse", false);
			Scribe_Values.Look(ref zoomToMouse, "zoomToMouse", true);
			Scribe_Values.Look(ref disableCameraShake, "disableCameraShake", false);
			Scribe_Values.Look(ref soundNearness, "soundNearness", 0);
			Scribe_Values.Look(ref dotStyle, "dotStyle", DotStyle.BetterSilhouettes);
			Scribe_Values.Look(ref dotSize, "dotSize", 9);
			Scribe_Values.Look(ref hidePawnLabelBelow, "hidePawnLabelBelow", 0);
			Scribe_Values.Look(ref hideThingLabelBelow, "hideThingLabelBelow", 32);
			Scribe_Values.Look(ref mouseOverShowsLabels, "mouseOverShowsLabels", true);
			Scribe_Values.Look(ref customNameStyle, "customNameStyle", LabelStyle.AnimalsDifferent);
			Scribe_Values.Look(ref includeNotTamedAnimals, "includeNotTamedAnimals", true);
			Scribe_Values.Look(ref dotRelativeSize, "dotRelativeSize", 1f);
			Scribe_Values.Look(ref clippedRelativeSize, "clippedRelativeSize", 1f);
			Scribe_Values.Look(ref outlineFactor, "outlineFactor", 0.15f);
			Scribe_Values.Look(ref cameraSettingsMod[0], "cameraSettingsMod1", KeyCode.LeftShift);
			Scribe_Values.Look(ref cameraSettingsMod[1], "cameraSettingsMod2", KeyCode.None);
			Scribe_Values.Look(ref cameraSettingsKey, "cameraSettingsKey", KeyCode.Tab);
			Scribe_Values.Look(ref cameraSettingsLoad[0], "cameraSettingsLoad1", KeyCode.LeftShift);
			Scribe_Values.Look(ref cameraSettingsLoad[1], "cameraSettingsLoad2", KeyCode.None);
			Scribe_Values.Look(ref cameraSettingsSave[0], "cameraSettingsSave1", KeyCode.LeftAlt);
			Scribe_Values.Look(ref cameraSettingsSave[1], "cameraSettingsSave2", KeyCode.None);

			if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
			{
				minRootResult = zoomedInPercent * 2;
				maxRootResult = zoomedOutPercent * 2;
			}
		}

		public void DoWindowContents(Rect inRect)
		{
			var restoreText = "RestoreToDefaultSettings".Translate();
			var restoreLen = restoreText.GetWidthCached() + 12f;
			var rect = new Rect(inRect.width - restoreLen, inRect.yMin - 30f, restoreLen, 30f);
			if (Widgets.ButtonText(rect, restoreText))
			{
				zoomedOutPercent = 65;
				zoomedInPercent = 1;
				exponentiality = 0.5f;
				zoomedOutDollyPercent = 1;
				zoomedInDollyPercent = 1;
				zoomedOutScreenEdgeDollyFactor = 0.5f;
				zoomedInScreenEdgeDollyFactor = 0.5f;
				stickyMiddleMouse = false;
				zoomToMouse = true;
				disableCameraShake = false;
				soundNearness = 0;
				dotStyle = DotStyle.BetterSilhouettes;
				dotSize = 9;
				hidePawnLabelBelow = 9;
				hideThingLabelBelow = 32;
				mouseOverShowsLabels = true;
				edgeIndicators = true;
				customNameStyle = LabelStyle.AnimalsDifferent;
				includeNotTamedAnimals = true;
				dotRelativeSize = 1f;
				clippedRelativeSize = 1f;
				outlineFactor = 0.15f;
				cameraSettingsMod[0] = KeyCode.LeftShift;
				cameraSettingsMod[1] = KeyCode.None;
				cameraSettingsKey = KeyCode.Tab;
				cameraSettingsLoad[0] = KeyCode.LeftShift;
				cameraSettingsLoad[1] = KeyCode.None;
				cameraSettingsSave[0] = KeyCode.LeftAlt;
				cameraSettingsSave[1] = KeyCode.None;
			}

			float previous;
			var map = Current.Game?.CurrentMap;

			var list = new Listing_Standard { ColumnWidth = (inRect.width - 34f) / 2f };
			list.Begin(inRect);

			list.Gap(16f);

			_ = list.Label("Zoom".Translate());

			previous = zoomedInPercent;
			list.Slider(ref zoomedInPercent, 0.1f, 25f, () => "ZoomedInPercent".Translate() + ": " + Math.Round(zoomedInPercent, 1) + "%");
			zoomedInPercent = Mathf.Min(zoomedInPercent, zoomedOutPercent);
			minRootResult = zoomedInPercent * 2;
			if (previous != zoomedInPercent && map != null)
			{
				var val = Current.cameraDriverInt.rootSize;
				if (val != minRootInput)
					Current.cameraDriverInt.SetRootPosAndSize(map.rememberedCameraPos.rootPos, minRootInput);
			}

			previous = zoomedOutPercent;
			list.Slider(ref zoomedOutPercent, 1f, 100f, () => "ZoomedOutPercent".Translate() + ": " + Math.Round(zoomedOutPercent, 1) + "%");
			zoomedOutPercent = Mathf.Max(zoomedInPercent, zoomedOutPercent);
			maxRootResult = zoomedOutPercent * 2;
			if (previous != zoomedOutPercent && map != null)
			{
				var val = Current.cameraDriverInt.rootSize;
				if (val != maxRootInput)
					Current.cameraDriverInt.SetRootPosAndSize(map.rememberedCameraPos.rootPos, maxRootInput);
			}

			exponentiality = Mathf.Floor(exponentiality * 100) / 100f;
			list.Slider(ref exponentiality, 0f, 3f, () => $"{"Exponentiality".Translate()}: " + (exponentiality == 0 ? "Off".Translate() : $"{Mathf.Round(exponentiality * 100)} %"));

			list.Gap(16f);

			_ = list.Label("DollyPercentLabel".Translate());
			list.TwoColumns(
				() => list.Slider(ref zoomedInDollyPercent, 0f, 4f, () => "ForZoomedInPercent".Translate() + ": " + Math.Round(zoomedInDollyPercent * 100, 1) + "%"),
				() => list.Slider(ref zoomedOutDollyPercent, 0f, 4f, () => "ForZoomedOutPercent".Translate() + ": " + Math.Round(zoomedOutDollyPercent * 100, 1) + " % ")
			);

			list.Gap(16f);

			_ = list.Label("ScreenEdgeDollyFrictionLabel".Translate());
			zoomedInScreenEdgeDollyFactor *= 2f;
			zoomedOutScreenEdgeDollyFactor *= 2f;
			list.TwoColumns(
				() => list.Slider(ref zoomedInScreenEdgeDollyFactor, 0f, 2f, () => "ForZoomedInPercent".Translate() + ": " + Math.Round(zoomedInScreenEdgeDollyFactor, 2) + "x"),
				() => list.Slider(ref zoomedOutScreenEdgeDollyFactor, 0f, 2f, () => "ForZoomedOutPercent".Translate() + ": " + Math.Round(zoomedOutScreenEdgeDollyFactor, 2) + "x")
			);
			zoomedInScreenEdgeDollyFactor /= 2f;
			zoomedOutScreenEdgeDollyFactor /= 2f;

			list.Gap(16f);

			list.CheckboxLabeled("ZoomToMouse".Translate(), ref zoomToMouse);
			list.CheckboxLabeled("MouseRevealsLabels".Translate(), ref mouseOverShowsLabels);
			list.CheckboxLabeled("EdgeIndicators".Translate(), ref edgeIndicators);
			list.CheckboxLabeled("DisableCameraShake".Translate(), ref disableCameraShake);

			list.Gap(16f);

			_ = list.Label("SoundNearness".Translate() + ": " + Math.Round(soundNearness * 100, 0) + "%");
			list.Slider(ref soundNearness, 0f, 1f, null);

			list.Gap(4f);

			list.TwoColumns(
				() =>
				{
					if (list.ButtonText("HotKeys".Translate()))
						Find.WindowStack.Add(new Dialog_Shortcuts());
				},
				() =>
				{
					if (list.ButtonText("Colors".Translate()))
						Log.Warning("Not implemented yet");
				}
			);

			list.NewColumn(); // -----------------------------------------------------------------------------------------------
			list.curX += 17;
			list.Gap(16f);

			_ = list.Label("DotStyle".Translate());
			var oldDotStyle = dotStyle;
			foreach (var label in Enum.GetNames(typeof(DotStyle)))
			{
				var val = (DotStyle)Enum.Parse(typeof(DotStyle), label);
				if (list.RadioButton(label.Translate(), dotStyle == val, 8f))
					dotStyle = val;
			}

			list.Gap(12f);

			if (dotStyle != DotStyle.VanillaDefault)
			{
				list.Gap(4f);

				var pixel = "Pixel".Translate();
				var label1 = "HidePawnLabelBelow".Translate();
				list.Slider(ref hidePawnLabelBelow, 0, 128, () => label1 + (hidePawnLabelBelow == 0 ? "Never".Translate() : hidePawnLabelBelow + " " + pixel));
				var label2 = "HideThingLabelBelow".Translate();
				list.Slider(ref hideThingLabelBelow, 0, 128, () => label2 + (hideThingLabelBelow == 0 ? "Never".Translate() : hideThingLabelBelow + " " + pixel));
				list.Slider(ref dotSize, 1, 32, () => "ShowMarkerBelow".Translate() + dotSize + " " + "Pixel".Translate());

				list.Gap(12f);

				_ = list.Label("Animals".Translate());
				foreach (var label in Enum.GetNames(typeof(LabelStyle)))
				{
					var val = (LabelStyle)Enum.Parse(typeof(LabelStyle), label);
					if (list.RadioButton(label.Translate(), customNameStyle == val, 8f))
						customNameStyle = val;
				}
				list.Gap(4f);
				list.CheckboxLabeled("IncludeNotTamedAnimals".Translate(), ref includeNotTamedAnimals);
			}

			list.Gap(16f);

			list.Slider(ref dotRelativeSize, 0f, 2f, () => "DotSilhouetteSize".Translate() + ": " + Math.Round(dotRelativeSize * 100, 0) + "%");
			list.Slider(ref clippedRelativeSize, 0f, 2f, () => "EdgeDotSize".Translate() + ": " + Math.Round(clippedRelativeSize * 100, 0) + "%");
			list.Slider(ref outlineFactor, 0f, 0.4f, () => "OutlineSize".Translate() + ": " + Math.Round(outlineFactor * 100, 0) + "%");

			list.End();
		}
	}
}
