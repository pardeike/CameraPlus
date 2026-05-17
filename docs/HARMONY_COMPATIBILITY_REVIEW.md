# Harmony Compatibility Review

Date: 2026-05-17

Purpose: identify CameraPlus Harmony integration points, check the RimWorld target shape, and search acquired GitHub RimWorld mod source for patches on the same methods. The specific concern was mods that indirectly or accidentally shortcut CameraPlus patches.

## Method

- Decompiled CameraPlus from `1.6/Assemblies/CameraPlus.dll` in the `cameraplus-current` decompiler context.
- Decompiled RimWorld 1.6 methods from `Assembly-CSharp.dll` in the `rimworld-current` decompiler context.
- Searched the local GitHubCodeSearch corpus one target at a time with `harmony_find_patches` and exact `code_search` fallbacks.
- Corpus state during this pass: 52 searchable repositories, ripgrep backend.
- Discovery query used for the fresh acquisition pass: `RimWorld Harmony mod language:C# topic:rimworld pushed:>=2022-01-01 fork:true archived:false`.

Limitations:

- The GitHub corpus is evidence, not a complete Workshop scan. Closed source mods, Workshop-only source bundles, and unacquired repositories can still patch the same methods.
- `harmony_find_patches` is intentionally pragmatic and can produce false positives for generic names. Exact `[HarmonyPatch(...)]`, `AccessTools.Method(...)`, and `evidence_get` snippets were used to filter the material findings.
- Harmony prefix skip-original behavior is not automatically a bypass for CameraPlus postfixes. A prefix returning `false` skips the original method body, but postfixes still run. The more dangerous cases are call-site replacement, transpilers that alter control flow, or higher-priority skip prefixes that prevent a CameraPlus prefix from running.

## Main Findings

1. The highest actual conflict surface is CameraPlus' prefix-based vanilla-render suppression, not its `DrawDynamicThings` postfix. CameraPlus uses priority `10000` on the pawn/body/label suppression prefixes, so it normally runs before standard `Priority.First` patches.
2. Dubs Performance Analyzer's `DynamicDrawManager.DrawDynamicThings` profiling patch skips the vanilla dynamic draw body when active, but CameraPlus' postfix should still run. This is a compatibility concern for draw semantics, not a current missing-marker bypass.
3. Dubs Performance Analyzer's `H_DrawNamesFix` is a real label-path replacement, and CameraPlus already mitigates it by patching `Analyzer.Fixes.H_DrawNamesFix:Prefix` to return `true` and skip the DPA replacement. That mitigation depends on DPA keeping that internal type and method name.
4. Zombieland is the clearest entity-specific renderer conflict. It patches `PawnRenderer.RenderPawnAt` and `GenMapUI.DrawPawnLabel` for zombies, including a skip-original path for emerging zombies and label suppression for zombies. CameraPlus can intentionally replace pawn rendering with markers first, so this is a policy conflict for zombies rather than a generic crash risk.
5. The most fragile CameraPlus patches are still the transpilers and string-reflection targets: `CameraDriver.Update`, `CameraDriver.ApplyPositionToGameObject`, `CameraDriver.CurrentViewRect`, `SaveOurShip2.MeshRecalculateHelper:RecalculateMesh`, and optional `Vehicles.VehicleRenderer:RenderPawnAt`.
6. The canonical patch inventory was missing `MarkerCacheLifecycle.cs`. That has now been added to `docs/HARMONY_PATCHES.md`.

## Target Review

| CameraPlus target | Patch kind | External evidence found | Robustness assessment |
| --- | --- | --- | --- |
| `UIRoot_Entry.Init` | Postfix | Zombieland also has startup asset loading on `UIRoot_Entry.Init`. | Low risk. Postfix startup work composes unless another mod prevents init entirely. |
| `World.FinalizeInit` | Postfix | `Falconne/ImprovedWorkbenches` uses a postfix to refresh world storage. | Low risk. Same pattern, no skip-original found. |
| `Widgets.WidgetsOnGUI` | Postfix | No exact external patch found in the acquired corpus. | Low risk. GUI timing only. |
| `Game.DeinitAndRemoveMap` | Prefix | HugsLib has a postfix map-removal hook. | Low risk. CameraPlus cache removal is idempotent and earlier cleanup is acceptable. |
| `Pawn.DeSpawn` | Prefix | `Falconne/ImprovedWorkbenches` and Multiplayer timestamp helpers use non-skipping prefixes. | Low risk. Cache removal is idempotent. |
| `Pawn.Destroy` | Prefix | Zombieland has a non-skipping cleanup prefix. | Low risk. No exact skip-original destroy prefix found. |
| `CameraDriver.Update` | Prefix plus transpiler | No material exact third-party patch hit in the acquired corpus beyond CameraPlus. | High local fragility because the transpiler depends on the `RootSize` assignment shape. Existing null-driver owner logging is useful and should stay. |
| `TimeControls.DoTimeControlsGUI` | Void prefix | `rimworld_access` has `Priority.First` prefixes that return `false` while intercepting the pause key in placement modes; Multiplayer and Zombieland also patch this method. | Moderate. CameraPlus' prefix has no return/ref signature, so prefix skip is less likely to suppress it, but shared key handling can still conflict at the event-consumption level. |
| `CameraDriver.CalculateCurInputDollyVect` | Postfix | Dubs Performance Analyzer `H_ZoomThrottle` also has a postfix. | Low to moderate. Postfix composition is expected; ordering can alter panning scale if another mod also mutates the vector. |
| `CameraDriver.CurrentZoom` getter | Prefix replacement | No exact external patch found; DPA reads this property. | Moderate. Replacing the zoom enum changes downstream consumers; direct patch collision evidence was not found. |
| `CameraDriver.ApplyPositionToGameObject` | Transpiler | No exact external patch found. | High local fragility. Core private camera method and IL-shape dependency. |
| `CameraDriver.CurrentViewRect` getter | Transpiler | Multiplayer has call-site manipulation around view rect in unrelated logic, not a direct getter patch. | High local fragility, moderate ecosystem risk. View rect affects culling and marker edge decisions. |
| `DynamicDrawManager.DrawDynamicThings` | Postfix | Dubs Performance Analyzer profiling prefix redraws dynamic things and returns `false` when active. | Moderate. Prefix skip does not bypass CameraPlus postfix, but DPA changes draw-loop semantics and `drawingNow` state. A missing marker bug here should be reproduced before adding a fallback. |
| `MoteMaker.ThrowText(Vector3, Map, string, Color, float)` | Prefix | No exact external patch found in the acquired corpus. | Low to moderate. User-visible feedback suppression is intentional but should remain easy to disable through settings. |
| `OverlayDrawer.RenderForbiddenOverlay` | Prefix | No exact external patch found. | Low. Corpse-only overlay suppression. |
| `GenMapUI.DrawThingLabel(Vector2, string, Color)` | Prefix plus transpiler | No exact external patch found. | Moderate. Label hiding and font replacement are user-visible; transpiler shape is small but still IL-dependent. |
| `PawnRenderer.RenderPawnAt(Vector3, Rot4?, bool)` | Prefix | Zombieland patches this with `Priority.First`, custom zombie rendering, and a skip-original path for emerging zombies. DPA forks also profile this target. | Moderate to high. CameraPlus priority `10000` should run early, but marker suppression can intentionally preempt entity-specific custom renderers. This is a compatibility policy risk. |
| `Vehicles.VehicleRenderer:RenderPawnAt` | Reflection prefix | No acquired external Vehicle Framework source hit. | Moderate. Optional API string target; robust against absence, fragile against signature/name drift. |
| `SelectionDrawer.DrawSelectionBracketFor` | Prefix | JecsTools BigBox has a skip-original prefix for custom brackets on big-box things. Multiplayer calls `DrawSelectionBracketFor` for remote selections. | Low to moderate. CameraPlus only suppresses pawn brackets; JecsTools scope is non-pawn `ThingWithComps`. |
| `PawnUIOverlay.DrawPawnGUIOverlay` | Prefix | Dubs Performance Analyzer `H_DrawNamesFix` replaces label drawing and returns `false` when enabled. | Moderate. CameraPlus has an explicit mitigation for the known DPA type. Drift in DPA internals would reopen this conflict. |
| `SilhouetteUtility.ShouldDrawSilhouette` | Prefix | No exact external patch found. | Low. CameraPlus returns false only when its marker decision suppresses vanilla rendering. |
| `GenMapUI.DrawPawnLabel(...)` | Prefix | Zombieland suppresses zombie labels unless they were map pawns before. | Moderate. Entity-specific label policy can conflict with CameraPlus marker/label policy. |
| `KeyBindingDef.KeyDownEvent` getter | Prefix replacement for `TogglePause` | No exact external patch found besides CameraPlus. | Moderate. It changes pause key semantics, but the scope is only `KeyBindingDefOf.TogglePause`. |
| `Root.OnGUI` | Postfix | No exact `Root.OnGUI` external patch found; DPA search hits were false positives on other GUI methods. | Low. End-of-frame cleanup should compose. |
| `Game.UpdatePlay` | Postfix | Zombieland has an unrelated `Game.UpdatePlay` postfix. | Low. Postfix composition expected. |
| `TickManager.TogglePaused` | Postfix | NoPauseChallenge and a multiplayer prototype patch can skip the original pause toggle. | Moderate. Postfixes should still run after skip-original prefixes, but semantics may be intentionally replaced by those mods. |
| `MainTabWindow_Menu.PreOpen` | Postfix | No material exact external patch found. | Low. Cleanup hook only. |
| `UIRoot_Play.UIRootOnGUI` | Postfix | NoPauseChallenge injects an early-return transpiler for freeze handling; `rimworld_access` has a postfix for sound maintenance. | Moderate. Prefix skip is not the issue; control-flow transpilers and event consumption are. |
| `Analyzer.Fixes.H_DrawNamesFix:Prefix` | Reflection prefix | Target is DPA internals. | Moderate. Useful mitigation, but internal-name drift risk. |
| `SaveOurShip2.MeshRecalculateHelper:RecalculateMesh` | Reflection transpiler | No acquired SOS2 source hit. | High. Optional IL-shape dependency. Absence is handled; changed internals are not. |
| `Map.MapUpdate` | Save Our Ship 2 gated postfix | Multiplayer arbiter patches can skip `Map.MapUpdate`; Zombieland has an SOS2-gated postfix for floating zombies. | Low for normal play, moderate in multiplayer/arbiter contexts. CameraPlus use is a one-shot SOS2 material adjustment. |

## Evidence Notes

These were the material external patches found in the acquired corpus:

| Target | Repository evidence | Finding |
| --- | --- | --- |
| `DynamicDrawManager.DrawDynamicThings` | `Cappuchinoo/Dubs-Performance-Analyzer`, commit `8288138470b2a1cd1b5fbf0b2ab877c5f54e69ff`, `Source/Profiling/Patches/Update/H_DrawDynamicThings.cs` | Patches `DrawDynamicThings` with a prefix. When active, it redraws dynamic things itself and returns `false`. CameraPlus' postfix should still run. |
| `PawnUIOverlay.DrawPawnGUIOverlay` | `Cappuchinoo/Dubs-Performance-Analyzer`, commit `8288138470b2a1cd1b5fbf0b2ab877c5f54e69ff`, `Source/Performance/Patches/H_DrawNamesFix.cs` | Patches pawn GUI overlay with a prefix that draws labels/overlays and returns `false` when enabled. CameraPlus already patches this prefix to disable the replacement. |
| `PawnRenderer.RenderPawnAt` | `pardeike/Zombieland`, commit `9ee89710eb0ad2eac1cab90d216634f3ed39dfa1`, `Source/Patches.cs` | Priority-first prefix returns `false` for emerging zombies after rendering them through Zombieland custom code. |
| `GenMapUI.DrawPawnLabel(...)` | `pardeike/Zombieland`, commit `9ee89710eb0ad2eac1cab90d216634f3ed39dfa1`, `Source/Patches.cs` | Prefix suppresses zombie labels unless `zombie.wasMapPawnBefore`. |
| `TimeControls.DoTimeControlsGUI` | `aaronr7734/rimworld_access`, commit `252dc51661eef232da9bf74fb965d160587cebac`, `src/Building/ArchitectPlacementPatch.cs` and `src/Building/ZoneCreationPatch.cs` | Priority-first prefixes consume pause key events and return `false` during accessible placement/zone modes. |
| `Map.MapUpdate` | `bnelz/Multiplayer`, commit `8a9216df977a5a9b2043b966eae8945cb4321b43`, `Source/Client/ArbiterPatches.cs` | Arbiter prefix returns `false` to cancel `MapUpdate` in arbiter mode. |
| `Map.MapUpdate` | `rwmt/Multiplayer`, commit `4a3be276bbf90cc597abfa5b299935ca8eeeb285`, `Source/Client/Patches/ArbiterPatches.cs` | Same arbiter-style skip-original pattern in the maintained Multiplayer repository. |
| `TickManager.TogglePaused` | `pardeike/NoPauseChallenge`, commit `1de35f123041fd748544ed91d75191650683abd2`, `Source/Main.cs` | Prefix can return `false` to block pause toggling while no-pause modes are active. |
| `SelectionDrawer.DrawSelectionBracketFor` | `RimWorld-CCL-Reborn/JecsTools`, commit `7be56bc5fc80f9802d940f41d69c634587235169`, `Source/AllModdingComponents/CompBigBox/_HarmonyCompBigBox.cs` | Prefix returns `false` for `ThingWithComps` using BigBox extension and draws custom brackets. Scope does not overlap CameraPlus pawn-only suppression. |
| `Game.DeinitAndRemoveMap` | `UnlimitedHugs/RimworldHugsLib`, commit `99d60abc5633e6d4746d180e1d1ec65591f775f7`, `Source/Patches/Game_DeinitAndRemoveMap_Patch.cs` | Postfix map-removal hook. Composes with CameraPlus prefix cache cleanup. |
| `World.FinalizeInit` | `Falconne/ImprovedWorkbenches`, commit `f8db7979e9885e0e723a110e56d702b68dbfc341`, `src/ImprovedWorkbenches/Main.cs` | Postfix world storage refresh. Composes with CameraPlus `CameraSettings` refresh. |

## Robustness Recommendations

1. Do not add a `Map.MapUpdate` fallback draw for DPA unless a live repro shows CameraPlus dots missing. The current evidence is a skip-original prefix on `DrawDynamicThings`, and Harmony postfixes should still run.
2. Keep `CameraPlusMain.skipCustomRendering` public and documented. It is the cleanest escape hatch for mods that deliberately draw pawns/labels themselves.
3. Add targeted runtime diagnostics, not broad fallback behavior, if a user reports a compatibility issue:
   - log `Harmony.GetPatchInfo(...)` owners for the affected method once.
   - distinguish prefix-skip, transpiler, and call-site bypass cases in the message.
4. For optional string targets, prefer fail-soft behavior:
   - `Vehicles.VehicleRenderer:RenderPawnAt` should keep `Prepare() => TargetMethod() != null`.
   - `SaveOurShip2.MeshRecalculateHelper:RecalculateMesh` should log once if the target exists but the expected IL anchor does not match.
5. Re-test Dubs Performance Analyzer with its draw labels fix and dynamic draw profiling enabled before changing the current DPA mitigation. It is the only evidence-backed replacement patch that directly overlaps CameraPlus marker/label policy.
6. Treat Zombieland as the first real renderer-policy compatibility test case: emerging zombies and zombie labels are deliberately non-vanilla. If a user reports this combination, decide whether CameraPlus marker rules should override Zombieland custom rendering or yield for zombies.
7. Keep the perf-gated renderer-phase shortcut out of release builds. It is much more likely than the normal build to conflict with apparel, weapon, overlay, and custom pawn renderer mods.

