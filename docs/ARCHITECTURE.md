# CameraPlus Architecture

This document describes the current mod shape as found in the source tree. It is intended as the baseline for later performance review and refactoring, not as a desired future architecture.

## Repository Layout

- `Source/CameraPlus.csproj` builds the mod assembly for RimWorld 1.6.
- `Source/*.cs` contains all runtime code, Harmony patches, settings UI, marker-rule editing UI, data models, and caches.
- `1.1` through `1.6` contain versioned RimWorld assembly outputs. The current C# project writes `1.6/Assemblies/CameraPlus.dll`.
- `About`, `LoadFolders.xml`, `Defs`, `Languages`, `Textures`, `Sounds`, and `Resources` are the RimWorld mod payload.
- `Resources/{Win64,Linux,MacOS}/effects` are Unity asset bundles loaded at runtime for color picker materials and the bordered marker shader.
- `Originals` contains source art and the Unity project used to generate the effects asset bundle.

## Startup

`CameraPlusMain` in `Source/Main.cs` is the RimWorld `Mod` entry point.

Startup sequence:

1. The constructor reads global mod settings via `GetSettings<CameraPlusSettings>()`.
2. It installs all Harmony patches with the id `net.pardeike.rimworld.mod.camera+`.
3. It installs cross-promotion support through `Brrainz.RimWorld.CrossPromotion`.
4. A later `UIRoot_Entry.Init` postfix in `Assets.LoadAssetBundle()` loads the platform-specific asset bundle, initializes default marker rules, creates the custom marker folder watcher, and shows the version notice when needed.

The mod has no separate composition root. Most runtime state is static and is reached through `CameraPlusMain.Settings`, `CameraPlusMain.orthographicSize`, `CameraSettings.settings`, and static helper/cache classes.

## Build Model

The project targets `net472`, which matches RimWorld's managed runtime expectations. `Krafs.Rimworld.Ref` supplies reference assemblies so the project can build without referencing a local RimWorld installation directly.

The build uses `TaskPubliciser` to publicise `Assembly-CSharp.dll` from the `Krafs.Rimworld.Ref` package and then replaces the original RimWorld reference with the publicised assembly. This is why the code can access internal fields and methods such as `CameraDriver.rootSize`, `cameraDriverInt`, `gameInt`, and renderer internals.

See [BUILD_AND_DEPENDENCIES.md](BUILD_AND_DEPENDENCIES.md) for the exact commands and package versions.

## Runtime Data Model

`CameraPlusSettings` in `Source/Settings.cs` is the global mod settings object. It owns zoom limits, zoom curve shape, dolly/edge scroll tuning, label and marker defaults, shortcuts, and global marker-style defaults.

`CameraSettings` in `Source/CameraSettings.cs` is a `WorldComponent`. It stores the active `List<DotConfig>` marker rules for the current save. The static `CameraSettings.settings` pointer is refreshed by a `World.FinalizeInit` postfix.

`SavedViews` in `Source/SavedViews.cs` is a `MapComponent`. It stores nine `RememberedCameraPos` entries per map for the `modifier + 1..9` load/save view hotkeys.

`DotConfig` in `Source/DotConfig.cs` represents one marker rule. A rule contains:

- `conditions`: all must match the pawn.
- `mode`: off, vanilla, classic dots, silhouettes, or custom marker image.
- colors for normal and selected states.
- map marker, edge marker, mouse reveal, size threshold, relative size, and outline settings.

`ConditionTag`, `BoolTag`, and `TextTag` define the marker-rule predicate system. The concrete predicates in `BoolTags.cs` and `TextTags.cs` cover RimWorld pawn type, faction, state, equipment, health, and text/name matching.

## Camera Behavior

Camera behavior is mostly in `Source/Main.cs` and `Source/Tools.cs`.

The core zoom mapping is:

1. RimWorld still changes `CameraDriver.rootSize` in its normal input range.
2. CameraPlus maps that input through `Tools.LerpRootSize()`.
3. `CameraDriver.ApplyPositionToGameObject` is transpiled so the Unity camera receives the mapped orthographic size.
4. The patch also adjusts camera height, clipping planes, field of view, and movement speed settings.

Important camera patches:

- `CameraDriver.Update` rewrites root-size assignment so zoom-to-mouse can preserve the map position under the cursor.
- `CameraDriver.CurrentZoom` remaps RimWorld's zoom enum decisions to the extended zoom range.
- `CameraDriver.CurrentViewRect` replaces uses of raw root size with the mapped size.
- `CameraDriver.CalculateCurInputDollyVect` scales edge-scroll input.
- `TimeControls.DoTimeControlsGUI` handles shortcuts.
- `Game.UpdatePlay`, `TickManager.TogglePaused`, and `UIRoot_Play.UIRootOnGUI` implement the pause-hold snapback feature.

## Marker And Label Rendering

Marker rendering is split across three layers:

- `MarkerDecision` computes the per-pawn marker decision once per Unity frame.
- `DotTools` decides whether vanilla pawn drawing, selection brackets, pawn labels, and silhouettes should continue.
- `DotDrawer` draws CameraPlus edge indicators and map markers in a `DynamicDrawManager.DrawDynamicThings` postfix.
- `MarkerCache` builds and recycles per-pawn `Material` instances for dots, silhouettes, and custom marker textures.

The normal draw flow is:

1. RimWorld reaches dynamic drawing for the current map.
2. `DotDrawer.DrawDots(map)` enumerates `map.mapPawns.AllPawnsSpawned`.
3. Each pawn is filtered for fog/invisibility.
4. `MarkerDecisionCache` fetches the first matching `DotConfig` through `Caches.dotConfigCache` and computes marker, edge, vanilla-suppression, zoom-threshold, and mouse-reveal decisions.
5. If no edge marker or in-map marker can be drawn, `DotDrawer` skips color and material work for that pawn.
6. `DotTools.GetMarkerColors()` resolves rule colors, external mod colors, or default pawn colors.
7. `MarkerCache.MaterialFor(pawn, dotConfig)` creates or refreshes the marker materials.
8. `DotDrawer` draws edge markers for off-screen pawns and in-map markers when zoom thresholds apply.

Vanilla rendering suppression is intentional:

- Pawn bodies, vehicle pawns, selection brackets, pawn UI overlays, and RimWorld silhouettes can be skipped when CameraPlus markers are active.
- Pawn and thing labels can be hidden when zoomed out, unless the mouse is close enough to reveal them.
- `CameraPlusMain.skipCustomRendering` is a public escape hatch other mods can set temporarily to bypass CameraPlus drawing decisions.
- Perf builds can additionally patch `PawnRenderer.DynamicDrawPhaseAt` to skip vanilla renderer phases for marker-replaced pawns. That experiment is intentionally behind the `CAMERAPLUS_PERF` compile gate.

## Caches

`FastUI` caches expensive UI coordinate and cell-size reads per frame.

`Caches.dotConfigCache` caches the first matching rule per pawn for 60 reads, keyed by `thingIDNumber`.

`Caches.shouldShowLabelCache` caches label visibility decisions for 60 reads.

`Caches.cachedMainColors` stores sampled main pawn colors by pawn type and body graphic path.

`Caches.cachedCameraDelegates` stores reflection-discovered external integration delegates by pawn runtime type.

`MarkerDecisionCache` stores the computed marker decision by `thingIDNumber` for the current Unity frame. It exists so the dynamic draw postfix and the vanilla-rendering suppression prefixes can share the same rule lookup and zoom/mouse decision work.

`MarkerCache.cache` stores `Material` objects by `Pawn`. Entries are reused while their marker mode, custom marker name, and outline factor still match the current rule/settings state. It owns `MaterialAllocator.Destroy()` cleanup when entries are invalidated or the cache is cleared. Custom marker PNG reloads clear this cache so stale custom marker materials are not reused.

## Settings And Editor UI

`CameraPlusSettings.DoWindowContents()` draws the main mod settings UI. It exposes zoom limits, zoom curve, movement tuning, marker style defaults, label thresholds, animal behavior, shortcut editor access, and marker-rule editor access.

The marker-rule editor is `Dialog_Customization`. It is a custom table-like editor for `DotConfig` rows. It supports:

- condition tag editing.
- mode selection, including custom PNG marker files.
- normal and selected color editing.
- map marker, edge marker, and mouse reveal toggles.
- per-rule zoom threshold, relative size, and outline values.
- row drag/reorder, delete, duplicate, copy, and paste.
- load/save of rule presets.

Related dialogs:

- `Dialog_AddTag` lists available predicate tags.
- `Dialog_TagEdit` edits text predicates and negation.
- `Dialog_ColorPicker` provides HSV color editing and persistent swatches.
- `Dialog_CustomizationList_Load` and `Dialog_CustomizationList_Save` load/save XML presets under the CameraPlus config folder.
- `Dialog_Shortcuts` and `Dialog_AskForKey` edit the keyboard shortcuts.
- `Dialog_NewVersion` is a first-run-after-version-bump notice.

## Assets And Player Files

Static textures in `Textures` are loaded through RimWorld `ContentFinder<Texture2D>`.

The platform-specific `Resources/*/effects` asset bundles provide:

- `ColorBed` material.
- `Hues` material.
- `Bordered` shader.

Player custom marker PNG files live in `GenFilePaths.FolderUnderSaveData("CameraPlus")`. A `FileSystemWatcher` reloads PNG files into `Assets.customMarkers`.

Player rule preset XML files also live in the same CameraPlus folder. The default rules file is `CameraPlusDefaultRules.xml` in `GenFilePaths.ConfigFolderPath`.

Color swatches are stored in `CameraPlusColors.txt` under `GenFilePaths.ConfigFolderPath`.

## External Mod Integration

CameraPlus has explicit compatibility paths:

- Harmony dependency is declared in `About/About.xml`.
- Optional Vehicle Framework support patches `Vehicles.VehicleRenderer:RenderPawnAt` by reflection when present.
- Optional Save Our Ship 2 support patches background mesh recalculation and material state when present.
- A Dubs Performance Analyzer name-drawing patch is disabled by patching `Analyzer.Fixes.H_DrawNamesFix:Prefix`.
- External pawn types can expose `CameraPlusSupport.Methods.GetCameraPlusColors(Pawn)` and `GetCameraPlusMarkers(Pawn)` in their own assembly. `CameraDelegates` discovers these by reflection.

## Main Architectural Risks

- Harmony transpilers depend on RimWorld method IL shape and publicised internals. API updates can compile while still changing runtime semantics.
- Rendering decisions are distributed across `Main.cs`, `DotTools.cs`, and `DotDrawer.cs`, so a marker change can also change labels, pawn bodies, overlays, and other mods' patches.
- Most caches are static and have no central lifecycle reset beyond targeted clear/expiry logic.
- `DotDrawer.DrawDots()` scans every spawned pawn during dynamic drawing.
- `MarkerCache` uses `Pawn` object keys and per-pawn Unity materials, so cleanup behavior matters for long sessions and large maps.
- Settings UI and runtime settings share mutable lists directly; editor interactions take effect immediately.
