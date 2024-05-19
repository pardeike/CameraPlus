using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	public class CameraPlusSettings : ModSettings
	{
		public int currentVersion = 3;
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
		public int hideDeadPawnsBelow = 9;
		public bool mouseOverShowsLabels = true;
		public bool edgeIndicators = true;
		public LabelStyle customNameStyle = LabelStyle.AnimalsDifferent;
		public bool includeNotTamedAnimals = true;
		public float dotRelativeSize = 1.25f;
		public float clippedRelativeSize = 0.75f;
		public float clippedBorderDistanceFactor = 0.4f;
		public float outlineFactor = 0.1f;

		public KeyCode[] cameraSettingsMod = [KeyCode.LeftShift, KeyCode.None];
		public KeyCode cameraSettingsKey = KeyCode.Tab;
		public KeyCode[] cameraSettingsLoad = [KeyCode.LeftShift, KeyCode.None];
		public KeyCode[] cameraSettingsSave = [KeyCode.LeftAlt, KeyCode.None];

		public OptionalColor[] playerNormalOuterColors = [new OptionalColor(Color.black), new OptionalColor(Color.white)];
		public OptionalColor[] playerNormalInnerColors = [new OptionalColor(Color.white), new OptionalColor(Color.white)];

		public OptionalColor[] playerDraftedOuterColors = [new OptionalColor(new(0f, 0.5f, 0f)), new OptionalColor(new(0.25f, 0.75f, 0.25f))];
		public OptionalColor[] playerDraftedInnerColors = [new OptionalColor(Color.white), new OptionalColor(Color.white)];

		public OptionalColor[] playerDownedOuterColors = [new OptionalColor(Color.gray), new OptionalColor(Color.white)];
		public OptionalColor[] playerDownedInnerColors = [new OptionalColor(Color.gray), new OptionalColor(Color.gray)];

		public OptionalColor[] playerMentalOuterColors = [new OptionalColor(new(0.5f, 0f, 0f)), new OptionalColor(Color.white)];
		public OptionalColor[] playerMentalInnerColors = [new OptionalColor(new(0.5f, 0f, 0f)), new OptionalColor(new(0.5f, 0f, 0f))];

		public CustomColor[] customColors =
		[
			new CustomColor([new AnimalTag()], [new OptionalColor(), new OptionalColor()])
		];

		public OptionalColor[] defaultOuterColors = [new OptionalColor(), new OptionalColor()];
		public OptionalColor[] defaultInnerColors = [new OptionalColor(), new OptionalColor()];
		public OptionalColor[] customOuterColors = [new OptionalColor(), new OptionalColor()];
		public OptionalColor[] customInnerColors = [new OptionalColor(), new OptionalColor()];

		public static float minRootResult = 2;
		public static float maxRootResult = 130;

		public static readonly float minRootInput = 11;
		public static readonly float maxRootInput = 60;

		public static readonly float minRootOutput = 15;
		public static readonly float maxRootOutput = 65;

		public override void ExposeData()
		{
			base.ExposeData();
			var defaults = new CameraPlusSettings();
			Scribe_Values.Look(ref currentVersion, "currentVersion", 0);
			Scribe_Values.Look(ref zoomedOutPercent, "zoomedOutPercent", defaults.zoomedOutPercent);
			Scribe_Values.Look(ref zoomedInPercent, "zoomedInPercent", defaults.zoomedInPercent);
			Scribe_Values.Look(ref exponentiality, "exponentiality", defaults.exponentiality);
			Scribe_Values.Look(ref zoomedOutDollyPercent, "zoomedOutDollyPercent", defaults.zoomedOutDollyPercent);
			Scribe_Values.Look(ref zoomedInDollyPercent, "zoomedInDollyPercent", defaults.zoomedInDollyPercent);
			Scribe_Values.Look(ref zoomedOutScreenEdgeDollyFactor, "zoomedOutScreenEdgeDollyFactor", defaults.zoomedOutScreenEdgeDollyFactor);
			Scribe_Values.Look(ref zoomedInScreenEdgeDollyFactor, "zoomedInScreenEdgeDollyFactor", defaults.zoomedInScreenEdgeDollyFactor);
			Scribe_Values.Look(ref stickyMiddleMouse, "stickyMiddleMouse", defaults.stickyMiddleMouse);
			Scribe_Values.Look(ref zoomToMouse, "zoomToMouse", defaults.zoomToMouse);
			Scribe_Values.Look(ref disableCameraShake, "disableCameraShake", defaults.disableCameraShake);
			Scribe_Values.Look(ref soundNearness, "soundNearness", defaults.soundNearness);
			Scribe_Values.Look(ref dotStyle, "dotStyle", defaults.dotStyle);
			Scribe_Values.Look(ref dotSize, "dotSize", defaults.dotSize);
			Scribe_Values.Look(ref hidePawnLabelBelow, "hidePawnLabelBelow", defaults.hidePawnLabelBelow);
			Scribe_Values.Look(ref hideThingLabelBelow, "hideThingLabelBelow", defaults.hideThingLabelBelow);
			Scribe_Values.Look(ref hideDeadPawnsBelow, "hideDeadPawnsBelow", defaults.hideDeadPawnsBelow);
			Scribe_Values.Look(ref mouseOverShowsLabels, "mouseOverShowsLabels", defaults.mouseOverShowsLabels);
			Scribe_Values.Look(ref edgeIndicators, "edgeIndicators", defaults.edgeIndicators);
			Scribe_Values.Look(ref customNameStyle, "customNameStyle", defaults.customNameStyle);
			Scribe_Values.Look(ref includeNotTamedAnimals, "includeNotTamedAnimals", defaults.includeNotTamedAnimals);
			Scribe_Values.Look(ref dotRelativeSize, "dotRelativeSize", defaults.dotRelativeSize);
			Scribe_Values.Look(ref clippedRelativeSize, "clippedRelativeSize", defaults.clippedRelativeSize);
			Scribe_Values.Look(ref clippedBorderDistanceFactor, "clippedBorderDistanceFactor", defaults.clippedBorderDistanceFactor);
			Scribe_Values.Look(ref outlineFactor, "outlineFactor", defaults.outlineFactor);
			Tools.ScribeArrays(ref cameraSettingsMod, "cameraSettingsMod", defaults.cameraSettingsMod);
			Scribe_Values.Look(ref cameraSettingsKey, "cameraSettingsKey", defaults.cameraSettingsKey);
			Tools.ScribeArrays(ref cameraSettingsLoad, "cameraSettingsLoad", defaults.cameraSettingsLoad);
			Tools.ScribeArrays(ref cameraSettingsSave, "cameraSettingsSave", defaults.cameraSettingsSave);
			Tools.ScribeArrays(ref playerNormalOuterColors, "playerNormalOuterColors", defaults.playerNormalOuterColors);
			Tools.ScribeArrays(ref playerNormalInnerColors, "playerNormalInnerColors", defaults.playerNormalInnerColors);
			Tools.ScribeArrays(ref playerDraftedOuterColors, "playerDraftedOuterColors", defaults.playerDraftedOuterColors);
			Tools.ScribeArrays(ref playerDraftedInnerColors, "playerDraftedInnerColors", defaults.playerDraftedInnerColors);
			Tools.ScribeArrays(ref playerDownedOuterColors, "playerDownedOuterColors", defaults.playerDownedOuterColors);
			Tools.ScribeArrays(ref playerDownedInnerColors, "playerDownedInnerColors", defaults.playerDownedInnerColors);
			Tools.ScribeArrays(ref playerMentalOuterColors, "playerMentalOuterColors", defaults.playerMentalOuterColors);
			Tools.ScribeArrays(ref playerMentalInnerColors, "playerMentalInnerColors", defaults.playerMentalInnerColors);
			Tools.ScribeArrays(ref defaultOuterColors, "defaultOuterColors", defaults.defaultOuterColors);
			Tools.ScribeArrays(ref defaultInnerColors, "defaultInnerColors", defaults.defaultInnerColors);
			Tools.ScribeArrays(ref customOuterColors, "customOuterColors", defaults.customOuterColors);
			Tools.ScribeArrays(ref customInnerColors, "customInnerColors", defaults.customInnerColors);

			ApplyCalculatedValues();
		}

		void ApplyCalculatedValues()
		{
			minRootResult = zoomedInPercent * 2;
			maxRootResult = zoomedOutPercent * 2;
		}

		public void DoWindowContents(Rect inRect)
		{
			if (Find.WindowStack.currentlyDrawnWindow.draggable == false)
				Find.WindowStack.currentlyDrawnWindow.draggable = true;

			var restoreText = "RestoreToDefaultSettings".Translate();
			var restoreLen = restoreText.GetWidthCached() + 12f;
			var rect = new Rect(inRect.width - restoreLen, inRect.yMin - 30f, restoreLen, 30f);
			if (Widgets.ButtonText(rect, restoreText))
			{
				Traverse.IterateFields(new CameraPlusSettings(), Settings, (t1, t2) => t2.SetValue(t1.GetValue()));
				ApplyCalculatedValues();
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
					if (list.ButtonText("Colors".Translate(), Current.Game != null))
						Find.WindowStack.Add(new Dialog_Customization());
				},
				0.4f
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
				list.Slider(ref hidePawnLabelBelow, 0, 64, () => "HidePawnLabelBelow".Translate() + (hidePawnLabelBelow == 0 ? "Never".Translate() : hidePawnLabelBelow + " " + pixel));
				list.Slider(ref hideThingLabelBelow, 0, 64, () => "HideThingLabelBelow".Translate() + (hideThingLabelBelow == 0 ? "Never".Translate() : hideThingLabelBelow + " " + pixel));
				list.Slider(ref hideDeadPawnsBelow, 0, 64, () => "HideDeadPawnsBelow".Translate() + (hideDeadPawnsBelow == 0 ? "Never".Translate() : hideDeadPawnsBelow + " " + pixel));
				list.Slider(ref dotSize, 1, 64, () => "ShowMarkerBelow".Translate() + dotSize + " " + "Pixel".Translate());

				list.Gap(12f);

				_ = list.Label("Animals".Translate());
				foreach (var label in Enum.GetNames(typeof(LabelStyle)))
				{
					if (dotStyle == DotStyle.BetterSilhouettes && label == LabelStyle.AnimalsDifferent.ToString())
						continue;
					var val = (LabelStyle)Enum.Parse(typeof(LabelStyle), label);
					if (list.RadioButton(label.Translate(), customNameStyle == val, 8f))
						customNameStyle = val;
				}
				list.Gap(4f);
				list.CheckboxLabeled("IncludeNotTamedAnimals".Translate(), ref includeNotTamedAnimals);
			}

			list.Gap(16f);

			list.Slider(ref dotRelativeSize, 0f, 4f, () => "DotSilhouetteSize".Translate() + ": " + Math.Round(dotRelativeSize * 100, 0) + "%");
			list.Slider(ref clippedRelativeSize, 0f, 2f, () => "EdgeDotSize".Translate() + ": " + Math.Round(clippedRelativeSize * 100, 0) + "%");
			list.Slider(ref clippedBorderDistanceFactor, 0f, 2f, () => "EdgeDistanceFactor".Translate() + ": " + Math.Round(clippedBorderDistanceFactor * 100, 0) + "%");
			var oldOutlineFactor = outlineFactor;
			list.Slider(ref outlineFactor, 0f, 0.4f, () => "OutlineSize".Translate() + ": " + Math.Round(outlineFactor * 100, 0) + "%");
			if (oldOutlineFactor != outlineFactor)
				MarkerCache.cache.Clear();

			list.End();
		}
	}
}
