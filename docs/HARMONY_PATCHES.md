# Harmony Patch Inventory

This inventory is grouped by subsystem. It covers all current Harmony patches in `Source`.

## Startup And State

| Target | File | Patch | Purpose | Risk |
| --- | --- | --- | --- | --- |
| `UIRoot_Entry.Init` | `Assets.cs` | Postfix | Loads platform asset bundle, initializes default marker rules, watches custom marker PNGs, and shows version notice. | Startup ordering, asset load failures, file watcher churn. |
| `World.FinalizeInit` | `CameraSettings.cs` | Postfix | Refreshes `CameraSettings.settings` to the current world's component. | Static pointer correctness after loading worlds. |
| `Widgets.WidgetsOnGUI` | `CheckBoxPaintingObserver.cs` | Postfix | Observes RimWorld checkbox-painting state so the customization dialog can disable dragging while checkbox painting is active. | GUI event timing only. |

## Camera Controls

| Target | File | Patch | Purpose | Risk |
| --- | --- | --- | --- | --- |
| `CameraDriver.Update` | `Main.cs` | Prefix and transpiler | Clears camera shake when disabled and replaces `RootSize` assignment with zoom-to-mouse-aware setter. | IL-shape dependency; mod conflicts on `CameraDriver.Update`; null-driver conflict logging. |
| `TimeControls.DoTimeControlsGUI` | `Main.cs` | Prefix | Handles CameraPlus shortcut keys every GUI pass. | Input handling can consume events used by other UI. |
| `CameraDriver.CalculateCurInputDollyVect` | `Main.cs` | Postfix | Scales screen-edge panning by current effective zoom. | Affects all edge-scroll movement. |
| `CameraDriver.CurrentZoom` getter | `Main.cs` | Prefix | Replaces vanilla zoom enum mapping with mapping over CameraPlus' extended zoom range. | Downstream code relying on vanilla zoom bands changes behavior. |
| `CameraDriver.ApplyPositionToGameObject` | `Main.cs` | Transpiler | Applies mapped orthographic size, camera height, clipping planes, FOV, and movement-speed config. | High-risk transpiler on a core camera method. |
| `CameraDriver.CurrentViewRect` getter | `Main.cs` | Transpiler | Uses effective mapped root size for view rect calculations. | View culling and UI-to-map calculations can shift if wrong. |

## Marker And Label Rendering

| Target | File | Patch | Purpose | Risk |
| --- | --- | --- | --- | --- |
| `DynamicDrawManager.DrawDynamicThings` | `Main.cs` | Postfix | Draws CameraPlus dots, silhouettes, custom markers, and edge indicators for current map. | Hot path; scans spawned pawns. |
| `MoteMaker.ThrowText(Vector3, Map, string, Color, float)` | `Main.cs` | Prefix | Suppresses floating text when zoomed out and labels are hidden, except close mouse reveal cases. | User-visible feedback can disappear if thresholds are wrong. |
| `OverlayDrawer.RenderForbiddenOverlay` | `Main.cs` | Prefix | Hides corpse forbidden overlays when dead pawns are hidden by zoom threshold. | Overlay suppression can hide information. |
| `GenMapUI.DrawThingLabel(Vector2, string, Color)` | `Main.cs` | Prefix and transpiler | Hides thing labels by zoom/mouse rules and uses larger fonts when heavily zoomed in. | Label visibility and font selection. |
| `PawnRenderer.RenderPawnAt(Vector3, Rot4?, bool)` | `DotTools.cs` | Prefix | Suppresses vanilla pawn body draw when a marker should replace it. | Very hot; incorrect decision hides pawns. |
| `Vehicles.VehicleRenderer:RenderPawnAt` | `DotTools.cs` | Reflection target, Prefix | Same body suppression for Vehicle Framework pawns when present. | Optional mod API shape. |
| `SelectionDrawer.DrawSelectionBracketFor` | `DotTools.cs` | Prefix | Suppresses vanilla selection brackets when marker should replace pawn rendering. | Selection readability. |
| `PawnUIOverlay.DrawPawnGUIOverlay` | `DotTools.cs` | Prefix | Suppresses pawn GUI overlay for marker-rendered pawns. | Name/status overlays can disappear. |
| `SilhouetteUtility.ShouldDrawSilhouette` | `DotTools.cs` | Prefix | Prevents vanilla silhouettes when CameraPlus markers are active or explicitly off. | Interaction with RimWorld silhouette cache. |
| `GenMapUI.DrawPawnLabel(Pawn, Vector2, float, float, Dictionary<string,string>, GameFont, bool, bool)` | `DotTools.cs` | Prefix | Hides pawn labels by marker/zoom/mouse rules. | Hot label path; `truncateToWidth == 9999f` guard matters. |

## Snapback And Pause Handling

| Target | File | Patch | Purpose | Risk |
| --- | --- | --- | --- | --- |
| `KeyBindingDef.KeyDownEvent` getter | `Main.cs` | Prefix | Makes pause key-down behave like a one-frame event for snapback handling. | Input semantics for `TogglePause`. |
| `Root.OnGUI` | `Main.cs` | Postfix | Cleans pause-key state at end of GUI frame. | GUI event timing. |
| `Game.UpdatePlay` | `Main.cs` | Postfix | Creates snapback after holding pause while paused and restores when unpaused. | Uses wall-clock time and coroutine restore. |
| `TickManager.TogglePaused` | `Main.cs` | Postfix | Restores snapback when pause toggles off. | Time-speed restoration. |
| `MainTabWindow_Menu.PreOpen` | `Main.cs` | Postfix | Clears pending snapback before opening main menu. | Low. |
| `UIRoot_Play.UIRootOnGUI` | `Main.cs` | Postfix | Draws full-screen border overlay while snapback is armed. | GUI overdraw. |

## Compatibility Patches

| Target | File | Patch | Purpose | Risk |
| --- | --- | --- | --- | --- |
| `Analyzer.Fixes.H_DrawNamesFix:Prefix` | `Main.cs` | Reflection target, Prefix | Disables Dubs Performance Analyzer draw-name fix by forcing true result and skipping original. | Depends on optional mod internals. |
| `SaveOurShip2.MeshRecalculateHelper:RecalculateMesh` | `Main.cs` | Reflection target, Transpiler | Scales Save Our Ship 2 background mesh calculation. | Optional mod IL-shape dependency. |
| `Map.MapUpdate` | `Main.cs` | Postfix, gated by active Save Our Ship 2 | Adjusts Save Our Ship 2 planet background material once per session. | Optional mod internals and static one-shot state. |

## Transpiler Fragility Notes

The transpilers rely on method names, property getters, and local IL shape from the publicised RimWorld assemblies. A successful compile after updating `Krafs.Rimworld.Ref` does not prove the transpiler still edits the intended instruction sequence. In-game verification should include:

- zooming in/out while the mouse is over recognizable map cells.
- checking the current view rect by looking for culling or edge-marker drift.
- observing labels and markers at close, middle, far, and furthest zoom.
- loading with Vehicle Framework, Save Our Ship 2, and Dubs Performance Analyzer when those compat paths are in scope.
