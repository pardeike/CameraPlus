using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CameraPlus;
using HarmonyLib;
using RimBridgeServer.Sdk;
using RimWorld;
using UnityEngine;
using Verse;

namespace CameraPlus.BridgeTools
{
	public sealed class CameraPlusBridgeTools
	{
		const string harmonyOwner = "net.pardeike.rimworld.mod.camera+";

		[Tool(
			"cameraplus/get_edge_ui_insets",
			Description = "Read CameraPlus's four live two-axis RimWorld interface insets for edge markers.",
			ResultDescription = "Returns marker counts, observed UI rectangles, and independently animated horizontal and vertical offsets for the four corner interface regions.")]
		public static object GetEdgeUiInsets()
		{
			if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || CameraPlusMain.Settings == null)
				return new { success = false, error = "A playable map and loaded CameraPlus settings are required." };

			var state = EdgeUIInsets.State();
			return new
			{
				success = true,
				modVersion = typeof(CameraPlusMain).Assembly.GetName().Version?.ToString() ?? string.Empty,
				mapId = Find.CurrentMap.uniqueID,
				frame = Time.frameCount,
				uiScale = Prefs.UIScale,
				screenWidth = UI.screenWidth,
				screenHeight = UI.screenHeight,
				state.enabled,
				state.leftMarkerCount,
				state.rightMarkerCount,
				state.topMarkerCount,
				state.bottomMarkerCount,
				topLeft = EdgeInsetChannel(state.topLeft),
				bottomLeft = EdgeInsetChannel(state.bottomLeft),
				topRight = EdgeInsetChannel(state.topRight),
				bottomRight = EdgeInsetChannel(state.bottomRight)
			};
		}

		static object EdgeInsetChannel(EdgeUIInsetChannelState channel)
			=> new
			{
				channel.uiVisible,
				channel.overlappingMarkers,
				uiMinX = channel.uiBounds.xMin,
				uiMaxX = channel.uiBounds.xMax,
				uiMinY = channel.uiBounds.yMin,
				uiMaxY = channel.uiBounds.yMax,
				horizontal = new
				{
					observed = channel.observed.x,
					target = channel.target.x,
					current = channel.current.x
				},
				vertical = new
				{
					observed = channel.observed.y,
					target = channel.target.y,
					current = channel.current.y
				}
			};

		[Tool(
			"cameraplus/validate_todo_runtime",
			Description = "Run CameraPlus's live semantic checks for the current TODO behavior slices against a playable map.",
			ResultDescription = "Returns a pass/fail result and evidence for movement, labels, floating text, dead pawns, animal edges, shortcuts, Shift zoom behavior, and obsolete settings XML.")]
		public static object ValidateTodoRuntime()
		{
			if (Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || CameraPlusMain.Settings == null)
				return new { success = false, error = "A playable map and loaded CameraPlus settings are required." };

			var movement = ValidateMovement();
			var labels = ValidateLabels();
			var floatingText = ValidateFloatingText();
			var deadPawns = ValidateDeadPawns();
			var animalEdges = ValidateAnimalEdges();
			var markerCaches = ValidateMarkerCaches();
			var shortcuts = ValidateShortcuts();
			var shiftZoom = ValidateShiftZoom();
			var obsoleteSettings = ValidateObsoleteSettings();

			return new
			{
				success = movement.success
					&& labels.success
					&& floatingText.success
					&& deadPawns.success
					&& animalEdges.success
					&& markerCaches.success
					&& shortcuts.success
					&& shiftZoom.success
					&& obsoleteSettings.success,
				modVersion = typeof(CameraPlusMain).Assembly.GetName().Version?.ToString() ?? string.Empty,
				mapId = Find.CurrentMap.uniqueID,
				movement,
				labels,
				floatingText,
				deadPawns,
				animalEdges,
				markerCaches,
				shortcuts,
				shiftZoom,
				obsoleteSettings
			};
		}

		static ValidationCase ValidateMovement()
		{
			var settings = CameraPlusMain.Settings;
			var driver = Current.cameraDriverInt;
			var originalRootPos = driver.rootPos;
			var originalRootSize = driver.rootSize;
			var originalInMovement = settings.zoomedInDollyPercent;
			var originalOutMovement = settings.zoomedOutDollyPercent;
			var originalInEdge = settings.zoomedInScreenEdgeDollyFactor;
			var originalOutEdge = settings.zoomedOutScreenEdgeDollyFactor;

			try
			{
				settings.zoomedInDollyPercent = 1f;
				settings.zoomedOutDollyPercent = 1f;
				settings.zoomedInScreenEdgeDollyFactor = 0.5f;
				settings.zoomedOutScreenEdgeDollyFactor = 0.5f;

				var roots = new[]
				{
					CameraPlusSettings.minRootInput,
					(CameraPlusSettings.minRootInput + CameraPlusSettings.maxRootInput) / 2f,
					CameraPlusSettings.maxRootInput
				};
				var normal = roots.Select(root => ReadCameraRates(driver, originalRootPos, root)).ToArray();

				settings.zoomedInScreenEdgeDollyFactor = 0f;
				settings.zoomedOutScreenEdgeDollyFactor = 0f;
				var zeroEdge = roots.Select(root => ReadCameraRates(driver, originalRootPos, root)).ToArray();

				var calculateInput = AccessTools.Method(typeof(CameraDriver), "CalculateCurInputDollyVect");
				var onGui = AccessTools.Method(typeof(CameraDriver), nameof(CameraDriver.CameraDriverOnGUI));
				var calculateOwners = PatchOwners(calculateInput);
				var onGuiOwners = PatchOwners(onGui);
				var configuredRatesMatch = normal.All(sample => Approximately(sample.keyRate, sample.expectedKeyRate) && Approximately(sample.edgeRate, sample.expectedEdgeRate));
				var zeroDisablesOnlyEdge = zeroEdge.All(sample => Approximately(sample.edgeRate, 0f))
					&& normal.Zip(zeroEdge, (before, after) => Approximately(before.keyRate, after.keyRate)).All(value => value);
				var defaultCalibration = Approximately(normal[0].keyRate, 25f)
					&& Approximately(normal[2].keyRate, 247.5f)
					&& Approximately(normal[0].edgeRate, 15f)
					&& Approximately(normal[2].edgeRate, 150f);
				var vanillaInputPathsUnpatched = calculateOwners.Contains(harmonyOwner) == false && onGuiOwners.Contains(harmonyOwner) == false;

				return new ValidationCase(
					configuredRatesMatch && zeroDisablesOnlyEdge && defaultCalibration && vanillaInputPathsUnpatched,
					new
					{
						configuredRatesMatch,
						zeroDisablesOnlyEdge,
						defaultCalibration,
						vanillaInputPathsUnpatched,
						inputMethod = "CameraDriver.CalculateCurInputDollyVect handles rebound MapDolly keys and Shift",
						middleDragMethod = "CameraDriver.CameraDriverOnGUI handles middle-mouse raw dolly",
						calculateInputPatchOwners = calculateOwners,
						onGuiPatchOwners = onGuiOwners,
						normal,
						zeroEdge
					});
			}
			finally
			{
				settings.zoomedInDollyPercent = originalInMovement;
				settings.zoomedOutDollyPercent = originalOutMovement;
				settings.zoomedInScreenEdgeDollyFactor = originalInEdge;
				settings.zoomedOutScreenEdgeDollyFactor = originalOutEdge;
				driver.SetRootPosAndSize(originalRootPos, originalRootSize);
			}
		}

		static CameraRateSample ReadCameraRates(CameraDriver driver, Vector3 rootPos, float rootSize)
		{
			driver.SetRootPosAndSize(rootPos, rootSize);
			var orthographicSize = Tools.LerpRootSize(rootSize);
			return new CameraRateSample
			{
				rootSize = rootSize,
				orthographicSize = orthographicSize,
				keyRate = driver.config.dollyRateKeys,
				edgeRate = driver.config.dollyRateScreenEdge,
				expectedKeyRate = Tools.GetDollyRateKeys(orthographicSize),
				expectedEdgeRate = Tools.GetDollyRateScreenEdge(orthographicSize)
			};
		}

		static ValidationCase ValidateLabels()
		{
			var map = Find.CurrentMap;
			var pawn = map.mapPawns.FreeColonistsSpawned.FirstOrDefault();
			if (pawn == null)
				return new ValidationCase(false, new { error = "The test map has no spawned free colonist." });

			var settings = CameraPlusMain.Settings;
			var originalStyle = settings.dotStyle;
			var originalDotSize = settings.dotSize;
			var originalThreshold = settings.hidePawnLabelBelow;
			var originalMouseReveal = settings.mouseOverShowsLabels;
			var originalAnimalStyle = settings.customNameStyle;
			var originalIncludeWild = settings.includeNotTamedAnimals;
			var worldSettings = CameraSettings.settings;
			var originalRules = worldSettings?.dotConfigs;
			Pawn animal = null;

			try
			{
				settings.dotSize = 64;
				settings.hidePawnLabelBelow = 0;
				settings.mouseOverShowsLabels = false;
				SetFastUi(8f, pawn.DrawPos + new Vector3(100f, 0f, 100f));

				SetRules(worldSettings, Array.Empty<DotConfig>());
				settings.dotStyle = DotStyle.VanillaDefault;
				var globalVanillaShows = Tools.ShouldShowLabel(pawn);

				settings.dotStyle = DotStyle.BetterSilhouettes;
				Caches.ClearMarkerState();
				var globalMarkerSuppresses = Tools.ShouldShowLabel(pawn) == false;

				var ruleMarker = new DotConfig { mode = DotStyle.BetterSilhouettes, showBelowPixels = 64, useInside = true, mouseReveals = false };
				settings.dotStyle = DotStyle.VanillaDefault;
				SetRules(worldSettings, new[] { ruleMarker });
				var ruleMarkerSuppresses = Tools.ShouldShowLabel(pawn) == false;

				var ruleVanilla = new DotConfig { mode = DotStyle.VanillaDefault, showBelowPixels = 64, useInside = true, mouseReveals = false };
				settings.dotStyle = DotStyle.BetterSilhouettes;
				SetRules(worldSettings, new[] { ruleVanilla });
				var ruleVanillaShows = Tools.ShouldShowLabel(pawn);

				var revealRule = new DotConfig { mode = DotStyle.BetterSilhouettes, showBelowPixels = 64, useInside = true, mouseReveals = true };
				SetFastUi(8f, pawn.DrawPos);
				SetRules(worldSettings, new[] { revealRule });
				var ruleMouseRevealShows = Tools.ShouldShowLabel(pawn);

				settings.hidePawnLabelBelow = 8;
				settings.mouseOverShowsLabels = false;
				SetFastUi(8f, pawn.DrawPos + new Vector3(100f, 0f, 100f));
				SetRules(worldSettings, new[] { ruleVanilla });
				var independentThresholdHides = Tools.ShouldShowLabel(pawn) == false;
				SetFastUi(9f, pawn.DrawPos + new Vector3(100f, 0f, 100f));
				Caches.ClearMarkerState();
				var aboveThresholdShows = Tools.ShouldShowLabel(pawn);

				settings.hidePawnLabelBelow = 0;
				var missingCustom = new DotConfig { mode = DotStyle.Custom, customDotStyle = "__CameraPlusMissingValidationAsset__", showBelowPixels = 64, useInside = true, mouseReveals = false };
				SetRules(worldSettings, new[] { missingCustom });
				var missingCustomShows = Tools.ShouldShowLabel(pawn);

				animal = SpawnAnimalFixture(map, pawn.Position);
				var animalPolicyLeavesVanillaLabel = false;
				if (animal != null)
				{
					settings.customNameStyle = LabelStyle.HideAnimals;
					settings.includeNotTamedAnimals = false;
					SetFastUi(8f, animal.DrawPos + new Vector3(100f, 0f, 100f));
					SetRules(worldSettings, new[] { ruleMarker });
					var decision = MarkerDecision.For(animal, ruleMarker);
					animalPolicyLeavesVanillaLabel = decision.defaultShow == false && decision.suppressVanilla == false && Tools.ShouldShowLabel(animal);
				}

				var success = globalVanillaShows
					&& globalMarkerSuppresses
					&& ruleMarkerSuppresses
					&& ruleVanillaShows
					&& ruleMouseRevealShows
					&& independentThresholdHides
					&& aboveThresholdShows
					&& missingCustomShows
					&& animalPolicyLeavesVanillaLabel;

				return new ValidationCase(success, new
				{
					pawn = pawn.LabelShortCap,
					animal = animal?.LabelShortCap,
					globalVanillaShows,
					globalMarkerSuppresses,
					ruleMarkerSuppresses,
					ruleVanillaShows,
					ruleMouseRevealShows,
					independentThresholdHides,
					aboveThresholdShows,
					missingCustomShows,
					animalPolicyLeavesVanillaLabel
				});
			}
			finally
			{
				if (animal != null && animal.Destroyed == false)
					animal.Destroy(DestroyMode.Vanish);
				settings.dotStyle = originalStyle;
				settings.dotSize = originalDotSize;
				settings.hidePawnLabelBelow = originalThreshold;
				settings.mouseOverShowsLabels = originalMouseReveal;
				settings.customNameStyle = originalAnimalStyle;
				settings.includeNotTamedAnimals = originalIncludeWild;
				if (worldSettings != null)
					worldSettings.dotConfigs = originalRules;
				Caches.ClearMarkerState();
			}
		}

		static ValidationCase ValidateFloatingText()
		{
			var settings = CameraPlusMain.Settings;
			var driver = Current.cameraDriverInt;
			var originalRootPos = driver.rootPos;
			var originalRootSize = driver.rootSize;
			var originalSuppress = settings.suppressFloatingText;
			var originalMouseReveal = settings.mouseOverShowsLabels;

			try
			{
				driver.SetRootPosAndSize(originalRootPos, CameraPlusSettings.maxRootInput);
				settings.suppressFloatingText = false;
				settings.mouseOverShowsLabels = false;
				var disabledShowsAtFarZoom = MoteMaker_ThrowText_Patch.Prefix(Vector3.zero);

				settings.suppressFloatingText = true;
				var enabledSuppressesAtFarZoom = MoteMaker_ThrowText_Patch.Prefix(Vector3.zero) == false;

				settings.mouseOverShowsLabels = true;
				SetFastUi(FastUI.CurUICellSize, Vector3.zero);
				var mouseRevealShowsNear = MoteMaker_ThrowText_Patch.Prefix(Vector3.zero);
				var mouseRevealSuppressesFar = MoteMaker_ThrowText_Patch.Prefix(new Vector3(100f, 0f, 100f)) == false;

				driver.SetRootPosAndSize(originalRootPos, CameraPlusSettings.minRootInput);
				settings.mouseOverShowsLabels = false;
				var enabledShowsAtClosestZoom = MoteMaker_ThrowText_Patch.Prefix(Vector3.zero);

				return new ValidationCase(
					disabledShowsAtFarZoom && enabledSuppressesAtFarZoom && mouseRevealShowsNear && mouseRevealSuppressesFar && enabledShowsAtClosestZoom,
					new { disabledShowsAtFarZoom, enabledSuppressesAtFarZoom, mouseRevealShowsNear, mouseRevealSuppressesFar, enabledShowsAtClosestZoom });
			}
			finally
			{
				settings.suppressFloatingText = originalSuppress;
				settings.mouseOverShowsLabels = originalMouseReveal;
				driver.SetRootPosAndSize(originalRootPos, originalRootSize);
			}
		}

		static ValidationCase ValidateDeadPawns()
		{
			var settings = CameraPlusMain.Settings;
			var originalStyle = settings.dotStyle;
			var originalThreshold = settings.hideDeadPawnsBelow;
			var corpse = ThingMaker.MakeThing(ThingDefOf.Human.race.corpseDef);

			try
			{
				settings.hideDeadPawnsBelow = 10;
				settings.dotStyle = DotStyle.VanillaDefault;
				SetFastUi(8f, Vector3.zero);
				var vanillaHidesBelow = DrawAllOverlaysPatch.Prefix(corpse) == false;
				SetFastUi(12f, Vector3.zero);
				var vanillaShowsAbove = DrawAllOverlaysPatch.Prefix(corpse);

				settings.dotStyle = DotStyle.BetterSilhouettes;
				SetFastUi(8f, Vector3.zero);
				var markerHidesBelow = DrawAllOverlaysPatch.Prefix(corpse) == false;
				SetFastUi(12f, Vector3.zero);
				var markerShowsAbove = DrawAllOverlaysPatch.Prefix(corpse);
				var nonCorpseUnaffected = DrawAllOverlaysPatch.Prefix(ThingMaker.MakeThing(ThingDefOf.Steel));

				return new ValidationCase(
					vanillaHidesBelow && vanillaShowsAbove && markerHidesBelow && markerShowsAbove && nonCorpseUnaffected,
					new { vanillaHidesBelow, vanillaShowsAbove, markerHidesBelow, markerShowsAbove, nonCorpseUnaffected });
			}
			finally
			{
				settings.dotStyle = originalStyle;
				settings.hideDeadPawnsBelow = originalThreshold;
			}
		}

		static ValidationCase ValidateAnimalEdges()
		{
			var map = Find.CurrentMap;
			var anchor = map.mapPawns.FreeColonistsSpawned.FirstOrDefault()?.Position ?? map.Center;
			var animal = SpawnAnimalFixture(map, anchor);
			if (animal == null)
				return new ValidationCase(false, new { error = "Could not create an animal fixture." });

			var settings = CameraPlusMain.Settings;
			var originalEdges = settings.edgeIndicators;
			var originalColoredEdges = settings.pawnColoredEdgeIndicators;
			var originalAnimalStyle = settings.customNameStyle;
			var originalIncludeWild = settings.includeNotTamedAnimals;

			try
			{
				settings.pawnColoredEdgeIndicators = true;
				settings.customNameStyle = LabelStyle.AnimalsDifferent;
				settings.includeNotTamedAnimals = true;
				var ruleOn = new DotConfig { mode = DotStyle.BetterSilhouettes, useEdge = true, fillColor = Color.clear, fillSelectedColor = Color.clear };
				var ruleOff = new DotConfig { mode = DotStyle.BetterSilhouettes, useEdge = false, fillColor = Color.clear, fillSelectedColor = Color.clear };

				settings.edgeIndicators = false;
				var globalOffRuleOn = MarkerDecision.For(animal, ruleOn);
				settings.edgeIndicators = true;
				var globalOnRuleOff = MarkerDecision.For(animal, ruleOff);
				var policy = AnimalMarkerPolicy.For(animal);
				var transparentFillUsesAnimalColor = DotTools.GetEdgeFillColor(animal, Color.clear);
				var explicitFill = new Color(0.2f, 0.3f, 0.4f, 1f);
				var explicitFillPreserved = DotTools.GetEdgeFillColor(animal, explicitFill);

				var success = globalOffRuleOn.edgeEnabled
					&& globalOnRuleOff.edgeEnabled == false
					&& policy.useAnimalEdgeColor
					&& transparentFillUsesAnimalColor.a > 0f
					&& Approximately(explicitFillPreserved.r, explicitFill.r)
					&& Approximately(explicitFillPreserved.g, explicitFill.g)
					&& Approximately(explicitFillPreserved.b, explicitFill.b);

				return new ValidationCase(success, new
				{
					animal = animal.LabelShortCap,
					globalOffRuleOn = globalOffRuleOn.edgeEnabled,
					globalOnRuleOff = globalOnRuleOff.edgeEnabled,
					policyUsesAnimalEdgeColor = policy.useAnimalEdgeColor,
					transparentFillUsesAnimalColor = transparentFillUsesAnimalColor.ToString(),
					explicitFillPreserved = explicitFillPreserved.ToString()
				});
			}
			finally
			{
				settings.edgeIndicators = originalEdges;
				settings.pawnColoredEdgeIndicators = originalColoredEdges;
				settings.customNameStyle = originalAnimalStyle;
				settings.includeNotTamedAnimals = originalIncludeWild;
				if (animal.Destroyed == false)
					animal.Destroy(DestroyMode.Vanish);
				Caches.ClearMarkerState();
			}
		}

		static ValidationCase ValidateMarkerCaches()
		{
			var pawn = Find.CurrentMap.mapPawns.AllPawnsSpawned.FirstOrDefault(candidate => candidate?.Drawer?.renderer != null);
			if (pawn == null)
				return new ValidationCase(false, new { error = "The test map has no renderable pawn." });

			var originalRotation = pawn.Rotation;
			var dotConfig = new DotConfig { mode = DotStyle.BetterSilhouettes, useInside = true, useEdge = true };
			try
			{
				pawn.Rotation = Rot4.East;
				Caches.RemovePawnMainColor(pawn);
				MarkerCache.Remove(pawn);
				Tools.GetMainColor(pawn);
				var eastMaterials = MarkerCache.MaterialFor(pawn, dotConfig, needInside: true, needEdge: true);
				var populated = Caches.cachedPawnMainColors.ContainsKey(pawn) && MarkerCache.cache.ContainsKey(pawn);

				SilhouetteUtility.NotifyGraphicDirty(pawn);
				var graphicDirtyInvalidates = Caches.cachedPawnMainColors.ContainsKey(pawn) == false
					&& MarkerCache.cache.ContainsKey(pawn) == false;

				Tools.GetMainColor(pawn);
				var refreshedEastMaterials = MarkerCache.MaterialFor(pawn, dotConfig, needInside: true, needEdge: true);
				var repopulated = Caches.cachedPawnMainColors.ContainsKey(pawn) && MarkerCache.cache.ContainsKey(pawn);

				pawn.Rotation = Rot4.West;
				var westMaterials = MarkerCache.MaterialFor(pawn, dotConfig, needInside: true, needEdge: true);
				var rotationRefreshes = ReferenceEquals(refreshedEastMaterials, westMaterials) == false
					&& refreshedEastMaterials.signature.westFacing == false
					&& westMaterials.signature.westFacing;

				return new ValidationCase(
					populated && graphicDirtyInvalidates && repopulated && rotationRefreshes,
					new { pawn = pawn.LabelShortCap, populated, graphicDirtyInvalidates, repopulated, rotationRefreshes });
			}
			finally
			{
				pawn.Rotation = originalRotation;
				SilhouetteUtility.NotifyGraphicDirty(pawn);
			}
		}

		static ValidationCase ValidateShortcuts()
		{
			var method = AccessTools.Method(typeof(CameraPlusSettings), "ShortcutDisabled");
			var emptyDisabled = method != null && (bool)method.Invoke(null, new object[] { new[] { KeyCode.None, KeyCode.None } });
			var oneModifierEnabled = method != null && (bool)method.Invoke(null, new object[] { new[] { KeyCode.LeftShift, KeyCode.None } }) == false;
			var disabledNote = "SettingsNote_ShortcutDisabled".Translate().ToString();
			var conflictNote = "SettingsNote_LoadSaveShortcutConflict".Translate().ToString();
			var loadHelp = "SettingsHelp_LoadShortcut".Translate().ToString();
			var saveHelp = "SettingsHelp_SaveShortcut".Translate().ToString();
			var copyExplainsDisabled = disabledNote.Contains("Disabled")
				&& loadHelp.Contains("disables")
				&& saveHelp.Contains("disables");
			var copyExplainsConflict = conflictNote.Contains("same modifiers") && loadHelp.Contains("loading runs first");

			return new ValidationCase(
				emptyDisabled && oneModifierEnabled && copyExplainsDisabled && copyExplainsConflict,
				new { emptyDisabled, oneModifierEnabled, copyExplainsDisabled, copyExplainsConflict, disabledNote, conflictNote });
		}

		static ValidationCase ValidateShiftZoom()
		{
			var update = AccessTools.Method(typeof(CameraDriver), nameof(CameraDriver.Update));
			var patchInfo = Harmony.GetPatchInfo(update);
			var cameraPlusTranspilerActive = patchInfo?.Transpilers.Any(patch => patch.owner == harmonyOwner) == true;
			var help = "SettingsHelp_ZoomToMouse".Translate().ToString();
			var helpDocumentsShift = help.IndexOf("Shift", StringComparison.OrdinalIgnoreCase) >= 0
				&& help.IndexOf("bypass", StringComparison.OrdinalIgnoreCase) >= 0;
			var shortcutModifiers = CameraPlusMain.Settings.cameraSettingsMod
				.Concat(CameraPlusMain.Settings.cameraSettingsLoad)
				.Concat(CameraPlusMain.Settings.cameraSettingsSave)
				.Select(key => key.ToString())
				.ToArray();

			return new ValidationCase(
				cameraPlusTranspilerActive && helpDocumentsShift,
				new { cameraPlusTranspilerActive, helpDocumentsShift, help, shortcutModifiers });
		}

		static ValidationCase ValidateObsoleteSettings()
		{
			const string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><CameraPlusSettings><currentVersion>3</currentVersion><stickyMiddleMouse>True</stickyMiddleMouse><zoomedInPercent>7</zoomedInPercent></CameraPlusSettings>";
			var originalMinRootResult = CameraPlusSettings.minRootResult;
			var originalMaxRootResult = CameraPlusSettings.maxRootResult;
			try
			{
				var loaded = Tools.ScribeFromString<CameraPlusSettings>(xml);
				var rewritten = loaded == null ? string.Empty : Tools.ScribeToString(loaded);
				var staleFieldAbsent = typeof(CameraPlusSettings).GetField("stickyMiddleMouse", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null;
				var oldXmlLoads = loaded != null && Approximately(loaded.zoomedInPercent, 7f);
				var obsoleteElementRemoved = rewritten.Contains("stickyMiddleMouse") == false;

				var onGui = AccessTools.Method(typeof(CameraDriver), nameof(CameraDriver.CameraDriverOnGUI));
				var onGuiOwners = PatchOwners(onGui);
				var vanillaMiddleDragPathUnpatched = onGuiOwners.Contains(harmonyOwner) == false;

				return new ValidationCase(
					staleFieldAbsent && oldXmlLoads && obsoleteElementRemoved && vanillaMiddleDragPathUnpatched,
					new { staleFieldAbsent, oldXmlLoads, obsoleteElementRemoved, vanillaMiddleDragPathUnpatched, onGuiPatchOwners = onGuiOwners });
			}
			finally
			{
				if (Scribe.mode != LoadSaveMode.Inactive)
					Scribe.ForceStop();
				CameraPlusSettings.minRootResult = originalMinRootResult;
				CameraPlusSettings.maxRootResult = originalMaxRootResult;
			}
		}

		static void SetRules(CameraSettings settings, IEnumerable<DotConfig> rules)
		{
			if (settings != null)
				settings.dotConfigs = rules.ToList();
			Caches.ClearMarkerState();
		}

		static Pawn SpawnAnimalFixture(Map map, IntVec3 anchor)
		{
			var kind = DefDatabase<PawnKindDef>.AllDefsListForReading.FirstOrDefault(def => def?.RaceProps?.Animal == true && def.RaceProps.IsFlesh);
			if (kind == null)
				return null;

			var pawn = PawnGenerator.GeneratePawn(kind, Faction.OfPlayer);
			var cell = CellFinder.RandomClosewalkCellNear(anchor, map, 6);
			GenSpawn.Spawn(pawn, cell, map);
			return pawn;
		}

		static void SetFastUi(float cellSize, Vector3 mouseMapPosition)
		{
			AccessTools.Field(typeof(FastUI), "curUICellSize")?.SetValue(null, cellSize);
			AccessTools.Field(typeof(FastUI), "lastUpdateFrameForCurUICellSize")?.SetValue(null, Time.frameCount);
			AccessTools.Field(typeof(FastUI), "mouseMapPosition")?.SetValue(null, mouseMapPosition);
			AccessTools.Field(typeof(FastUI), "lastUpdateFrameForMouseMapPosition")?.SetValue(null, Time.frameCount);
		}

		static string[] PatchOwners(MethodBase method)
		{
			var info = method == null ? null : Harmony.GetPatchInfo(method);
			if (info == null)
				return Array.Empty<string>();

			return info.Prefixes
				.Concat(info.Postfixes)
				.Concat(info.Transpilers)
				.Concat(info.Finalizers)
				.Select(patch => patch.owner)
				.Where(owner => string.IsNullOrEmpty(owner) == false)
				.Distinct()
				.OrderBy(owner => owner)
				.ToArray();
		}

		static bool Approximately(float a, float b)
			=> Math.Abs(a - b) <= 0.001f;

		sealed class ValidationCase
		{
			public bool success { get; }
			public object details { get; }

			public ValidationCase(bool success, object details)
			{
				this.success = success;
				this.details = details;
			}
		}

		sealed class CameraRateSample
		{
			public float rootSize { get; set; }
			public float orthographicSize { get; set; }
			public float keyRate { get; set; }
			public float edgeRate { get; set; }
			public float expectedKeyRate { get; set; }
			public float expectedEdgeRate { get; set; }
		}
	}
}
