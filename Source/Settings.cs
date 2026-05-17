using System;
using System.Collections.Generic;
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
		public int dotSize = 10;
		public int hidePawnLabelBelow = 0;
		public int hideThingLabelBelow = 32;
		public int hideDeadPawnsBelow = 0;
		public bool mouseOverShowsLabels = true;
		public bool edgeIndicators = true;
		public bool pawnColoredEdgeIndicators = true;
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

		public static float minRootResult = 2;
		public static float maxRootResult = 130;

		public static readonly float minRootInput = 11;
		public static readonly float maxRootInput = 60;

		public static readonly float minRootOutput = 15;
		public static readonly float maxRootOutput = 65;

		const float topRowHeight = 34f;
		const float columnSpacing = 12f;
		const float navWidth = 160f;
		const float helpWidth = 220f;
		const float scrollBarWidth = 16f;

		SettingsTopicId selectedTopic = SettingsTopicId.All;
		Vector2 settingsScrollPosition = Vector2.zero;
		float settingsContentHeight = 1f;
		string hoveredHelpTitleKey;
		string hoveredHelpBodyKey;

		static readonly SettingsTopic[] topics = [
			new(SettingsTopicId.All, "SettingsTopic_All", "SettingsHelp_All"),
			new(SettingsTopicId.Zoom, "SettingsTopic_Zoom", "SettingsHelp_Zoom"),
			new(SettingsTopicId.Movement, "SettingsTopic_Movement", "SettingsHelp_Movement"),
			new(SettingsTopicId.Audio, "SettingsTopic_Audio", "SettingsHelp_Audio"),
			new(SettingsTopicId.Camera, "SettingsTopic_Camera", "SettingsHelp_Camera"),
			new(SettingsTopicId.Labels, "SettingsTopic_Labels", "SettingsHelp_Labels"),
			new(SettingsTopicId.Markers, "SettingsTopic_Markers", "SettingsHelp_Markers"),
			new(SettingsTopicId.Animals, "SettingsTopic_Animals", "SettingsHelp_Animals"),
			new(SettingsTopicId.Edges, "SettingsTopic_Edges", "SettingsHelp_Edges"),
			new(SettingsTopicId.Appearance, "SettingsTopic_Appearance", "SettingsHelp_Appearance"),
		];

		static readonly SettingsGroup[] groups = [
			new(SettingsGroupId.ZoomRange, SettingsTopicId.Zoom, "SettingsGroup_ZoomRange", "SettingsHelp_ZoomRange"),
			new(SettingsGroupId.ZoomCurve, SettingsTopicId.Zoom, "SettingsGroup_ZoomCurve", "SettingsHelp_ZoomCurve"),
			new(SettingsGroupId.MovementSpeed, SettingsTopicId.Movement, "SettingsGroup_MovementSpeed", "SettingsHelp_MovementSpeed"),
			new(SettingsGroupId.EdgeScroll, SettingsTopicId.Movement, "SettingsGroup_EdgeScroll", "SettingsHelp_EdgeScroll"),
			new(SettingsGroupId.CameraBehavior, SettingsTopicId.Camera, "SettingsGroup_CameraBehavior", "SettingsHelp_CameraBehavior"),
			new(SettingsGroupId.AudioListener, SettingsTopicId.Audio, "SettingsGroup_AudioListener", "SettingsHelp_AudioListener"),
			new(SettingsGroupId.LabelVisibility, SettingsTopicId.Labels, "SettingsGroup_LabelVisibility", "SettingsHelp_LabelVisibility"),
			new(SettingsGroupId.MarkerStyle, SettingsTopicId.Markers, "SettingsGroup_MarkerStyle", "SettingsHelp_MarkerStyle"),
			new(SettingsGroupId.AnimalPolicy, SettingsTopicId.Animals, "SettingsGroup_AnimalPolicy", "SettingsHelp_AnimalPolicy"),
			new(SettingsGroupId.EdgeIndicators, SettingsTopicId.Edges, "SettingsGroup_EdgeIndicators", "SettingsHelp_EdgeIndicators"),
			new(SettingsGroupId.MarkerAppearance, SettingsTopicId.Appearance, "SettingsGroup_MarkerAppearance", "SettingsHelp_MarkerAppearance"),
		];

		static List<DotConfig> CurrentDotConfigs()
		{
			var isInGame = Current.Game != null;
			return isInGame ? CameraSettings.settings.dotConfigs : CameraSettings.defaultConfig;
		}

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
			Scribe_Values.Look(ref pawnColoredEdgeIndicators, "pawnColoredEdgeIndicators", defaults.pawnColoredEdgeIndicators);
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

			ApplyCalculatedValues();
		}

		void ApplyCalculatedValues()
		{
			minRootResult = zoomedInPercent * 2;
			maxRootResult = zoomedOutPercent * 2;
		}

		static string OverrideNote(Func<DotConfig, bool> predicate)
		{
			var n = CurrentDotConfigs().Count(dc => predicate(dc));
			if (n == 1)
				return "SettingsNote_OverriddenByRule".Translate(1);
			if (n > 1)
				return "SettingsNote_OverriddenByRules".Translate(n);
			return null;
		}

		public void DoWindowContents(Rect inRect)
		{
			if (Find.WindowStack.currentlyDrawnWindow.draggable == false)
				Find.WindowStack.currentlyDrawnWindow.draggable = true;

			hoveredHelpTitleKey = null;
			hoveredHelpBodyKey = null;

			var outerRect = inRect.ContractedBy(4f);
			var topRect = new Rect(outerRect.x, outerRect.y, outerRect.width, topRowHeight);
			DrawTopActions(topRect);

			var bodyRect = new Rect(outerRect.x, topRect.yMax + 8f, outerRect.width, outerRect.height - topRowHeight - 8f);
			var effectiveHelpWidth = Mathf.Min(helpWidth, Mathf.Max(190f, bodyRect.width * 0.25f));
			var effectiveNavWidth = Mathf.Min(navWidth, Mathf.Max(140f, bodyRect.width * 0.18f));
			var settingsWidth = bodyRect.width - effectiveNavWidth - effectiveHelpWidth - 2f * columnSpacing;

			var navRect = new Rect(bodyRect.x, bodyRect.y, effectiveNavWidth, bodyRect.height);
			var settingsRect = new Rect(navRect.xMax + columnSpacing, bodyRect.y, settingsWidth, bodyRect.height);
			var helpRect = new Rect(settingsRect.xMax + columnSpacing, bodyRect.y, effectiveHelpWidth, bodyRect.height);

			DrawTopicNavigation(navRect);
			DrawSettingsContent(settingsRect);
			DrawHelp(helpRect);
		}

		void DrawTopActions(Rect rect)
		{
			var restoreLabel = "RestoreToDefaultSettings".Translate().ToString();
			var rulesLabel = (Current.Game != null ? "RulesGame" : "RulesStart").Translate().ToString();
			var shortcutsLabel = "HotKeys".Translate().ToString();

			var restoreWidth = Mathf.Min(180f, restoreLabel.GetWidthCached() + 24f);
			var rulesWidth = Mathf.Min(220f, rulesLabel.GetWidthCached() + 24f);
			var shortcutsWidth = Mathf.Min(190f, shortcutsLabel.GetWidthCached() + 24f);
			var buttonY = rect.y + 2f;
			var buttonHeight = rect.height - 4f;

			var restoreRect = new Rect(rect.xMax - restoreWidth, buttonY, restoreWidth, buttonHeight);
			var rulesRect = new Rect(restoreRect.x - rulesWidth - 8f, buttonY, rulesWidth, buttonHeight);
			var shortcutsRect = new Rect(rulesRect.x - shortcutsWidth - 8f, buttonY, shortcutsWidth, buttonHeight);

			if (Widgets.ButtonText(shortcutsRect, shortcutsLabel))
				Find.WindowStack.Add(new Dialog_Shortcuts());
			if (Widgets.ButtonText(rulesRect, rulesLabel))
				OpenRulesDialog();
			if (Widgets.ButtonText(restoreRect, restoreLabel))
				RestoreDefaults();
		}

		void RestoreDefaults()
		{
			Traverse.IterateFields(new CameraPlusSettings(), Settings, (t1, t2) => t2.SetValue(t1.GetValue()));
			ApplyCalculatedValues();
			Caches.ClearMarkerState();
			settingsScrollPosition = Vector2.zero;
		}

		void OpenRulesDialog()
		{
			var isInGame = Current.Game != null;
			var dotConfigDefaults = isInGame ? CameraSettings.defaultConfig : CameraSettings.defaultDefaultConfig;
			var closeAction = isInGame ? null : new Action(() => Tools.SaveDotConfigs(Tools.DefaultRulesFilePath, CameraSettings.defaultConfig));
			Find.WindowStack.Add(new Dialog_Customization(CurrentDotConfigs(), dotConfigDefaults, closeAction));
		}

		void DrawTopicNavigation(Rect rect)
		{
			var y = rect.y;
			foreach (var topic in topics)
			{
				var label = topic.LabelKey.Translate().ToString();
				var rowHeight = Mathf.Max(28f, TextHeight(label, rect.width - 12f, GameFont.Tiny) + 8f);
				var rowRect = new Rect(rect.x, y, rect.width, rowHeight);
				var selected = selectedTopic == topic.Id;
				if (selected)
					DrawFill(rowRect, new Color(1f, 1f, 1f, 0.10f));
				else if (Mouse.IsOver(rowRect))
					DrawFill(rowRect, new Color(1f, 1f, 1f, 0.05f));

				DrawText(rowRect.ContractedBy(6f, 3f), label, GameFont.Tiny, selected ? Color.white : Color.gray);
				if (Widgets.ButtonInvisible(rowRect))
				{
					if (selectedTopic != topic.Id)
						settingsScrollPosition = Vector2.zero;
					selectedTopic = topic.Id;
				}

				if (Mouse.IsOver(rowRect))
					SetHelp(topic.LabelKey, topic.HelpKey);
				y += rowHeight + 2f;
			}
		}

		void DrawSettingsContent(Rect rect)
		{
			var viewRect = new Rect(0f, 0f, rect.width - scrollBarWidth, Mathf.Max(settingsContentHeight, rect.height + 1f));
			Widgets.BeginScrollView(rect, ref settingsScrollPosition, viewRect);
			var ctx = new SettingsUiContext(viewRect.width);

			foreach (var group in groups)
				if (selectedTopic == SettingsTopicId.All || selectedTopic == group.Topic)
					DrawGroup(ctx, group);

			settingsContentHeight = Mathf.Max(rect.height + 1f, ctx.CurY + 12f);
			Widgets.EndScrollView();
		}

		void DrawGroup(SettingsUiContext ctx, SettingsGroup group)
		{
			ctx.Gap(6f);
			DrawGroupTitle(ctx, group.TitleKey, group.HelpKey);
			switch (group.Id)
			{
				case SettingsGroupId.ZoomRange:
					DrawZoomRange(ctx);
					break;
				case SettingsGroupId.ZoomCurve:
					DrawZoomCurve(ctx);
					break;
				case SettingsGroupId.MovementSpeed:
					DrawMovementSpeed(ctx);
					break;
				case SettingsGroupId.EdgeScroll:
					DrawEdgeScroll(ctx);
					break;
				case SettingsGroupId.CameraBehavior:
					DrawCameraBehavior(ctx);
					break;
				case SettingsGroupId.AudioListener:
					DrawAudioListener(ctx);
					break;
				case SettingsGroupId.LabelVisibility:
					DrawLabelVisibility(ctx);
					break;
				case SettingsGroupId.MarkerStyle:
					DrawMarkerStyle(ctx);
					break;
				case SettingsGroupId.AnimalPolicy:
					DrawAnimalPolicy(ctx);
					break;
				case SettingsGroupId.EdgeIndicators:
					DrawEdgeIndicators(ctx);
					break;
				case SettingsGroupId.MarkerAppearance:
					DrawMarkerAppearance(ctx);
					break;
			}
			ctx.Gap(14f);
		}

		void DrawGroupTitle(SettingsUiContext ctx, string titleKey, string helpKey)
		{
			var title = titleKey.Translate().ToString();
			var height = TextHeight(title, ctx.Width, GameFont.Small) + 8f;
			var rect = ctx.GetRect(height);
			if (Mouse.IsOver(rect))
				SetHelp(titleKey, helpKey);
			DrawText(new Rect(rect.x, rect.y, rect.width, height - 3f), title, GameFont.Small, Color.white);
			DrawFill(new Rect(rect.x, rect.yMax - 2f, rect.width, 1f), new Color(1f, 1f, 1f, 0.12f));
		}

		void DrawZoomRange(SettingsUiContext ctx)
		{
			var map = Current.Game?.CurrentMap;

			var previous = zoomedInPercent;
			DrawFloatSlider(ctx, "ZoomedInPercent", zoomedInPercent, 0.1f, 25f, value => $"{Math.Round(value, 1)}%", value =>
			{
				zoomedInPercent = Mathf.Min(value, zoomedOutPercent);
				minRootResult = zoomedInPercent * 2;
				if (previous != zoomedInPercent && map != null && Current.cameraDriverInt.rootSize != minRootInput)
					Current.cameraDriverInt.SetRootPosAndSize(map.rememberedCameraPos.rootPos, minRootInput);
			}, "SettingsHelp_ZoomedInPercent");

			previous = zoomedOutPercent;
			DrawFloatSlider(ctx, "ZoomedOutPercent", zoomedOutPercent, 1f, 100f, value => $"{Math.Round(value, 1)}%", value =>
			{
				zoomedOutPercent = Mathf.Max(zoomedInPercent, value);
				maxRootResult = zoomedOutPercent * 2;
				if (previous != zoomedOutPercent && map != null && Current.cameraDriverInt.rootSize != maxRootInput)
					Current.cameraDriverInt.SetRootPosAndSize(map.rememberedCameraPos.rootPos, maxRootInput);
			}, "SettingsHelp_ZoomedOutPercent");
		}

		void DrawZoomCurve(SettingsUiContext ctx)
		{
			DrawFloatSlider(ctx, "Exponentiality", exponentiality, 0f, 3f, value => value == 0f ? "Off".Translate().ToString() : $"{Mathf.Round(value * 100)}%", value =>
			{
				exponentiality = Mathf.Floor(value * 100) / 100f;
			}, "SettingsHelp_Exponentiality");
		}

		void DrawMovementSpeed(SettingsUiContext ctx)
		{
			DrawFloatSlider(ctx, "ForZoomedInPercent", zoomedInDollyPercent, 0f, 4f, value => $"{Math.Round(value * 100, 1)}%", value => zoomedInDollyPercent = value, "SettingsHelp_ZoomedInDollyPercent");
			DrawFloatSlider(ctx, "ForZoomedOutPercent", zoomedOutDollyPercent, 0f, 4f, value => $"{Math.Round(value * 100, 1)}%", value => zoomedOutDollyPercent = value, "SettingsHelp_ZoomedOutDollyPercent");
		}

		void DrawEdgeScroll(SettingsUiContext ctx)
		{
			var zoomedIn = zoomedInScreenEdgeDollyFactor * 2f;
			DrawFloatSlider(ctx, "ForZoomedInPercent", zoomedIn, 0f, 2f, value => $"{Math.Round(value, 2)}x", value => zoomedInScreenEdgeDollyFactor = value / 2f, "SettingsHelp_ZoomedInEdgeScroll");

			var zoomedOut = zoomedOutScreenEdgeDollyFactor * 2f;
			DrawFloatSlider(ctx, "ForZoomedOutPercent", zoomedOut, 0f, 2f, value => $"{Math.Round(value, 2)}x", value => zoomedOutScreenEdgeDollyFactor = value / 2f, "SettingsHelp_ZoomedOutEdgeScroll");
		}

		void DrawCameraBehavior(SettingsUiContext ctx)
		{
			DrawCheckbox(ctx, "ZoomToMouse", ref zoomToMouse, "SettingsHelp_ZoomToMouse");
			DrawCheckbox(ctx, "DisableCameraShake", ref disableCameraShake, "SettingsHelp_DisableCameraShake");
		}

		void DrawAudioListener(SettingsUiContext ctx)
		{
			DrawFloatSlider(ctx, "SoundNearness", soundNearness, 0f, 1f, value => $"{Math.Round(value * 100, 0)}%", value => soundNearness = value, "SettingsHelp_SoundNearness");
		}

		void DrawLabelVisibility(SettingsUiContext ctx)
		{
			var enabled = dotStyle > DotStyle.VanillaDefault;
			var note = enabled ? null : "SettingsNote_RequiresCameraMarkers".Translate().ToString();

			DrawCheckbox(ctx, "MouseRevealsLabels", ref mouseOverShowsLabels, "SettingsHelp_MouseRevealsLabels", enabled, note ?? OverrideNote(dc => dc.mouseReveals != mouseOverShowsLabels), Caches.ClearMarkerState);
			DrawIntSlider(ctx, "HidePawnLabelBelow", hidePawnLabelBelow, 0, 64, PixelValue, value =>
			{
				hidePawnLabelBelow = value;
				Caches.ClearMarkerState();
			}, "SettingsHelp_HidePawnLabelBelow", enabled, note);
			DrawIntSlider(ctx, "HideThingLabelBelow", hideThingLabelBelow, 0, 64, PixelValue, value =>
			{
				hideThingLabelBelow = value;
				Caches.ClearMarkerState();
			}, "SettingsHelp_HideThingLabelBelow", enabled, note);
			DrawIntSlider(ctx, "HideDeadPawnsBelow", hideDeadPawnsBelow, 0, 64, PixelValue, value => hideDeadPawnsBelow = value, "SettingsHelp_HideDeadPawnsBelow", enabled, note);
		}

		void DrawMarkerStyle(SettingsUiContext ctx)
		{
			DrawDotStyleOption(ctx, DotStyle.VanillaDefault);
			DrawDotStyleOption(ctx, DotStyle.ClassicDots);
			DrawDotStyleOption(ctx, DotStyle.BetterSilhouettes);

			var enabled = dotStyle > DotStyle.VanillaDefault;
			var note = enabled ? null : "SettingsNote_RequiresCameraMarkers".Translate().ToString();
			DrawIntSlider(ctx, "ShowMarkerBelow", dotSize, 1, 64, value => $"{value} {"Pixel".Translate()}", value =>
			{
				dotSize = value;
				Caches.ClearMarkerState();
			}, "SettingsHelp_ShowMarkerBelow", enabled, note, true);
		}

		void DrawAnimalPolicy(SettingsUiContext ctx)
		{
			DrawAnimalStyleOption(ctx, LabelStyle.IncludeAnimals);
			DrawAnimalStyleOption(ctx, LabelStyle.AnimalsDifferent);
			DrawAnimalStyleOption(ctx, LabelStyle.HideAnimals);

			DrawCheckbox(ctx, "IncludeNotTamedAnimals", ref includeNotTamedAnimals, "SettingsHelp_IncludeNotTamedAnimals", true, null, Caches.ClearMarkerState);
		}

		void DrawEdgeIndicators(SettingsUiContext ctx)
		{
			DrawCheckbox(ctx, "EdgeIndicators", ref edgeIndicators, "SettingsHelp_EdgeIndicators", true, OverrideNote(dc => dc.useEdge != edgeIndicators), Caches.ClearMarkerState);
			var note = edgeIndicators ? null : "SettingsNote_RequiresEdgeIndicators".Translate().ToString();
			DrawCheckbox(ctx, "PawnColoredEdgeIndicators", ref pawnColoredEdgeIndicators, "SettingsHelp_AnimalEdgeColors", edgeIndicators, note, Caches.ClearMarkerState, 18f);
		}

		void DrawMarkerAppearance(SettingsUiContext ctx)
		{
			DrawFloatSlider(ctx, "DotSilhouetteSize", dotRelativeSize, 0f, 4f, PercentValue, value => dotRelativeSize = value, "SettingsHelp_DotSilhouetteSize");
			DrawFloatSlider(ctx, "EdgeDotSize", clippedRelativeSize, 0f, 2f, PercentValue, value => clippedRelativeSize = value, "SettingsHelp_EdgeDotSize");
			DrawFloatSlider(ctx, "EdgeDistanceFactor", clippedBorderDistanceFactor, 0f, 2f, PercentValue, value => clippedBorderDistanceFactor = value, "SettingsHelp_EdgeDistanceFactor");
			DrawFloatSlider(ctx, "OutlineSize", outlineFactor, 0f, 0.4f, PercentValue, value =>
			{
				if (Mathf.Approximately(outlineFactor, value) == false)
					MarkerCache.Clear();
				outlineFactor = value;
			}, "SettingsHelp_OutlineSize");
		}

		void DrawDotStyleOption(SettingsUiContext ctx, DotStyle value)
		{
			DrawRadio(ctx, value.ToString(), dotStyle == value, "SettingsHelp_DotStyle", () =>
			{
				if (dotStyle != value)
				{
					dotStyle = value;
					Caches.ClearMarkerState();
				}
			});
		}

		void DrawAnimalStyleOption(SettingsUiContext ctx, LabelStyle value)
		{
			DrawRadio(ctx, value.ToString(), customNameStyle == value, "SettingsHelp_AnimalMarkerStyle", () =>
			{
				if (customNameStyle != value)
				{
					customNameStyle = value;
					Caches.ClearMarkerState();
				}
			});
		}

		void DrawFloatSlider(SettingsUiContext ctx, string labelKey, float value, float min, float max, Func<float, string> valueText, Action<float> setter, string helpKey, bool enabled = true, string note = null)
		{
			var label = labelKey.Translate().ToString();
			var valueLabel = valueText(value);
			var valueWidth = Mathf.Min(120f, Mathf.Max(70f, valueLabel.GetWidthCached() + 8f));
			var labelWidth = ctx.Width - valueWidth - 8f;
			var labelHeight = TextHeight(label, labelWidth, GameFont.Tiny);
			var noteHeight = NoteHeight(note, ctx.Width);
			var row = ctx.GetRect(labelHeight + 28f + noteHeight + 8f);

			DrawControlHover(row, labelKey, helpKey);
			DrawText(new Rect(row.x, row.y, labelWidth, labelHeight), label, GameFont.Tiny, enabled ? Color.white : Color.gray);
			DrawText(new Rect(row.xMax - valueWidth, row.y, valueWidth, labelHeight), valueLabel, GameFont.Tiny, enabled ? Color.white : Color.gray, TextAnchor.UpperRight);

			var sliderRect = new Rect(row.x, row.y + labelHeight + 3f, row.width, 20f);
			var oldEnabled = GUI.enabled;
			GUI.enabled = enabled;
			var newValue = GUI.HorizontalSlider(sliderRect, value, min, max);
			GUI.enabled = oldEnabled;
			if (enabled)
				setter(newValue);

			DrawNote(note, row.x, sliderRect.yMax + 2f, row.width);
		}

		void DrawIntSlider(SettingsUiContext ctx, string labelKey, int value, int min, int max, Func<int, string> valueText, Action<int> setter, string helpKey, bool enabled = true, string note = null, bool clearMarkerState = false)
		{
			DrawFloatSlider(ctx, labelKey, value, min, max, f => valueText(Mathf.RoundToInt(f)), f =>
			{
				var newValue = Mathf.RoundToInt(f);
				if (newValue != value)
				{
					setter(newValue);
					if (clearMarkerState)
						Caches.ClearMarkerState();
				}
			}, helpKey, enabled, note);
		}

		void DrawCheckbox(SettingsUiContext ctx, string labelKey, ref bool value, string helpKey, bool enabled = true, string note = null, Action changed = null, float indent = 0f)
		{
			var label = labelKey.Translate().ToString();
			var labelWidth = ctx.Width - indent - 32f;
			var labelHeight = Mathf.Max(24f, TextHeight(label, labelWidth, GameFont.Tiny));
			var noteHeight = NoteHeight(note, labelWidth);
			var row = ctx.GetRect(labelHeight + noteHeight + 8f);
			var contentRect = new Rect(row.x + indent, row.y, row.width - indent, row.height);
			var lineRect = new Rect(contentRect.x, contentRect.y, contentRect.width, labelHeight);

			DrawControlHover(contentRect, labelKey, helpKey);
			Widgets.CheckboxDraw(contentRect.x, contentRect.y + 2f, value, enabled == false, 24f, null, null);
			DrawText(new Rect(contentRect.x + 30f, contentRect.y, labelWidth, labelHeight), label, GameFont.Tiny, enabled ? Color.white : Color.gray);
			if (enabled && Widgets.ButtonInvisible(lineRect))
			{
				value = !value;
				changed?.Invoke();
			}
			DrawNote(note, contentRect.x + 30f, contentRect.y + labelHeight + 1f, labelWidth);
		}

		void DrawRadio(SettingsUiContext ctx, string labelKey, bool chosen, string helpKey, Action select, bool enabled = true)
		{
			var label = labelKey.Translate().ToString();
			var labelWidth = ctx.Width - 32f;
			var labelHeight = Mathf.Max(24f, TextHeight(label, labelWidth, GameFont.Tiny));
			var row = ctx.GetRect(labelHeight + 4f);

			DrawControlHover(row, labelKey, helpKey);
			var clickedCircle = Widgets.RadioButton(row.x, row.y + 2f, chosen, enabled == false);
			DrawText(new Rect(row.x + 30f, row.y, labelWidth, labelHeight), label, GameFont.Tiny, enabled ? Color.white : Color.gray);
			var clickedLabel = enabled && Widgets.ButtonInvisible(new Rect(row.x + 30f, row.y, labelWidth, labelHeight));
			if ((clickedCircle || clickedLabel) && enabled)
				select();
		}

		void DrawHelp(Rect rect)
		{
			var topic = TopicFor(selectedTopic);
			var titleKey = hoveredHelpTitleKey ?? topic.LabelKey;
			var bodyKey = hoveredHelpBodyKey ?? topic.HelpKey;
			var y = rect.y;

			DrawText(new Rect(rect.x, y, rect.width, 28f), "SettingsHelp_Title".Translate().ToString(), GameFont.Small, Color.white);
			y += 32f;

			var about = "SettingsHelp_About".Translate(titleKey.Translate()).ToString();
			var aboutHeight = TextHeight(about, rect.width, GameFont.Tiny);
			DrawText(new Rect(rect.x, y, rect.width, aboutHeight), about, GameFont.Tiny, Color.white);
			y += aboutHeight + 8f;

			var body = bodyKey.Translate().ToString();
			DrawText(new Rect(rect.x, y, rect.width, rect.yMax - y), body, GameFont.Tiny, Color.gray);
		}

		void SetHelp(string titleKey, string bodyKey)
		{
			hoveredHelpTitleKey = titleKey;
			hoveredHelpBodyKey = bodyKey;
		}

		void DrawControlHover(Rect rect, string titleKey, string helpKey)
		{
			if (Mouse.IsOver(rect))
			{
				DrawFill(rect, new Color(1f, 1f, 1f, 0.035f));
				SetHelp(titleKey, helpKey);
			}
		}

		static SettingsTopic TopicFor(SettingsTopicId id)
		{
			foreach (var topic in topics)
				if (topic.Id == id)
					return topic;
			return topics[0];
		}

		static string PixelValue(int value)
			=> value == 0 ? "Never".Translate().ToString() : $"{value} {"Pixel".Translate()}";

		static string PercentValue(float value)
			=> $"{Math.Round(value * 100, 0)}%";

		static float NoteHeight(string note, float width)
			=> note.NullOrEmpty() ? 0f : TextHeight(note, width, GameFont.Tiny) + 2f;

		static void DrawNote(string note, float x, float y, float width)
		{
			if (note.NullOrEmpty())
				return;
			DrawText(new Rect(x, y, width, TextHeight(note, width, GameFont.Tiny)), note, GameFont.Tiny, Color.gray);
		}

		static float TextHeight(string text, float width, GameFont font)
		{
			var oldFont = Text.Font;
			var oldWrap = Text.WordWrap;
			Text.Font = font;
			Text.WordWrap = true;
			var height = Text.CalcHeight(text, width);
			Text.Font = oldFont;
			Text.WordWrap = oldWrap;
			return Mathf.Ceil(height);
		}

		static void DrawText(Rect rect, string text, GameFont font, Color color, TextAnchor anchor = TextAnchor.UpperLeft)
		{
			var oldFont = Text.Font;
			var oldColor = GUI.color;
			var oldAnchor = Text.Anchor;
			var oldWrap = Text.WordWrap;
			Text.Font = font;
			Text.Anchor = anchor;
			Text.WordWrap = true;
			GUI.color = color;
			Widgets.Label(rect, text);
			Text.Font = oldFont;
			Text.Anchor = oldAnchor;
			Text.WordWrap = oldWrap;
			GUI.color = oldColor;
		}

		static void DrawFill(Rect rect, Color color)
		{
			var oldColor = GUI.color;
			GUI.color = color;
			GUI.DrawTexture(rect, BaseContent.WhiteTex);
			GUI.color = oldColor;
		}

		readonly struct SettingsTopic(SettingsTopicId id, string labelKey, string helpKey)
		{
			public readonly SettingsTopicId Id = id;
			public readonly string LabelKey = labelKey;
			public readonly string HelpKey = helpKey;
		}

		readonly struct SettingsGroup(SettingsGroupId id, SettingsTopicId topic, string titleKey, string helpKey)
		{
			public readonly SettingsGroupId Id = id;
			public readonly SettingsTopicId Topic = topic;
			public readonly string TitleKey = titleKey;
			public readonly string HelpKey = helpKey;
		}

		sealed class SettingsUiContext(float width)
		{
			public float Width { get; } = width;
			public float CurY { get; private set; }

			public Rect GetRect(float height)
			{
				var rect = new Rect(0f, CurY, Width, height);
				CurY += height;
				return rect;
			}

			public void Gap(float height)
			{
				CurY += height;
			}
		}

		enum SettingsTopicId
		{
			All,
			Zoom,
			Movement,
			Audio,
			Camera,
			Labels,
			Markers,
			Animals,
			Edges,
			Appearance
		}

		enum SettingsGroupId
		{
			ZoomRange,
			ZoomCurve,
			MovementSpeed,
			EdgeScroll,
			CameraBehavior,
			AudioListener,
			LabelVisibility,
			MarkerStyle,
			AnimalPolicy,
			EdgeIndicators,
			MarkerAppearance
		}
	}
}
