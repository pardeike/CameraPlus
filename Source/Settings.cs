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
		const float contentHorizontalPadding = 12f;
		const float bottomButtonClearance = 64f;
		const float navItemPadding = 8f;
		const float navIconBoxSize = 26f;
		const float navIconPadding = 2f;
		const float navIconLabelGap = 8f;
		const float groupHeaderContentGap = 10f;
		const float shortcutButtonWidth = 80f;
		const float shortcutButtonSpacing = 4f;
		const float shortcutButtonHeight = 28f;

		static readonly Color DisabledTextColor = new Color(0.55f, 0.55f, 0.55f);
		static readonly Color RuleNoteColor = new Color(0.95f, 0.72f, 0.36f);

		SettingsTopicId selectedTopic = SettingsTopicId.All;
		Vector2 settingsScrollPosition = Vector2.zero;
		float settingsContentHeight = 1f;
		string hoveredHelpTitleKey;
		string hoveredHelpBodyKey;
		string hoveredHelpExtraText;

		static readonly SettingsTopic[] topics = [
			new(SettingsTopicId.All, "SettingsTopic_All", "SettingsHelp_All"),
			new(SettingsTopicId.Zoom, "SettingsTopic_Zoom", "SettingsHelp_Zoom", "SettingsTopics/Zoom"),
			new(SettingsTopicId.Movement, "SettingsTopic_Movement", "SettingsHelp_Movement", "SettingsTopics/Movement"),
			new(SettingsTopicId.Audio, "SettingsTopic_Audio", "SettingsHelp_Audio", "SettingsTopics/Audio"),
			new(SettingsTopicId.Camera, "SettingsTopic_Camera", "SettingsHelp_Camera", "SettingsTopics/Camera"),
			new(SettingsTopicId.Labels, "SettingsTopic_Labels", "SettingsHelp_Labels", "SettingsTopics/Labels"),
			new(SettingsTopicId.Markers, "SettingsTopic_Markers", "SettingsHelp_Markers", "SettingsTopics/Markers"),
			new(SettingsTopicId.Animals, "SettingsTopic_Animals", "SettingsHelp_Animals", "SettingsTopics/Animals"),
			new(SettingsTopicId.Edges, "SettingsTopic_Edges", "SettingsHelp_Edges", "SettingsTopics/Edges"),
			new(SettingsTopicId.Appearance, "SettingsTopic_Appearance", "SettingsHelp_Appearance", "SettingsTopics/Appearance"),
			new(SettingsTopicId.Shortcuts, "SettingsTopic_Keyboard", "SettingsHelp_Shortcuts", "SettingsTopics/Keyboard"),
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
			new(SettingsGroupId.KeyboardShortcuts, SettingsTopicId.Shortcuts, "HotKeys", "SettingsHelp_KeyboardShortcuts"),
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

		static int RuleCount(Func<DotConfig, bool> predicate)
			=> CurrentDotConfigs().Count(dc => predicate(dc));

		static string OverrideNote(int n)
		{
			if (n == 1)
				return "SettingsNote_OverriddenByRule".Translate(1);
			if (n > 1)
				return "SettingsNote_OverriddenByRules".Translate(n);
			return null;
		}

		static string OverrideNote(Func<DotConfig, bool> predicate)
			=> OverrideNote(RuleCount(predicate));

		static string RuleOverrideHelp(int n)
			=> n <= 0 ? null : (n == 1 ? "SettingsHelp_RuleOverrideByRule".Translate().ToString() : "SettingsHelp_RuleOverrideByRules".Translate(n).ToString());

		static string RuleMarkerStyleHelp(int n)
			=> n <= 0 ? null : (n == 1 ? "SettingsHelp_RuleMarkerStyleByRule".Translate().ToString() : "SettingsHelp_RuleMarkerStyleByRules".Translate(n).ToString());

		static string RuleMarkerThresholdHelp(int n)
			=> n <= 0 ? null : (n == 1 ? "SettingsHelp_RuleMarkerThresholdByRule".Translate().ToString() : "SettingsHelp_RuleMarkerThresholdByRules".Translate(n).ToString());

		static string RuleSizeMultiplierHelp(int n)
			=> n <= 0 ? null : (n == 1 ? "SettingsHelp_RuleSizeMultiplierByRule".Translate().ToString() : "SettingsHelp_RuleSizeMultiplierByRules".Translate(n).ToString());

		static string RuleInsideMarkerHelp(int n)
			=> n <= 0 ? null : (n == 1 ? "SettingsHelp_RuleInsideMarkerByRule".Translate().ToString() : "SettingsHelp_RuleInsideMarkerByRules".Translate(n).ToString());

		static string RuleEdgeDisabledByStyleHelp(int n)
			=> n <= 0 ? null : (n == 1 ? "SettingsHelp_RuleEdgeDisabledByStyleByRule".Translate().ToString() : "SettingsHelp_RuleEdgeDisabledByStyleByRules".Translate(n).ToString());

		static string RuleAnimalEdgeColorHelp(int n)
			=> n <= 0 ? null : (n == 1 ? "SettingsHelp_RuleAnimalEdgeColorByRule".Translate().ToString() : "SettingsHelp_RuleAnimalEdgeColorByRules".Translate(n).ToString());

		static string CombineHelp(params string[] paragraphs)
		{
			var result = "";
			foreach (var paragraph in paragraphs)
			{
				if (paragraph.NullOrEmpty())
					continue;
				if (result.NullOrEmpty() == false)
					result += "\n\n";
				result += paragraph;
			}
			return result.NullOrEmpty() ? null : result;
		}

		public void DoWindowContents(Rect inRect)
		{
			if (Find.WindowStack.currentlyDrawnWindow.draggable == false)
				Find.WindowStack.currentlyDrawnWindow.draggable = true;

			hoveredHelpTitleKey = null;
			hoveredHelpBodyKey = null;
			hoveredHelpExtraText = null;

			var outerRect = inRect.ContractedBy(4f);
			var topRect = new Rect(outerRect.x, outerRect.y, outerRect.width, topRowHeight);
			DrawTopActions(topRect);

			var bodyRect = new Rect(outerRect.x, topRect.yMax + 8f, outerRect.width, outerRect.height - topRowHeight - 8f - bottomButtonClearance);
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

			var restoreWidth = Mathf.Min(180f, restoreLabel.GetWidthCached() + 24f);
			var rulesWidth = Mathf.Min(220f, rulesLabel.GetWidthCached() + 24f);
			var buttonY = rect.y + 2f;
			var buttonHeight = rect.height - 4f;

			var restoreRect = new Rect(rect.xMax - restoreWidth, buttonY, restoreWidth, buttonHeight);
			var rulesRect = new Rect(restoreRect.x - rulesWidth - 8f, buttonY, rulesWidth, buttonHeight);

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
				var icon = TopicIcon(topic);
				var hasIcon = icon != null;
				var labelX = rect.x + navItemPadding + (hasIcon ? navIconBoxSize + navIconLabelGap : 0f);
				var labelWidth = rect.xMax - navItemPadding - labelX;
				var labelHeight = TextHeight(label, labelWidth, GameFont.Small);
				var lineHeight = TextLineHeight(GameFont.Small);
				var minTextHeight = hasIcon
					? navItemPadding + (navIconBoxSize - lineHeight) / 2f + labelHeight + navItemPadding
					: labelHeight + 2f * navItemPadding;
				var rowHeight = Mathf.Max(38f, minTextHeight, hasIcon ? navIconBoxSize + 2f * navItemPadding : 0f);
				var rowRect = new Rect(rect.x, y, rect.width, rowHeight);
				var selected = selectedTopic == topic.Id;
				if (selected)
					DrawFill(rowRect, new Color(1f, 1f, 1f, 0.13f));
				else if (Mouse.IsOver(rowRect))
					DrawFill(rowRect, new Color(1f, 1f, 1f, 0.06f));

				if (hasIcon)
				{
					var iconBox = new Rect(rect.x + navItemPadding, rowRect.y + navItemPadding, navIconBoxSize, navIconBoxSize);
					GUI.DrawTexture(iconBox.ContractedBy(navIconPadding), icon, ScaleMode.ScaleToFit, true);
					var labelRect = new Rect(labelX, iconBox.center.y - lineHeight / 2f, labelWidth, labelHeight);
					DrawText(labelRect, label, GameFont.Small, Color.white);
				}
				else
				{
					DrawText(rowRect.ContractedBy(navItemPadding, 0f), label, GameFont.Small, Color.white, TextAnchor.MiddleLeft);
				}

				if (Widgets.ButtonInvisible(rowRect))
				{
					if (selectedTopic != topic.Id)
					{
						settingsScrollPosition = Vector2.zero;
						settingsContentHeight = 1f;
					}
					selectedTopic = topic.Id;
				}

				if (Mouse.IsOver(rowRect))
					SetHelp(topic.LabelKey, topic.HelpKey);
				y += rowHeight + 2f;
			}
		}

		void DrawSettingsContent(Rect rect)
		{
			var viewHeight = Mathf.Max(settingsContentHeight, rect.height);
			var needsScrollBar = viewHeight > rect.height;
			var viewWidth = rect.width - (needsScrollBar ? scrollBarWidth : 0f);
			var viewRect = new Rect(0f, 0f, viewWidth, viewHeight);
			Widgets.BeginScrollView(rect, ref settingsScrollPosition, viewRect);
			var ctx = new SettingsUiContext(contentHorizontalPadding, viewRect.width - 2f * contentHorizontalPadding);

			foreach (var group in groups)
				if (selectedTopic == SettingsTopicId.All || selectedTopic == group.Topic)
					DrawGroup(ctx, group);

			settingsContentHeight = Mathf.Max(rect.height, ctx.CurY + 12f);
			Widgets.EndScrollView();
		}

		void DrawGroup(SettingsUiContext ctx, SettingsGroup group)
		{
			ctx.Gap(6f);
			DrawGroupTitle(ctx, group.TitleKey, group.HelpKey);
			ctx.Gap(groupHeaderContentGap);
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
				case SettingsGroupId.KeyboardShortcuts:
					DrawKeyboardShortcuts(ctx);
					break;
			}
			ctx.Gap(14f);
		}

		void DrawGroupTitle(SettingsUiContext ctx, string titleKey, string helpKey)
		{
			var title = titleKey.Translate().ToString();
			var height = Mathf.Max(28f, TextHeight(title, ctx.Width - 2f * navItemPadding, GameFont.Small) + 6f);
			var rect = ctx.GetRect(height);
			if (Mouse.IsOver(rect))
				SetHelp(titleKey, helpKey);
			DrawFill(rect, new Color(1f, 1f, 1f, 0.13f));
			DrawText(rect.ContractedBy(navItemPadding, 0f), title, GameFont.Small, Color.white, TextAnchor.MiddleLeft);
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

			var mouseRevealRuleCount = enabled ? RuleCount(dc => dc.mouseReveals != mouseOverShowsLabels) : 0;
			var overrideNote = OverrideNote(mouseRevealRuleCount);
			DrawCheckbox(ctx, "MouseRevealsLabels", ref mouseOverShowsLabels, "SettingsHelp_MouseRevealsLabels", enabled, note ?? overrideNote, Caches.ClearMarkerState, 0f, note == null && overrideNote != null ? RuleNoteColor : DisabledTextColor, RuleOverrideHelp(mouseRevealRuleCount));
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
			var markerStyleRuleCount = RuleCount(dc => dc.mode != dotStyle);
			var insideMarkerRuleCount = RuleCount(dc => dc.useInside == false);
			var markerStyleHelp = CombineHelp(RuleMarkerStyleHelp(markerStyleRuleCount), RuleInsideMarkerHelp(insideMarkerRuleCount));
			DrawDotStyleOption(ctx, DotStyle.VanillaDefault, markerStyleHelp);
			DrawDotStyleOption(ctx, DotStyle.ClassicDots, markerStyleHelp);
			DrawDotStyleOption(ctx, DotStyle.BetterSilhouettes, markerStyleHelp);
			ctx.Gap(8f);

			var enabled = dotStyle > DotStyle.VanillaDefault;
			var note = enabled ? null : "SettingsNote_RequiresCameraMarkers".Translate().ToString();
			var thresholdRuleCount = enabled ? RuleCount(dc => dc.showBelowPixels != -1) : 0;
			var thresholdNote = note ?? OverrideNote(thresholdRuleCount);
			var thresholdHelp = CombineHelp(RuleMarkerThresholdHelp(thresholdRuleCount), RuleInsideMarkerHelp(insideMarkerRuleCount));
			DrawIntSlider(ctx, "ShowMarkerBelow", dotSize, 1, 64, value => $"{value} {"Pixel".Translate()}", value =>
			{
				dotSize = value;
				Caches.ClearMarkerState();
			}, "SettingsHelp_ShowMarkerBelow", enabled, thresholdNote, true, note == null && thresholdNote != null ? RuleNoteColor : DisabledTextColor, thresholdHelp);
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
			var edgeRuleCount = RuleCount(dc => dc.useEdge != edgeIndicators);
			var edgeOffStyleRuleCount = RuleCount(dc => dc.mode == DotStyle.Off);
			var edgeHelp = CombineHelp(RuleOverrideHelp(edgeRuleCount), RuleEdgeDisabledByStyleHelp(edgeOffStyleRuleCount));
			DrawCheckbox(ctx, "EdgeIndicators", ref edgeIndicators, "SettingsHelp_EdgeIndicators", true, OverrideNote(edgeRuleCount), Caches.ClearMarkerState, 0f, RuleNoteColor, edgeHelp);
			var note = edgeIndicators ? null : "SettingsNote_RequiresEdgeIndicators".Translate().ToString();
			var animalEdgeColorRuleCount = edgeIndicators && pawnColoredEdgeIndicators ? RuleCount(dc => dc.fillColor.a > 0f || dc.fillSelectedColor.a > 0f) : 0;
			DrawCheckbox(ctx, "PawnColoredEdgeIndicators", ref pawnColoredEdgeIndicators, "SettingsHelp_AnimalEdgeColors", edgeIndicators, note, Caches.ClearMarkerState, 18f, helpExtra: RuleAnimalEdgeColorHelp(animalEdgeColorRuleCount));
		}

		void DrawMarkerAppearance(SettingsUiContext ctx)
		{
			var sizeRuleCount = RuleCount(dc => Mathf.Approximately(dc.relativeSize, 1f) == false);
			var sizeHelp = RuleSizeMultiplierHelp(sizeRuleCount);
			DrawFloatSlider(ctx, "DotSilhouetteSize", dotRelativeSize, 0f, 4f, PercentValue, value => dotRelativeSize = value, "SettingsHelp_DotSilhouetteSize", helpExtra: sizeHelp);
			DrawFloatSlider(ctx, "EdgeDotSize", clippedRelativeSize, 0f, 2f, PercentValue, value => clippedRelativeSize = value, "SettingsHelp_EdgeDotSize", helpExtra: sizeHelp);
			DrawFloatSlider(ctx, "EdgeDistanceFactor", clippedBorderDistanceFactor, 0f, 2f, PercentValue, value => clippedBorderDistanceFactor = value, "SettingsHelp_EdgeDistanceFactor");
			var outlineRuleCount = RuleCount(dc => Mathf.Approximately(dc.outlineFactor, outlineFactor) == false);
			var outlineNote = OverrideNote(outlineRuleCount);
			DrawFloatSlider(ctx, "OutlineSize", outlineFactor, 0f, 0.4f, PercentValue, value =>
			{
				if (Mathf.Approximately(outlineFactor, value) == false)
					MarkerCache.Clear();
				outlineFactor = value;
			}, "SettingsHelp_OutlineSize", note: outlineNote, noteColor: outlineNote != null ? RuleNoteColor : DisabledTextColor, helpExtra: RuleOverrideHelp(outlineRuleCount));
		}

		void DrawKeyboardShortcuts(SettingsUiContext ctx)
		{
			DrawShortcutRow(ctx, "SettingsKey", "SettingsHelp_SettingsShortcut", rect =>
			{
				var keyRect = TakeShortcutButton(ref rect);
				Tools.KeySettingsButton(keyRect, true, Settings.cameraSettingsKey, KeyCode.Tab, code => Settings.cameraSettingsKey = code);
				var secondModifierRect = TakeShortcutButton(ref rect);
				Tools.KeySettingsButton(secondModifierRect, false, Settings.cameraSettingsMod[1], KeyCode.None, code => Settings.cameraSettingsMod[1] = code);
				var firstModifierRect = TakeShortcutButton(ref rect);
				Tools.KeySettingsButton(firstModifierRect, false, Settings.cameraSettingsMod[0], KeyCode.None, code => Settings.cameraSettingsMod[0] = code);
			});

			DrawShortcutRow(ctx, "LoadModifier", "SettingsHelp_LoadShortcut", rect =>
			{
				var numberRect = TakeShortcutButton(ref rect);
				DrawText(numberRect, "1 - 9", GameFont.Small, Color.white, TextAnchor.MiddleCenter);
				var secondModifierRect = TakeShortcutButton(ref rect);
				Tools.KeySettingsButton(secondModifierRect, false, Settings.cameraSettingsLoad[1], KeyCode.None, code => Settings.cameraSettingsLoad[1] = code);
				var firstModifierRect = TakeShortcutButton(ref rect);
				Tools.KeySettingsButton(firstModifierRect, false, Settings.cameraSettingsLoad[0], KeyCode.LeftShift, code => Settings.cameraSettingsLoad[0] = code);
			});

			DrawShortcutRow(ctx, "SaveModifier", "SettingsHelp_SaveShortcut", rect =>
			{
				var numberRect = TakeShortcutButton(ref rect);
				DrawText(numberRect, "1 - 9", GameFont.Small, Color.white, TextAnchor.MiddleCenter);
				var secondModifierRect = TakeShortcutButton(ref rect);
				Tools.KeySettingsButton(secondModifierRect, false, Settings.cameraSettingsSave[1], KeyCode.None, code => Settings.cameraSettingsSave[1] = code);
				var firstModifierRect = TakeShortcutButton(ref rect);
				Tools.KeySettingsButton(firstModifierRect, false, Settings.cameraSettingsSave[0], KeyCode.LeftAlt, code => Settings.cameraSettingsSave[0] = code);
			});
		}

		void DrawShortcutRow(SettingsUiContext ctx, string labelKey, string helpKey, Action<Rect> drawButtons)
		{
			var label = labelKey.Translate().ToString();
			var buttonsWidth = 3f * shortcutButtonWidth + 2f * shortcutButtonSpacing;
			var labelWidth = ctx.Width - buttonsWidth - 12f;
			var labelHeight = TextHeight(label, labelWidth, GameFont.Small);
			var rowHeight = Mathf.Max(shortcutButtonHeight, labelHeight) + 8f;
			var row = ctx.GetRect(rowHeight);
			DrawControlHover(row, labelKey, helpKey);
			DrawText(new Rect(row.x, row.y, labelWidth, row.height), label, GameFont.Small, Color.white, TextAnchor.MiddleLeft);
			drawButtons(new Rect(row.xMax - buttonsWidth, row.y + (row.height - shortcutButtonHeight) / 2f, buttonsWidth, shortcutButtonHeight));
		}

		static Rect TakeShortcutButton(ref Rect available)
		{
			var rect = new Rect(available.xMax - shortcutButtonWidth, available.y, shortcutButtonWidth, shortcutButtonHeight);
			available.xMax = rect.x - shortcutButtonSpacing;
			return rect;
		}

		void DrawDotStyleOption(SettingsUiContext ctx, DotStyle value, string helpExtra)
		{
			DrawRadio(ctx, value.ToString(), dotStyle == value, "SettingsHelp_DotStyle", () =>
			{
				if (dotStyle != value)
				{
					dotStyle = value;
					Caches.ClearMarkerState();
				}
			}, helpExtra: helpExtra);
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

		void DrawFloatSlider(SettingsUiContext ctx, string labelKey, float value, float min, float max, Func<float, string> valueText, Action<float> setter, string helpKey, bool enabled = true, string note = null, Color? noteColor = null, string helpExtra = null)
		{
			var label = ControlLabel(labelKey);
			var valueLabel = valueText(value);
			var valueWidth = Mathf.Min(120f, Mathf.Max(70f, TextWidth(valueLabel, GameFont.Small) + 8f));
			var labelWidth = ctx.Width - valueWidth - 8f;
			var labelHeight = TextHeight(label, labelWidth, GameFont.Small);
			var noteHeight = NoteHeight(note, ctx.Width);
			var row = ctx.GetRect(labelHeight + 30f + noteHeight + 8f);

			DrawControlHover(row, labelKey, helpKey, helpExtra);
			DrawText(new Rect(row.x, row.y, labelWidth, labelHeight), label, GameFont.Small, enabled ? Color.white : DisabledTextColor);
			DrawText(new Rect(row.xMax - valueWidth, row.y, valueWidth, labelHeight), valueLabel, GameFont.Small, enabled ? Color.white : DisabledTextColor, TextAnchor.UpperRight);

			var sliderRect = new Rect(row.x, row.y + labelHeight + 5f, row.width, 20f);
			var oldEnabled = GUI.enabled;
			GUI.enabled = enabled;
			var newValue = GUI.HorizontalSlider(sliderRect, value, min, max);
			GUI.enabled = oldEnabled;
			if (enabled)
				setter(newValue);

			DrawNote(note, row.x, sliderRect.yMax + 2f, row.width, noteColor ?? DisabledTextColor);
		}

		void DrawIntSlider(SettingsUiContext ctx, string labelKey, int value, int min, int max, Func<int, string> valueText, Action<int> setter, string helpKey, bool enabled = true, string note = null, bool clearMarkerState = false, Color? noteColor = null, string helpExtra = null)
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
			}, helpKey, enabled, note, noteColor, helpExtra);
		}

		void DrawCheckbox(SettingsUiContext ctx, string labelKey, ref bool value, string helpKey, bool enabled = true, string note = null, Action changed = null, float indent = 0f, Color? noteColor = null, string helpExtra = null)
		{
			var label = ControlLabel(labelKey);
			var labelWidth = ctx.Width - indent - 32f;
			var labelHeight = Mathf.Max(28f, TextHeight(label, labelWidth, GameFont.Small));
			var noteHeight = NoteHeight(note, labelWidth);
			var row = ctx.GetRect(labelHeight + noteHeight + 8f);
			var contentRect = new Rect(row.x + indent, row.y, row.width - indent, row.height);
			var lineRect = new Rect(contentRect.x, contentRect.y, contentRect.width, labelHeight);

			DrawControlHover(contentRect, labelKey, helpKey, helpExtra);
			Widgets.CheckboxDraw(contentRect.x, contentRect.y + (labelHeight - 24f) / 2f, value, enabled == false, 24f, null, null);
			DrawText(new Rect(contentRect.x + 30f, contentRect.y, labelWidth, labelHeight), label, GameFont.Small, enabled ? Color.white : DisabledTextColor, TextAnchor.MiddleLeft);
			if (enabled && Widgets.ButtonInvisible(lineRect))
			{
				value = !value;
				changed?.Invoke();
			}
			DrawNote(note, contentRect.x + 30f, contentRect.y + labelHeight + 1f, labelWidth, noteColor ?? DisabledTextColor);
		}

		void DrawRadio(SettingsUiContext ctx, string labelKey, bool chosen, string helpKey, Action select, bool enabled = true, string helpExtra = null)
		{
			var label = labelKey.Translate().ToString();
			var labelWidth = ctx.Width - 32f;
			var labelHeight = Mathf.Max(28f, TextHeight(label, labelWidth, GameFont.Small));
			var row = ctx.GetRect(labelHeight + 6f);

			DrawControlHover(row, labelKey, helpKey, helpExtra);
			var clickedCircle = Widgets.RadioButton(row.x, row.y + (labelHeight - 24f) / 2f, chosen, enabled == false);
			DrawText(new Rect(row.x + 30f, row.y, labelWidth, labelHeight), label, GameFont.Small, enabled ? Color.white : DisabledTextColor, TextAnchor.MiddleLeft);
			var clickedLabel = enabled && Widgets.ButtonInvisible(new Rect(row.x + 30f, row.y, labelWidth, labelHeight));
			if ((clickedCircle || clickedLabel) && enabled)
				select();
		}

		void DrawHelp(Rect rect)
		{
			var topic = TopicFor(selectedTopic);
			var titleKey = hoveredHelpTitleKey ?? topic.LabelKey;
			var bodyKey = hoveredHelpBodyKey ?? topic.HelpKey;
			var title = titleKey.NullOrEmpty() ? null : titleKey.Translate().ToString();
			var body = bodyKey.NullOrEmpty() ? null : bodyKey.Translate().ToString();
			var extra = hoveredHelpExtraText;

			if (title.NullOrEmpty() && body.NullOrEmpty() && extra.NullOrEmpty())
			{
				DrawText(rect, "SettingsHelp_Title".Translate().ToString(), GameFont.Small, Color.white, TextAnchor.MiddleCenter);
				return;
			}

			var y = rect.y;
			if (title.NullOrEmpty() == false)
			{
				var about = "SettingsHelp_About".Translate(title).ToString();
				var aboutHeight = TextHeight(about, rect.width, GameFont.Small);
				DrawText(new Rect(rect.x, y, rect.width, aboutHeight), about, GameFont.Small, Color.white);
				y += aboutHeight + 10f;
			}

			if (body.NullOrEmpty() == false && y < rect.yMax)
			{
				var bodyHeight = TextHeight(body, rect.width, GameFont.Small);
				DrawText(new Rect(rect.x, y, rect.width, Mathf.Min(bodyHeight, rect.yMax - y)), body, GameFont.Small, Color.white);
				y += bodyHeight + 10f;
			}

			if (extra.NullOrEmpty() == false && y < rect.yMax)
				DrawText(new Rect(rect.x, y, rect.width, rect.yMax - y), extra, GameFont.Small, RuleNoteColor);
		}

		void SetHelp(string titleKey, string bodyKey, string extraText = null)
		{
			hoveredHelpTitleKey = titleKey;
			hoveredHelpBodyKey = bodyKey;
			hoveredHelpExtraText = extraText;
		}

		void DrawControlHover(Rect rect, string titleKey, string helpKey, string extraText = null)
		{
			if (Mouse.IsOver(rect))
			{
				DrawFill(rect, new Color(1f, 1f, 1f, 0.035f));
				SetHelp(titleKey, helpKey, extraText);
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

		static string ControlLabel(string key)
			=> key.Translate("").ToString();

		static float NoteHeight(string note, float width)
			=> note.NullOrEmpty() ? 0f : TextHeight(note, width, GameFont.Tiny) + 2f;

		static void DrawNote(string note, float x, float y, float width, Color color)
		{
			if (note.NullOrEmpty())
				return;
			DrawText(new Rect(x, y, width, TextHeight(note, width, GameFont.Tiny)), note, GameFont.Tiny, color);
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

		static float TextWidth(string text, GameFont font)
		{
			var oldFont = Text.Font;
			Text.Font = font;
			var width = Text.CalcSize(text).x;
			Text.Font = oldFont;
			return Mathf.Ceil(width);
		}

		static float TextLineHeight(GameFont font)
			=> TextHeight("Ag", 9999f, font);

		static Texture2D TopicIcon(SettingsTopic topic)
			=> topic.IconPath.NullOrEmpty() ? null : ContentFinder<Texture2D>.Get(topic.IconPath, false);

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

		readonly struct SettingsTopic(SettingsTopicId id, string labelKey, string helpKey, string iconPath = null)
		{
			public readonly SettingsTopicId Id = id;
			public readonly string LabelKey = labelKey;
			public readonly string HelpKey = helpKey;
			public readonly string IconPath = iconPath;
		}

		readonly struct SettingsGroup(SettingsGroupId id, SettingsTopicId topic, string titleKey, string helpKey)
		{
			public readonly SettingsGroupId Id = id;
			public readonly SettingsTopicId Topic = topic;
			public readonly string TitleKey = titleKey;
			public readonly string HelpKey = helpKey;
		}

		sealed class SettingsUiContext(float x, float width)
		{
			readonly float x = x;
			public float Width { get; } = width;
			public float CurY { get; private set; }

			public Rect GetRect(float height)
			{
				var rect = new Rect(x, CurY, Width, height);
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
			Appearance,
			Shortcuts
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
			MarkerAppearance,
			KeyboardShortcuts
		}
	}
}
