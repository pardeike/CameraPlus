# Performance Review Baseline

This document lists the runtime areas most likely to matter in the upcoming performance review. It is not a finding list yet; it is a map of where to measure and what behavior must be preserved.

## Primary Hot Paths

`DynamicDrawManager.DrawDynamicThings` postfix:

- Calls `DotDrawer.DrawDots(map)` during dynamic drawing.
- Enumerates `map.mapPawns.AllPawnsSpawned`.
- Performs hidden/fog filtering, rule lookup, color lookup, material lookup, edge marker math, and mesh draw calls.
- This is the first place to profile on large colonies, animal-heavy maps, raids, and vehicle-heavy maps.

Pawn and label suppression prefixes:

- `PawnRenderer.RenderPawnAt`
- `Vehicles.VehicleRenderer:RenderPawnAt`
- `SelectionDrawer.DrawSelectionBracketFor`
- `PawnUIOverlay.DrawPawnGUIOverlay`
- `SilhouetteUtility.ShouldDrawSilhouette`
- `GenMapUI.DrawPawnLabel`
- `GenMapUI.DrawThingLabel`

These run as RimWorld tries to draw or label things. Keep prefix decisions cheap and avoid allocations.

Camera patches:

- `CameraDriver.Update`
- `CameraDriver.ApplyPositionToGameObject`
- `CameraDriver.CurrentViewRect`
- `CameraDriver.CurrentZoom`

These are lower volume than per-pawn rendering but central to user feel. Regressions show up immediately as zoom drift, clipping, culling, wrong mouse anchoring, or odd movement speed.

Settings and editor UI:

- `Dialog_Customization.DoWindowContents()` clears `Caches.dotConfigCache` every tick while the rule editor is open.
- The editor performs per-row layout and color/mode UI work.
- This is less critical during gameplay, but it can matter for users with many custom rules.

## Current Cache Behavior

`FastUI` is frame-scoped and avoids repeating RimWorld UI coordinate calls in the same frame.

`Caches.dotConfigCache` and `Caches.shouldShowLabelCache` are quota-based. Each cached entry is refreshed after 60 retrievals, not by tick or frame. This can reduce repeated rule scans but can also keep stale rule decisions briefly after state changes.

`MarkerCache` holds per-pawn Unity materials and refreshes entries after 300 retrievals. It destroys old materials through `MaterialAllocator.Destroy()`.

`cachedMainColors` stores sampled texture colors by pawn runtime type and body graphic path. This avoids repeated texture readback/downsampling after the first sample for a graphic.

`cachedCameraDelegates` stores reflection-discovered optional integration methods by pawn runtime type.

## Allocation And Expensive Work Candidates

Likely candidates to verify with profiling:

- LINQ in `DotDrawer.DrawDots()` and rule matching paths.
- `DotConfig.conditions.All(...)` for every uncached rule lookup.
- `Tools.GetMainColor()`, especially texture downsampling and pixel grouping on cache misses.
- `MarkerCache.MaterialFor()`, especially material creation/destruction cadence and cache key lifetime.
- `CameraDelegates` reflection for new pawn runtime types.
- Repeated `UI.MapToUIPosition()` and `UI.UIToMapPosition()` calls in edge-marker calculations.
- File watcher reload behavior for custom marker PNGs.

## Correctness Constraints For Optimization

Do not break these behaviors while optimizing:

- Zoom-to-mouse should keep the map point under the cursor stable unless Shift is held or the setting is off.
- Effective zoom limits come from `zoomedInPercent`, `zoomedOutPercent`, and `exponentiality`.
- Marker display is controlled by the first matching `DotConfig` rule.
- Rule matching is an AND across all conditions in a rule.
- Mouse proximity can reveal labels and suppress markers.
- Edge indicators are separate from in-map markers.
- Dead pawn hiding affects body rendering and corpse forbidden overlays.
- Animal display must respect `LabelStyle` and `includeNotTamedAnimals`.
- `skipCustomRendering` must allow other mods to bypass CameraPlus custom drawing.
- Custom marker PNG files in the CameraPlus save-data folder must be reloadable without restarting RimWorld.
- Save-specific marker rules live in `CameraSettings`; default rules for new games live in `CameraPlusDefaultRules.xml`.

## Suggested Measurement Passes

Use at least these scenarios before and after performance changes:

- Empty colony, default settings, close/middle/far/furthest zoom.
- Large late-game colony with many colonists, animals, mechs, corpses, and items.
- Raid/combat scene with drafted colonists, downed pawns, mental states, and many labels.
- Many custom marker rules, including text predicates for hediffs, apparel, inventory, equipment, weapon, pawn name, and faction name.
- Custom PNG marker modes.
- Long session with map changes to check material/cache cleanup.
- Optional compat pass with Vehicle Framework, Save Our Ship 2, and Dubs Performance Analyzer when available.

## Build Verification

Minimum verification after source changes:

```sh
dotnet build Source/CameraPlus.csproj -c Release
```

Runtime verification should include:

- open game to main menu and confirm asset bundle load has no errors.
- load a save and test zoom-to-mouse, camera shake setting, zoom limits, and edge scrolling.
- check map markers and edge markers at several zoom levels.
- open marker-rule editor, change a rule, and verify it takes effect immediately.
- save/load a customization preset.
- save/load camera views with the configured shortcuts.
