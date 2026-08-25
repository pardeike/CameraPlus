# CameraPlus Architecture

This document describes the current mod shape as found in the source tree. It is intended as the baseline for later performance review and refactoring, not as a desired future architecture.

## Repository Layout

- `Source/CameraPlus.csproj` builds the mod assembly for RimWorld 1.6.
- `Source/*.cs` contains all runtime code, Harmony patches, settings UI, marker-rule editing UI, data models, and caches.
- `1.1` through `1.6` contain versioned RimWorld assembly outputs. The current C# project writes `1.6/Assemblies/CameraPlus.dll`.
- `About`, `LoadFolders.xml`, `Defs`, `Languages`, `Textures`, and `Sounds` are shared RimWorld mod payload.
- Root `Resources/{Win64,Linux,MacOS}/effects` and `Originals/Effects` are frozen legacy assets for RimWorld 1.5 and earlier. Those bundles still contain the legacy color-picker materials and bordered shader expected by the older assemblies.
- `1.6/Resources/{Win64,Linux,MacOS}/effects` contains the 1.6-only marker shaders. `Originals/Effects-1.6` is their isolated Unity source project.

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

- `conditions`: all must match the pawn. An empty condition list matches every pawn, so a tagless rule is a global catch-all; the first matching rule wins. Null entries and conditions that fail on an unusual modded pawn do not match that rule instead of breaking the draw pass.
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
- CameraPlus writes independent keyboard and screen-edge rates into
  `CameraDriver.config`; it does not scale RimWorld's combined input vector.
- `TimeControls.DoTimeControlsGUI` handles shortcuts.
- `Game.UpdatePlay`, `TickManager.TogglePaused`, and `UIRoot_Play.UIRootOnGUI` implement the pause-hold snapback feature.

## Marker And Label Rendering

Marker rendering is split across three layers:

- `MarkerDecision` computes the per-pawn marker decision once per Unity frame and resolves the visible map target: normally the pawn itself, or its spawned `PawnFlyer` while in flight.
- `DotTools` decides whether vanilla pawn drawing, ordinary selection brackets, pawn labels, and silhouettes should continue. Selection calls carrying a mod-supplied material pass through because CameraPlus has no equivalent marker state for them.
- `DotDrawer` draws CameraPlus edge indicators and map markers in a `DynamicDrawManager.DrawDynamicThings` postfix.
- `EdgeUIInsets` compares visible marker rectangles with independently animated vanilla-interface groups. Top-left messages move with the resource list, the mouseover readout keeps its own bottom-left channel, and alerts, status text, controls, and gizmos share one bottom-right channel so each visual block moves as a unit. Markers outside a group's opposite-axis span do not move it.
- `MarkerCache` builds and recycles per-pawn `Material` instances for dots, silhouettes, and custom marker textures. It prepares one guarded, non-mipmapped GPU copy per source texture, generates padded radial outline masks per source texture and outline width, and measures the prepared textures once so clearance follows visible pixels rather than transparent quad padding.

The normal draw flow is:

1. RimWorld reaches dynamic drawing for a map. CameraPlus draws only when that manager belongs to the current map, avoiding duplicate current-map markers when another mod renders a secondary map.
2. `DotDrawer.DrawDots(map)` enumerates the map's registered dynamic drawables and accepts ordinary `Pawn` instances plus `PawnFlyer` instances that currently hold a pawn. Using RimWorld's draw registry preserves off-screen edge markers while respecting mods that deregister hidden pawns instead of changing the vanilla fog grid.
3. Each represented pawn is filtered for fog/invisibility at its visible map target. A flyer-held pawn uses the flyer's interpolated draw position for marker placement, clipping, and mouse reveal.
4. `MarkerDecisionCache` fetches the first matching `DotConfig` through `Caches.dotConfigCache` and computes marker, edge, vanilla-suppression, zoom-threshold, and mouse-reveal decisions.
5. If no edge marker or in-map marker can be drawn, `DotDrawer` skips color and material work for that pawn.
6. `DotTools.GetMarkerColors()` resolves rule colors, external mod colors, or default pawn colors.
7. `MarkerCache.MaterialFor(pawn, dotConfig)` creates or refreshes the marker materials.
8. `DotDrawer` draws edge markers for off-screen pawns and in-map markers when zoom thresholds apply.
9. For all four edges, `DotDrawer` records each marker's allocation-free logical-screen rectangle from its cached visible texture bounds. `EdgeUIInsets` compares those rectangles with the vanilla UI groups observed during the previous GUI pass and updates only an overlapping group's horizontal and/or vertical animation target. Each axis is capped at that marker's normal single-edge depth, so moving around a corner can activate the second axis without amplifying the first. A marker partly outside the physical screen adds 6 logical pixels of clearance from the moved UI. Bottom groups use the top of RimWorld's main-tab bar as their reference edge so that the bar's reserved height is not counted twice.

Vanilla rendering suppression is intentional:

- Pawn bodies, vehicle pawns, selection brackets, pawn UI overlays, and RimWorld silhouettes can be skipped when CameraPlus markers are active.
- Pawn and thing labels can be hidden when zoomed out, unless the mouse is close enough to reveal them.
- General map floating text can be suppressed independently outside RimWorld's
  closest zoom level, with the global mouse-reveal setting providing a nearby
  exception.
- `CameraPlusMain.skipCustomRendering` is a cooperative public escape hatch other mods can set temporarily to bypass CameraPlus drawing decisions. Callers must restore the global flag from a `finally` block; CameraPlus cannot infer ownership or safely reset another mod's render scope.
- Perf builds can additionally patch `PawnRenderer.DynamicDrawPhaseAt` to skip vanilla renderer phases for marker-replaced pawns. That experiment is intentionally behind the `CAMERAPLUS_PERF` compile gate.

## Caches

`FastUI` caches expensive UI coordinate and cell-size reads per frame.

`EdgeUIInsets` reuses one marker-rectangle list and holds one frame of passive vanilla-UI observations plus four short-lived, two-axis smoothed offsets. It does not cache pawns or textures. The rectangle calculation uses the already-known orthographic view rectangle and cached marker bounds, so steady-state rendering adds no per-marker camera projections, texture transfers, or allocations. When interface clearance is disabled, marker rectangles and mouseover rows are not inspected, and visible texture bounds remain lazy until the feature is enabled.

`Caches.dotConfigCache` caches the first matching rule per pawn for 60 reads, keyed by `thingIDNumber`.

`Caches.cachedMainColors` shares sampled colors between identical body graphics. `Caches.cachedPawnMainColors` provides the steady-state per-pawn fast path; RimWorld's graphics-dirty notification, despawn/destruction, and map removal invalidate it. `MarkerCache` keeps flyer-held pawns alive while their spawned flyer remains on a loaded map.

`Caches.cachedCameraDelegates` stores reflection-discovered external integration delegates by pawn runtime type.

`MarkerDecisionCache` stores the computed marker decision by pawn reference for the current Unity frame. It exists so the dynamic draw postfix and the vanilla-rendering suppression prefixes can share the same rule lookup and zoom/mouse decision work without temporary or malformed pawn identifiers colliding.

`MarkerCache.cache` stores `Material` objects by `Pawn`. Entries are reused while their marker mode, custom marker name, outline factor, and silhouette-facing direction still match. RimWorld's graphics-dirty notification invalidates ordinary pawn entries, so steady-state hits do not rebuild silhouette inputs; integrations that provide dynamic marker textures retain per-frame texture validation. Outline values are normalized before they become cache keys or GPU dimensions, and generated textures have fixed dimension and pixel-count limits in addition to the GPU's own limit. Its shared texture cache stores a guarded, non-mipmapped GPU copy per source texture to prevent sub-pixel edge bleed, while its outline cache stores GPU-generated `RenderTexture` masks by source texture and outline width. Normal marker draws sample those two prepared textures once each. A one-time alpha-bounds readback per source texture and outline width is cached alongside them; no readback occurs during steady-state marker drawing. Changing rule outline values releases the old masks and their material references while retaining reusable source copies. Full cache clears additionally release source copies, silhouette textures, and the generator material. Custom marker PNG reloads use the full clear so stale custom marker resources are not reused.

## Settings And Editor UI

`CameraPlusSettings.DoWindowContents()` draws the main mod settings UI. It exposes zoom limits, zoom curve, movement tuning, marker style defaults, label thresholds, animal behavior, shortcut editor access, and marker-rule editor access.

The edge settings include an enabled-by-default option that keeps RimWorld's messages, resource and mouseover readouts, global controls, alerts, letters, and selected-object gizmos clear of markers. Messages and resources move together in the top-left, the mouseover readout keeps its own bottom-left channel, and the right-side alerts, status text, controls, and gizmos move as one visual block. Side markers are compared with each group's observed vertical span, while top and bottom markers are compared with its observed horizontal span. Each group follows the marker's final visible edge after edge-distance settings, per-rule size, and interface scale, then eases back independently on each axis. A marker in unused space outside a group's opposite-axis span has no effect. When settings place part of a marker outside the screen, the moved UI keeps an additional 6 logical pixels of clearance from its visible inner edge.

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
- `Dialog_ColorPicker` provides HSV color editing through RimWorld's native color wheel, a value strip, alpha control, and persistent swatches.
- `Dialog_CustomizationList_Load` and `Dialog_CustomizationList_Save` load/save XML presets under the CameraPlus config folder.
- `Dialog_Shortcuts` and `Dialog_AskForKey` edit the keyboard shortcuts.
- `Dialog_NewVersion` is a first-run-after-version-bump notice.

## Assets And Player Files

Static textures in `Textures` are loaded through RimWorld `ContentFinder<Texture2D>`.

The platform-specific `1.6/Resources/*/effects` asset bundles contain the `Bordered` marker shader and the hidden `OutlineMask` generator shader. `OutlineMask` prepares guarded source copies and builds each radial mask once with jump-flood GPU passes; `Bordered` then composites fill over outline with premultiplied alpha to avoid edge halos and quad-edge bleed. The 1.6 assembly loads this versioned path directly so rebuilding it cannot alter the shared bundles used by older assemblies.

Player custom marker PNG files live in `GenFilePaths.FolderUnderSaveData("CameraPlus")`. A `FileSystemWatcher` reloads PNG files into `Assets.customMarkers`.

Player rule preset XML files also live in the same CameraPlus folder. `CameraPlusDefaultRules.xml` in `GenFilePaths.ConfigFolderPath` is the user-editable preset for new games; the built-in `CameraSettings.defaultDefaultConfig` rules remain the factory baseline used by **Restore defaults** in every rules dialog.

Color swatches are stored in `CameraPlusColors.txt` under `GenFilePaths.ConfigFolderPath`.

## External Mod Integration

CameraPlus has explicit compatibility paths:

- Harmony dependency is declared in `About/About.xml`.
- Optional Vehicle Framework support patches the draw phase of `Vehicles.Rendering.VehicleRenderer.DynamicDrawPhaseAt` by reflection when present. It suppresses only the vehicle body draw; Vehicle Framework's surrounding hitbox and component-overlay work still runs.
- Optional Save Our Ship 2 support patches background mesh recalculation and material state when present.
- A Dubs Performance Analyzer name-drawing patch is disabled by patching `Analyzer.Fixes.H_DrawNamesFix:Prefix`.
- External pawn types can expose static `CameraPlusSupport.Methods.GetCameraPlusColors(Pawn)` and `GetCameraPlusMarkers(Pawn)` methods in their own assembly, returning exactly two colors or textures. A null result requests CameraPlus defaults. Invalid results warn once and fall back; a throwing provider is disabled for that pawn type so it cannot break later draw frames. Flyer-held pawns are passed to providers as the same temporarily unspawned `Pawn`, so integrations that need the visible map should use held/parent state rather than assume `Pawn.Map` is non-null.
- [HARMONY_COMPATIBILITY_REVIEW.md](HARMONY_COMPATIBILITY_REVIEW.md) records the 2026-05-17 decompiler/GitHub compatibility pass over these patch targets.

## Main Architectural Risks

- Harmony transpilers depend on RimWorld method IL shape and publicised internals. API updates can compile while still changing runtime semantics.
- Rendering decisions are distributed across `Main.cs`, `DotTools.cs`, and `DotDrawer.cs`, so a marker change can also change labels, pawn bodies, overlays, and other mods' patches.
- Most caches are static and have no central lifecycle reset beyond targeted clear/expiry logic.
- `DotDrawer.DrawDots()` scans every registered dynamic drawable and filters it to pawn and pawn-flyer marker candidates during dynamic drawing.
- `MarkerCache` uses `Pawn` object keys and per-pawn Unity materials, so cleanup behavior matters for long sessions and large maps.
- Settings UI and runtime settings share mutable lists directly; editor interactions take effect immediately.
