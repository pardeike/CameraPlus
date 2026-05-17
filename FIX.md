# Camera+ Follow-Up Fix Notes

This file collects the issues found after commit `d53042c` (`Fix animal marker colors and edge ordering`). Each item describes the suspected problem, the relevant code path, and a manual test case that should make the problem visible.

General setup for all tests:

1. Build and deploy the current working tree:
   ```sh
   dotnet build Source/CameraPlus.csproj -c Release
   ```
2. Start RimWorld with Camera+ enabled.
3. Enable dev mode in RimWorld if the test asks for spawning pawns, changing factions, or checking logs.
4. After each test, check the RimWorld log for Camera+ exceptions or Harmony errors.
5. When changing Camera+ rules, use `Options -> Mod Settings -> Camera+ -> Rules`.
6. For tests involving default rules, remember that existing saves keep save-specific rules, while new games use the default rules loaded from `CameraPlusDefaultRules.xml`.

The current code references below are the relevant areas to inspect while fixing:

- `Source/MarkerDecision.cs`
- `Source/DotTools.cs`
- `Source/DotDrawer.cs`
- `Source/MarkerCache.cs`
- `Source/Materials.cs`
- `Source/Tools.cs`
- `Source/BoolTags.cs`
- `Source/CameraSettings.cs`
- `Source/Dialog_Customization.cs`

## Guiding Decisions

These notes are meant to guide implementation, not expand the rule UI unnecessarily.

1. Keep the main Camera+ settings understandable for average users. Rules are for advanced styling and exceptions, not for hiding core behavior behind extra per-rule switches.
2. Treat `Include not tamed animals` as a global inclusion gate. If it is off, unnamed wild animals should not be pulled back in by default animal rules.
3. Camera+ should suppress vanilla pawn rendering only when it will draw a real in-map replacement marker. Edge indicators do not count as replacement markers.
4. Prefer small policy helpers and shared decision points over scattered special cases.
5. Go the extra mile where it prevents future drift: animal marker policy, cache lifecycle, and color/tint signatures need explicit ownership because they affect many paths.

## 1. Animal "No Marker" Can Hide The Animal Body

### Problem

Rule-backed pawns can suppress vanilla rendering even when the current global animal setting says the animal should not get a Camera+ marker.

`MarkerDecision.For()` computes `drawInside` with `defaultShow`, but `ShouldSuppressVanilla()` does not receive or check `defaultShow` for rule-backed pawns. That means a rule can suppress the vanilla pawn renderer while the marker draw path declines to draw anything.

Relevant code:

- `Source/MarkerDecision.cs`: `drawInside` calculation
- `Source/MarkerDecision.cs`: `ShouldSuppressVanilla(...)`
- `Source/DotTools.cs`: `PawnRenderer.RenderPawnAt` prefix uses `ShouldShowMarker(...)`

### Why It Matters

The UI setting "Animals have no marker" should not make animals disappear. It should either show vanilla animals or show Camera+ markers. A hidden vanilla body plus no Camera+ marker is the worst possible combination.

### Manual Test Case

Goal: make a named animal vanish or stop rendering its body when animal markers are disabled.

1. Start or load a save with at least one visible tamed/named animal. `Camera Animal Colors 2` is useful if it already has visible named animals.
2. Pause the game and center the camera on the animal.
3. Open `Options -> Mod Settings -> Camera+`.
4. Set `Dot style` to `Camera+ silhouettes`.
5. In the `Animals` section, choose `Animals have no marker`.
6. Leave the default animal rules enabled.
7. Zoom out until Camera+ markers would normally replace pawn bodies.
8. Observe the animal.

Expected correct behavior:

- The animal should remain visible with its normal vanilla body, because the user asked for no animal marker.

Failure behavior to look for:

- The animal body disappears.
- No Camera+ marker appears either.
- Selection brackets or labels may also be inconsistent because they are controlled by the same marker decision.

Extra checks:

1. Select the animal before zooming out.
2. Move the mouse over the animal to trigger label reveal.
3. Toggle `Mouse reveals labels` and repeat.

Cleanup:

1. Restore `Animals` to the previous value, typically `Animals have a different marker` or `Include animals`.
2. Reload the save if the visual state seems stuck.

### Solution Proposal

Make marker ownership a single decision computed once in `MarkerDecision.For()`. The decision should have one authoritative boolean for "Camera+ replaces the pawn's in-map vanilla rendering this frame", and vanilla body/overlay/bracket suppression should only use that boolean.

Preferred fix:

1. Apply global animal visibility first, before deciding suppression.
2. Compute whether an inside Camera+ marker will actually be drawn, including dot style, rule `useInside`, zoom threshold, mouse reveal, hidden/fog state, and marker color availability.
3. Suppress vanilla in-map rendering only when that inside marker draw is true.
4. Keep edge indicators independent: an off-screen edge marker must not imply vanilla body suppression for the in-map pawn body.

Avoid:

- Do not fix this by adding a special animal-only check to `ShouldSuppressVanilla()`. The same class of bug exists for any rule or mode that suppresses rendering without drawing a replacement.
- Do not let rule-backed pawns ignore global "no animal marker" settings. That keeps the current surprising behavior and makes the UI impossible to reason about.

## 2. "Include Not Tamed Animals" Is Bypassed By Default Animal Rules

### Problem

The global `includeNotTamedAnimals` setting only affects animals when no rule matched the pawn. The default rules include a broad `AnimalTag` rule, so most animals do have a rule. As a result, the global checkbox can appear to do nothing for wild animals.

Relevant code:

- `Source/Settings.cs`: `IncludeNotTamedAnimals` checkbox
- `Source/CameraSettings.cs`: default animal rules
- `Source/MarkerDecision.cs`: `if (Settings.includeNotTamedAnimals == false && pawn.Name == null && dotConfig == null)`
- `Source/Extensions.cs`: first matching `DotConfig`

### Why It Matters

The UI presents `Include not tamed animals` as a global animal visibility setting. Users will not expect it to be overridden by hidden default rules unless the rule editor explicitly communicates that.

### Manual Test Case

Goal: show that wild animals still get Camera+ marker behavior even when `Include not tamed animals` is off.

1. Start or load a map with at least one wild unnamed animal visible.
2. If there is no wild animal nearby, use dev mode to spawn a wild animal such as a deer, hare, or rat.
3. Open `Options -> Mod Settings -> Camera+`.
4. Set `Dot style` to `Camera+ silhouettes` or `Camera+ dots`.
5. Choose an animal mode that allows animal markers, for example `Animals have a different marker` if available for the current dot style, or `Include animals`.
6. Turn `Include not tamed animals` off.
7. Zoom out until markers normally appear.
8. Observe the wild unnamed animal.

Expected correct behavior:

- A wild unnamed animal should keep vanilla rendering or otherwise be excluded from Camera+ marker replacement.

Failure behavior to look for:

- The wild animal still gets a Camera+ marker.
- Its vanilla body or label is suppressed as if it were included.

Control test:

1. Temporarily remove or disable the broad default `AnimalTag` rule in the rule editor.
2. Repeat the test.
3. If the checkbox starts working only after removing the rule, the rule/default setting interaction is confirmed.

Cleanup:

1. Restore default rules from the rule editor if you changed them.
2. Delete the test animal if you spawned one.

### Solution Proposal

Move "is this animal globally included?" into the shared animal policy and apply it before default animal rules can style the pawn. Do not add new rule parameters for this; the checkbox is intended to be global and should stay understandable from the main settings window.

Preferred fix:

1. Treat `Include not tamed animals` as a global eligibility gate for unnamed wild animals.
2. When it is off, unnamed wild animals should not match default animal styling rules and should not have vanilla rendering suppressed by those rules.
3. Keep tamed/named animals eligible for the default animal rules.
4. Use the existing "overwritten by # rules" convention only for settings that rules truly override. Do not imply that rules can override this global inclusion gate unless the UI later grows an explicit, deliberate feature for that.
5. Recompute marker decisions immediately when the setting changes, or clear the relevant caches.

Avoid:

- Do not remove the default animal rules just to make the checkbox work. Those rules carry useful color/edge/mouse behavior.
- Do not add per-rule animal-inclusion parameters. That overloads the rule interface and hides a main setting behind advanced UX.
- Do not document the current behavior as "rules override the checkbox"; the main settings checkbox is meant to be global.

Interactions:

- This should be implemented through the animal policy from issue 17 so default rules, marker decisions, and marker textures all see the same inclusion answer.

## 3. Restore Defaults Leaves No Animal Marker Option Selected

### Problem

The default settings set `customNameStyle` to `AnimalsDifferent`, while the settings UI hides the `AnimalsDifferent` radio option whenever `dotStyle` is `Camera+ silhouettes`. Pressing `Restore to default settings` can therefore put the underlying animal marker setting into a hidden value, leaving the visible `Animals` radio group with no selected option.

This overlaps with the broader animal-settings inconsistency above, but it is a separate UI state bug: the settings window can display an impossible-looking state immediately after restoring defaults.

Relevant code:

- `Source/Settings.cs`: default `customNameStyle = LabelStyle.AnimalsDifferent`
- `Source/Settings.cs`: restore defaults copies a fresh `CameraPlusSettings`
- `Source/Settings.cs`: the animal option loop skips `AnimalsDifferent` for `DotStyle.BetterSilhouettes`
- `Source/LabelStyle.cs`: available animal marker states

### Why It Matters

The restore button should produce a self-explanatory settings UI. A radio group with no selected option makes it look like the settings are corrupted, and it hides the actual value that will be serialized unless the user explicitly chooses another animal mode.

### Manual Test Case

Goal: show that restoring defaults creates a hidden animal marker state.

1. Start RimWorld with Camera+ enabled.
2. Open `Options -> Mod Settings -> Camera+`.
3. Change a few settings so the restore button clearly has work to do. For example:
   - Set `Dot style` to `Camera+ dots`.
   - In the `Animals` section, choose `Include animals` or `Animals have no marker`.
4. Click `Restore to default settings` at the top of the Camera+ settings window.
5. Find the `Dot style` section and confirm it is now `Camera+ silhouettes`.
6. Find the `Animals` section below the label/marker thresholds.
7. Look at the visible animal radio buttons.

Expected correct behavior:

- One visible animal option should be selected after restoring defaults.
- Alternatively, if `Animals have a different marker` is a valid default, it should remain visible and selected for the default dot style.

Failure behavior to look for:

- No visible animal radio option is selected.
- The hidden underlying value is still `AnimalsDifferent`, but the UI does not show that option for `Camera+ silhouettes`.

Control test:

1. Change `Dot style` from `Camera+ silhouettes` to `Camera+ dots`.
2. The `Animals have a different marker` option should reappear.
3. It should be selected, confirming that restore defaults set a real value that the current UI hid.
4. Change `Dot style` back to `Camera+ silhouettes`.
5. The selected animal option disappears again.

Cleanup:

1. Pick the animal marker option you actually want before closing settings.
2. If you only tested this in a disposable session, restore the previous Camera+ settings manually or reload without saving settings.

### Solution Proposal

Expose every meaningful `LabelStyle` value in every dot style where that value can affect rendering. Because `AnimalsDifferent` affects edge marker textures even in `Camera+ silhouettes`, the default value should remain selectable and visible instead of being hidden.

Preferred fix:

1. Remove the UI filter that skips `AnimalsDifferent` for `DotStyle.BetterSilhouettes`.
2. Adjust the label text or tooltip if needed so the option is clear in silhouette mode, for example "Animals use animal edge markers" rather than implying that in-map silhouettes change shape.
3. Keep `Restore to default settings` as a plain copy from `new CameraPlusSettings()`; once the option is visible, the restored state is no longer invalid-looking.
4. Clear marker material caches when the animal marker option changes, because it can change dot/edge textures.

Avoid:

- Do not silently rewrite `AnimalsDifferent` to `IncludeAnimals` during restore. That would remove the current default behavior and hide a real rendering distinction.
- Do not add a one-off "select first visible radio" fallback. That masks hidden state instead of fixing why a meaningful state is hidden.

Interactions:

- This should fall out naturally from issue 4: once all meaningful animal marker states are visible, restore defaults no longer creates an invisible radio selection.

## 4. Silhouette Animal Settings Hide A Real Third Marker Mode

### Problem

The `LabelStyle` enum has three animal marker states:

1. `IncludeAnimals`: animals have the same marker.
2. `AnimalsDifferent`: animals have a different marker.
3. `HideAnimals`: animals have no marker.

The settings UI hides `AnimalsDifferent` whenever `dotStyle` is `Camera+ silhouettes`, leaving only two visible choices. That hidden value is not purely redundant. `Tools.DefaultMarkerTextures()` still uses `AnimalsDifferent` to choose animal-specific marker textures, and edge indicators use those marker textures even when in-map markers are silhouettes.

This means the silhouette UI is missing a real third user-facing behavior: "show animals, but use animal-specific marker textures for dot/edge marker surfaces". The state is reachable through defaults or by switching from `Camera+ dots`, but it is not directly selectable while `Camera+ silhouettes` is active.

Relevant code:

- `Source/LabelStyle.cs`: three animal marker states
- `Source/Settings.cs`: hides `AnimalsDifferent` for `DotStyle.BetterSilhouettes`
- `Source/Tools.cs`: `DefaultMarkerTextures()` uses `AnimalsDifferent` for animal marker textures
- `Source/MarkerCache.cs`: edge dots are built from default marker textures
- `Source/DotDrawer.cs`: edge indicators draw `materials.edgeDot`

### Why It Matters

The current two visible choices make the setting look binary in silhouette mode: same marker or no marker. Internally there is still a third behavior that affects visuals, especially edge indicators. That makes the UI incomplete and explains why restore defaults can land on a hidden value.

### Manual Test Case

Goal: show that `AnimalsDifferent` affects silhouette-mode edge indicators but cannot be selected while staying in silhouette mode.

1. Start RimWorld with Camera+ enabled and load a map with at least one visible animal.
2. Open `Options -> Mod Settings -> Camera+`.
3. Set `Dot style` to `Camera+ dots`.
4. In the `Animals` section, confirm there are three choices:
   - `Animals have the same marker`
   - `Animals have a different marker`
   - `Animals have no marker`
5. Select `Animals have a different marker`.
6. Change `Dot style` to `Camera+ silhouettes`.
7. Return to the `Animals` section.

Expected correct behavior:

- If animal-specific marker textures still matter in silhouette mode, a visible option should let the user choose that behavior.
- If the option is truly meaningless in silhouette mode, the hidden value should be normalized to a visible equivalent.

Failure behavior to look for:

- Only two choices are visible.
- The hidden `AnimalsDifferent` state remains active, but no visible radio option represents it.
- If this follows a restore-defaults flow, no animal option is selected at all.

Functional edge-marker confirmation:

1. Keep the hidden `AnimalsDifferent` state by choosing it in `Camera+ dots`, then switching back to `Camera+ silhouettes`.
2. Ensure `Edge indicators` is on.
3. Move the camera until the animal is off-screen and its edge indicator is visible.
4. Note the animal edge indicator texture/shape.
5. Without leaving `Camera+ silhouettes`, choose the visible `Animals have the same marker` option.
6. Move the camera the same way and compare the edge indicator.

Expected confirmation:

- If the edge marker texture/shape changes, then the hidden third state has a real visual effect in silhouette mode.
- The user cannot switch back to that animal-specific edge marker state without temporarily switching `Dot style` to `Camera+ dots`.

Cleanup:

1. Restore the animal marker option you want.
2. If you changed rules or spawned animals for the test, restore those changes.

### Solution Proposal

Keep all three animal marker choices visible and make their effect explicit. In silhouette mode, `AnimalsDifferent` should be described as affecting animal marker textures for dot/edge marker surfaces, not as changing the silhouette body texture.

Preferred fix:

1. Show `IncludeAnimals`, `AnimalsDifferent`, and `HideAnimals` for `Camera+ dots`, `Camera+ silhouettes`, and any future marker style that still uses marker textures for edge indicators or fallbacks.
2. If the current label text is misleading in silhouette mode, update translations to a neutral wording that covers both dots and edge indicators.
3. Ensure the selected value is always visible and serializable from the settings window.
4. Add a cache clear when switching between same/different animal markers so edge marker materials rebuild with the correct texture.

Avoid:

- Do not collapse `AnimalsDifferent` into `IncludeAnimals` only for silhouettes. Edge marker textures prove that the states are not equivalent.
- Do not split this into a separate edge-only checkbox unless a future design intentionally separates in-map animal marker style from edge marker style. That would be a larger UX change than needed.

Interactions:

- This should be implemented through the animal policy from issue 17 so the UI, marker textures, and cache invalidation all use the same interpretation of `LabelStyle`.

## 5. Null-Unsafe Rule Tags Can Break Matching For Unrelated Pawns

### Problem

Some condition tags assume every pawn has the relevant component. `PlayerFactionTag` dereferences `pawn.Faction.IsPlayer`; factionless pawns can have `pawn.Faction == null`. `SelfShutdownTag` dereferences `pawn.needs.energy.IsSelfShutdown`; most non-mech pawns do not have an energy need.

Rule lookup evaluates every condition in a matching candidate rule, so one unsafe tag can throw while processing pawns that are not intended to match that rule.

Relevant code:

- `Source/Extensions.cs`: `dotConfig.conditions.All(condition => condition.Matches(pawn))`
- `Source/BoolTags.cs`: `PlayerFactionTag`
- `Source/BoolTags.cs`: `SelfShutdownTag`

### Why It Matters

The rule editor exposes these tags as normal condition choices. A user can reasonably create a rule like "Player faction" or "Self shutdown" without expecting wild animals, raiders, or humans to throw exceptions during rendering.

### Manual Test Case A: `PlayerFactionTag` With Factionless Pawns

Goal: make a factionless pawn evaluate a rule containing `PlayerFactionTag`.

1. Start or load a map with a wild animal or any factionless pawn visible.
2. Open `Options -> Mod Settings -> Camera+ -> Rules`.
3. Add a new rule near the top of the list.
4. Add the condition `Player faction`.
5. Set the rule to a visible marker mode and distinct colors, for example a red fill and black outline.
6. Close the rule editor.
7. Zoom out until Camera+ scans and draws markers.
8. Watch the log.

Expected correct behavior:

- Factionless pawns should simply not match the rule.
- No exception should be logged.

Failure behavior to look for:

- `NullReferenceException` or a Camera+ stack trace involving `PlayerFactionTag.Matches`.
- Marker drawing or label suppression becomes inconsistent for pawns after the exception.

Manual variation:

1. Negate the `Player faction` condition.
2. Repeat with wild animals.
3. A negated condition should match factionless pawns if that is the intended rule semantics, but it still must not throw.

Cleanup:

1. Remove the test rule.
2. Clear any spawned test pawns.

### Manual Test Case B: `SelfShutdownTag` With Non-Mechs

Goal: make a human or animal evaluate a rule containing `SelfShutdownTag`.

1. Start or load a normal colony with colonists and animals.
2. Open `Options -> Mod Settings -> Camera+ -> Rules`.
3. Add a new rule near the top of the list.
4. Add the condition `Self shutdown`.
5. Give it a visible marker style and distinct colors.
6. Close the editor and zoom out.
7. Watch the log while humans and animals are visible.

Expected correct behavior:

- Non-mechs should simply not match `Self shutdown`.
- No exception should be logged.

Failure behavior to look for:

- `NullReferenceException` or a Camera+ stack trace involving `SelfShutdownTag.Matches`.

Cleanup:

1. Remove the test rule.

### Solution Proposal

Make condition evaluation null-safe at the individual tag level, and apply negation only after each tag has computed its raw non-negated boolean. Each tag should answer "does this pawn have this property?" without throwing for pawn types where the property is not applicable.

Preferred fix:

1. Change `PlayerFactionTag` to use `pawn.Faction?.IsPlayer ?? false`.
2. Change `SelfShutdownTag` to use `pawn.needs?.energy?.IsSelfShutdown ?? false`.
3. Review the rest of `BoolTags.cs` for the same pattern and make similar safe checks where components can be null or pawn-type-specific.
4. Prefer a small helper pattern such as `return MatchesRaw(pawn) != Negated` only if it reduces repeated precedence mistakes without restructuring the whole rule system.

Avoid:

- Do not wrap `dotConfig.conditions.All(...)` in a broad try/catch as the main fix. It would hide bad tag implementations and create hot-path exception cost.
- Do not special-case animals or mechs in the rule selector. The tags themselves should be safe for every pawn.

## 6. Negated `Can Cast` Rule Does Not Match Pawns With No Ability

### Problem

`CanCastTag` uses nullable-bool precedence in a way that collapses "no ability" to false after the negation expression. A negated `Can cast` rule should probably match pawns that cannot cast, including pawns with no current ability, but the current expression can return false for the common null case.

Relevant code:

- `Source/BoolTags.cs`: `CanCastTag`

### Why It Matters

Negated tags should generally mean "not this condition". If a pawn has no ability, it is natural for `Not Can Cast` to match. The current behavior makes the negated condition unexpectedly narrow.

### Manual Test Case

Goal: show that `Not Can Cast` does not match ordinary colonists or animals.

1. Load a normal colony with at least one visible colonist that is not currently using an ability.
2. Open `Options -> Mod Settings -> Camera+ -> Rules`.
3. Add a new rule at the top of the list.
4. Add condition `Can cast`.
5. Edit the condition and check `Opposite` so it becomes `Not Can cast`.
6. Set a very obvious marker style and color, for example `Camera+ dots` with bright magenta fill.
7. Close the editor.
8. Zoom out until markers are visible.
9. Observe ordinary colonists.

Expected correct behavior:

- Ordinary colonists with no castable current ability should match `Not Can cast`.
- They should use the bright test marker.

Failure behavior to look for:

- Ordinary colonists do not match the rule.
- A later rule or the default marker behavior is used instead.

Control test:

1. Replace `Not Can cast` with a simple condition that definitely matches the colonist, such as `Colonist`.
2. Confirm that the same marker style/color appears.
3. This confirms the rule order and marker configuration are correct.

Cleanup:

1. Remove the test rule.

### Solution Proposal

Rewrite nullable condition tags so the raw condition is computed first and negation is applied last. For `CanCastTag`, the raw value should be `pawn.CurJob?.ability?.CanCast ?? false`, then the method should return `Negated ^ canCast`.

Preferred fix:

1. Fix `CanCastTag` directly with a local `canCast` boolean.
2. Search all condition tags for `Negated ^` combined with nullable expressions or `??`.
3. Where found, rewrite them to the same raw-then-negate shape.
4. Add a focused manual test using a negated tag against a pawn where the optional component is absent.

Avoid:

- Do not reinterpret null as "unknown" for rule matching. The existing rule model is boolean; absent capability should mean the raw condition is false.
- Do not add a special `CannotCastTag`. Negation already exists and should be reliable.

## 7. Main Pawn Color Cache Is Too Coarse For Tinted Animals

### Problem

`Tools.GetMainColor()` caches by pawn runtime type plus body graphic path. It does not include `graphic.color`, material `_Color`, faction/color variant data, gender variant, or any other per-pawn tint source.

The new silhouette texture cache includes tint, but edge indicator fill colors still use `Tools.GetMainColor()`.

Relevant code:

- `Source/Tools.cs`: cache key in `GetMainColor`
- `Source/DotTools.cs`: `GetEdgeFillColor`
- `Source/MarkerCache.cs`: silhouette tint hash, for comparison

### Why It Matters

Two animals can share the same body texture but have different colors. If the first animal cached as white, brown, grey, etc., every later animal with the same body graphic path can inherit that cached main color for edge indicators or default marker fill.

### Manual Test Case

Goal: show two same-kind animals with different tint sharing one edge/marker color.

1. Load `Camera Animal Colors 2` or create a test map with several animals of the same kind but different visible colors.
2. Good candidates are animals where RimWorld or another mod applies tint variants while reusing the same body texture.
3. Open Camera+ settings.
4. Ensure `Dot style` is `Camera+ silhouettes`.
5. Ensure `Edge indicators` is on.
6. Ensure `Pawn-colored edge indicators` is on.
7. Use the default animal silhouette rule with transparent normal fill.
8. Move the camera so two differently colored animals of the same kind are off-screen and their edge indicators are visible at the same edge.
9. Compare the edge indicator fill colors.

Expected correct behavior:

- Each edge indicator should follow its own pawn's visible main color.

Failure behavior to look for:

- Same-kind animals with different body colors get identical edge fill colors.
- The color may match whichever animal of that body graphic was first processed after cache clear.

Control test:

1. Restart RimWorld to clear static caches.
2. Load the same save.
3. Move the camera so the other color variant is encountered first.
4. If the shared edge color changes to the first processed variant, the cache key is confirmed as the cause.

Cleanup:

1. No persistent cleanup needed unless you spawned animals.

### Solution Proposal

Replace the current main-color cache key with a key that represents the final visible body-color source, not only the body graphic path. Keep the expensive texture sampling cached, but do not share sampled results across differently tinted animals.

Preferred fix:

1. Build a color cache key from pawn runtime type, body graphic path, source texture instance, `graphic.color`, and material `_Color` if available.
2. If the final tint is non-white, include that tint in the key and apply it consistently to the sampled color.
3. Keep the downsampled texture analysis cached for identical texture/tint signatures.
4. Clear `Caches.cachedMainColors` when custom marker or appearance-affecting state changes are detected, or when a save/map is unloaded.

Avoid:

- Do not key by `Pawn` only. That fixes color correctness but throws away most of the useful sharing and can make texture sampling expensive on animal-heavy maps.
- Do not remove caching entirely. The perf baseline already identifies color sampling as an expensive operation on cache misses.

## 8. Silhouette Tint Composition May Ignore Material Tint

### Problem

`SourceMaterialTint()` uses the material `_Color` only when `graphicTint` is white. If both the graphic tint and the material tint are meaningful, Camera+ applies only one of them.

Relevant code:

- `Source/MarkerCache.cs`: `SourceMaterialTint`
- `Source/MarkerCache.cs`: `CreateCutoutTexture`

### Why It Matters

Some modded graphics pipelines combine a `Graphic.color` tint with a material tint or shader tint. Camera+ now creates a baked cutout texture, so if it bakes the wrong tint it can render a silhouette that is consistently off-color.

### Manual Test Case

Goal: find a pawn whose vanilla body color is produced by both a non-white graphic tint and non-white material tint, then compare vanilla body color to Camera+ silhouette color.

1. Use a save with modded animals or pawns known to have color variants.
2. Pick a pawn whose vanilla body has a distinctive non-white color.
3. Open Camera+ settings.
4. Set `Dot style` to `Vanilla default`.
5. Zoom to a range where the vanilla pawn body is visible.
6. Take a screenshot or visually note the body color.
7. Switch `Dot style` to `Camera+ silhouettes`.
8. Use a rule where normal fill is transparent and outline is not hiding the body texture.
9. Zoom until Camera+ replaces the pawn body with a silhouette marker.
10. Compare the silhouette color to the vanilla body color.

Expected correct behavior:

- The Camera+ silhouette should preserve the visible color relationship of the vanilla pawn body.

Failure behavior to look for:

- The silhouette is too pale, too dark, or missing a tint component.
- The same pawn's vanilla body and Camera+ silhouette differ in hue even when lighting/background are stable.

Advanced confirmation:

1. Add temporary logging around `SourceMaterialTint()` to print `graphicTint` and `material.GetColor("_Color")` for the target pawn.
2. Confirm both are non-white.
3. If the silhouette only reflects one tint, the composition issue is confirmed.

Cleanup:

1. Remove any temporary logging before committing.

### Solution Proposal

Create one shared resolver for the effective body tint used by both silhouette cutout baking and main-color sampling. The resolver should prefer the same tint source RimWorld is actually using for the rendered body material.

Preferred fix:

1. Inspect the material returned by `SilhouetteUtility.GetCachedSilhouetteData(pawn)` and use its `_Color` when present and non-white.
2. Fall back to `graphic.color` when the material has no meaningful tint.
3. Only multiply material tint and graphic tint if a targeted test with a known modded pawn proves RimWorld expects both to combine. Avoid blind multiplication because the material color may already include the final graphic tint.
4. Use the resolved tint in the silhouette texture cache key and in the main-color cache key.

Avoid:

- Do not keep separate tint logic in `MarkerCache` and `Tools.GetMainColor`; that will reintroduce color mismatches.
- Do not always trust `graphic.color` over material `_Color`; the current bug is that material tint can be more authoritative for some silhouettes.

## 9. Marker Material Cache Misses Texture-Affecting Settings And Dynamic Providers

### Problem

`Materials.Matches()` invalidates cached materials only when marker mode, custom marker name, or outline factor changes. It does not include settings that affect selected textures, such as `customNameStyle`, and it does not account for external `GetCameraPlusMarkers()` providers that may return different textures over time for the same pawn.

Relevant code:

- `Source/Materials.cs`: `Matches`
- `Source/MarkerCache.cs`: material creation and reuse
- `Source/Tools.cs`: `DefaultMarkerTextures`
- `Source/DotTools.cs`: `GetMarkerTextures`
- `Source/CameraDelegates.cs`: external marker delegates

### Why It Matters

The marker material owns the texture. If the selected texture source changes but `Materials.Matches()` still returns true, Camera+ reuses the old texture indefinitely.

### Manual Test Case A: Animal Marker Style Toggle

Goal: show that toggling animal marker style can leave old dot/edge textures.

1. Load a map with a named animal visible.
2. Open Camera+ settings.
3. Use `Camera+ dots`.
4. Choose an animal setting that uses animal-specific marker textures, for example `Animals have a different marker` if available.
5. Zoom out until the animal marker is visible.
6. Note the marker shape.
7. Change the animal setting to one that should use the normal colonist marker texture, for example `Include animals`.
8. Do not change outline size, dot style, or custom marker name.
9. Zoom/scroll so the same animal marker is redrawn.

Expected correct behavior:

- The animal marker texture should change immediately when the setting changes.

Failure behavior to look for:

- The marker keeps the old animal-specific texture until a cache-clearing event occurs.

Control test:

1. Change `Outline size` slightly.
2. This calls `MarkerCache.Clear()`.
3. If the marker texture updates only after that, cache invalidation is the cause.

Cleanup:

1. Restore the original animal setting.

### Manual Test Case B: External Marker Provider Returns Dynamic Textures

Goal: show that an integration returning different textures for the same pawn type can get stuck.

1. Use or create a small test mod whose pawn type exposes `CameraPlusSupport.Methods.GetCameraPlusMarkers(Pawn)`.
2. Make the method return marker texture A for a pawn in state A and marker texture B for the same pawn in state B.
3. Load a map with that pawn.
4. Zoom out until Camera+ creates the marker material.
5. Change the pawn state so the provider should now return texture B.
6. Keep Camera+ mode and outline factor unchanged.
7. Observe the marker.

Expected correct behavior:

- Marker texture changes from A to B after the pawn state changes.

Failure behavior to look for:

- Marker remains texture A until `MarkerCache.Clear()` is triggered by an unrelated action.

Cleanup:

1. Remove the test mod or restore its normal behavior.

### Solution Proposal

Store marker texture identity in `Materials` and include it in the cache match. Material reuse should be based on the texture actually assigned to each material, not only marker mode and outline factor.

Preferred fix:

1. Add texture identity fields to `Materials`, for example dot texture instance id, edge texture instance id, silhouette texture signature, and custom texture name/id as applicable.
2. Have `MarkerCache.MaterialFor()` compute the expected texture identities before accepting a cached `Materials`.
3. Clear `MarkerCache` when global settings that influence marker texture family change, including animal marker style.
4. For external `GetCameraPlusMarkers()` providers, treat returned texture instance ids as part of the material signature. If a provider returns dynamic textures, the material should refresh when the returned instance changes.

Avoid:

- Do not solve this only by clearing the whole marker cache on every settings draw. That would be simple but expensive and would undo recent cache optimizations.
- Do not assume external marker providers are static by type. The delegate API accepts a pawn argument, so the returned marker can be pawn-state-dependent.

## 10. Selected-Animal Color Guard Creates An Unconfigurable Exception

### Problem

The compatibility guard for selected animals treats selected white fill as legacy/default and replaces it with the normal transparent fill for animal silhouettes. That fixes the observed bug where selected animal silhouettes became white-filled, but it also prevents a user from intentionally configuring that exact effect.

The rule editor still displays and stores the selected fill color normally, so the UI can say "selected fill is white" while runtime rendering ignores it in this specific case.

Relevant code:

- `Source/DotTools.cs`: `ShouldPreserveSelectedAnimalSilhouetteFill`
- `Source/Dialog_Customization.cs`: selected color preview and editor

### Why It Matters

This is a behavior/UI mismatch. It is acceptable as a compatibility shim, but it should either be documented in the UI, migrated away, or made more explicit so users can still intentionally choose white selected fill if they want it.

### Manual Test Case

Goal: show that a configured selected white fill is ignored for selected animal silhouettes.

1. Load a save with a named animal visible.
2. Open `Options -> Mod Settings -> Camera+ -> Rules`.
3. Add or edit an animal rule:
   - Conditions: `Animal`
   - Mode: `Camera+ silhouettes`
   - Normal fill: fully transparent
   - Normal outline: black
   - Selected fill: white
   - Selected outline: white or another obvious color
4. Ensure the rule is above other animal rules.
5. Close the editor.
6. Zoom out until the animal is rendered as a Camera+ silhouette.
7. Select the animal.

Expected behavior if the UI is literal:

- The selected silhouette fill turns white.

Current compatibility behavior:

- The selected outline changes, but the fill stays transparent/pawn-colored.

Failure/effect to document:

- The selected fill color shown in the rule editor is not honored for this exact animal silhouette case.

Control test:

1. Change selected fill from pure white to a clearly non-white color, such as bright red.
2. Select the animal again.
3. If red is honored while white is ignored, the exact-white compatibility guard is confirmed.

Cleanup:

1. Remove the test rule or restore default rules.

### Solution Proposal

Migrate known legacy/default animal silhouette rules away from selected white fill, then remove or narrow the runtime exact-white compatibility guard. The runtime should honor explicit user color choices after migration.

Preferred fix:

1. Identify the known broad default animal silhouette rule by its conditions and key values, not merely by having `fillSelectedColor == Color.white`.
2. Change that rule's selected fill to transparent during migration.
3. Keep selected outline white if that is still the desired selected-state outline.
4. Remove `ShouldPreserveSelectedAnimalSilhouetteFill()` after migration, or restrict it to only rules that have been positively identified as old defaults and not user-authored rules.

Avoid:

- Do not leave the exact-white guard as permanent behavior. It makes white selected fill unconfigurable.
- Do not migrate every animal rule with selected white fill. Some users may have intentionally configured that color.

## 11. Existing Default Rules And Save Rules Are Not Migrated

### Problem

The code changed `CameraSettings.defaultDefaultConfig`, but existing `CameraPlusDefaultRules.xml` files are only created when missing. Existing default rule files and existing save-specific `CameraSettings.dotConfigs` retain old values, including selected white fill for animal silhouettes.

Relevant code:

- `Source/CameraSettings.cs`: `defaultDefaultConfig`
- `Source/CameraSettings.cs`: `InitDefaultDefaults`
- `Source/CameraSettings.cs`: `ExposeData`
- `Source/Dialog_Customization.cs`: rule editor uses raw saved colors

### Why It Matters

New installs get the new default config. Existing players keep older config semantics. The runtime guard hides part of the selected-fill issue, but the editor and saved XML still show stale values.

### Manual Test Case

Goal: prove that an existing default rules file keeps the old selected white fill.

1. Close RimWorld.
2. Locate the RimWorld config file:
   - macOS usual path: `~/Library/Application Support/RimWorld/Config/CameraPlusDefaultRules.xml`
3. Open the file and find the broad `AnimalTag` rule.
4. Look for `fillSelectedColor`.
5. Start RimWorld with the current Camera+ build.
6. Open `Options -> Mod Settings -> Camera+ -> Rules` from the main menu, before loading a save.
7. Inspect the broad animal rule's selected fill color.

Expected behavior after a migration:

- Existing default rules would be updated or explicitly migrated to the new intended value.

Current behavior:

- The existing XML still contains the old selected white fill.
- The rule editor displays selected white fill even though runtime may special-case it.

Save-specific variation:

1. Load an older save.
2. Open the in-game Camera+ rules.
3. Inspect the save-specific broad animal rule.
4. It can still contain selected white fill independent of the default rules file.

Cleanup:

1. If you manually edit the default rules file, keep a backup.
2. Use `Restore to default settings` only if you are okay replacing the current rule list.

### Solution Proposal

Add a small, idempotent migration layer for both the global default rules file and save-specific `CameraSettings.dotConfigs`. The migration should change only rules that match known historical default signatures.

Preferred fix:

1. Introduce a Camera+ rules/config version separate from general mod settings if one does not already exist for rule migrations.
2. On startup, load `CameraPlusDefaultRules.xml`, migrate known default rule signatures, and save the file only if it changed.
3. On world load, migrate `CameraSettings.dotConfigs` in the same conservative way.
4. Match old defaults by condition set and relevant color/edge/mouse values before changing them.
5. Do not rewrite user-authored custom rules that merely resemble animal rules but differ in colors, modes, thresholds, or conditions.

Avoid:

- Do not delete and recreate `CameraPlusDefaultRules.xml` for existing users. That would erase customization.
- Do not rely only on the runtime selected-fill guard. It leaves the editor and serialized rules misleading.

## 12. Edge Ordering Buckets Do Not Exactly Match All Pawn Types

### Problem

The requested edge draw order was:

1. Colonists
2. Colony animals
3. Enemies
4. Friendlies
5. Non-colony animals

The current classification checks animal status first, so all non-player animals go into the non-colony animal bucket even if hostile. It also classifies any player-faction non-animal as colonist, including possible player mechs, slaves, guests, vehicles, or other player-faction pawns that are not strictly colonists.

Relevant code:

- `Source/DotDrawer.cs`: `EdgeLayerFor`
- `Source/DotDrawer.cs`: edge command sorting

### Why It Matters

The visual stacking order can be surprising in raids, manhunter packs, mechanoid situations, or modded pawn types. A hostile animal may be pushed below friendly pawns even though the requested order says enemies should be above friendlies and non-colony animals.

### Manual Test Case A: Hostile Animal Versus Friendly Pawn

Goal: show that a hostile animal is treated as non-colony animal rather than enemy for edge stacking.

1. Load a test map.
2. Use dev mode to create or trigger a hostile/manhunter animal near the edge of the camera view.
3. Place a friendly non-colony pawn or allied visitor so their off-screen edge indicators overlap the hostile animal's edge indicator.
4. Ensure `Edge indicators` is on.
5. Zoom/scroll until both pawns are off-screen in the same direction and their edge circles overlap.
6. Observe which circle renders on top.

Expected behavior from requested order:

- Enemy hostile animal should render above friendly pawns.

Current behavior to look for:

- Hostile animal renders below friendly pawns because all non-player animals are bucketed last.

Cleanup:

1. End the mental state or delete spawned test pawns.

### Manual Test Case B: Player-Faction Non-Colonist

Goal: show that player-faction non-colonist pawns are treated as colonists for edge stacking.

1. Load a map with a player-faction pawn that is not `IsColonist`, such as a mech, slave, vehicle pawn, or another modded player-faction pawn.
2. Place it off-screen with an actual colonist and a colony animal so their edge indicators overlap.
3. Ensure `Edge indicators` is on.
4. Observe the stacking order.

Expected behavior if "colonists" is strict:

- Only true colonists are in the top bucket.
- Other player-faction non-animals should have an explicitly chosen bucket.

Current behavior:

- The player-faction non-colonist is bucketed with colonists.

Cleanup:

1. No persistent cleanup needed unless test pawns were spawned.

### Solution Proposal

Make edge layer classification explicit and hostile-first where it matters. The ordering should express the requested visual priority rather than relying on broad faction shortcuts.

Preferred fix:

1. Keep true player colonists in the top layer.
2. Keep player-faction animals in the colony-animal layer.
3. Classify hostile pawns, including hostile animals and manhunters, into the enemy layer before the non-colony-animal fallback.
4. Classify non-hostile, non-player, non-animal pawns as friendlies.
5. Classify non-hostile, non-player animals as non-colony animals.
6. Decide explicitly where player-faction non-colonist non-animals belong; the least surprising default is below colonists but above enemies only if they are player-controllable, otherwise friendlies.

Avoid:

- Do not classify all animals before hostility. That is the current source of hostile animals rendering too low.
- Do not treat all `Faction.IsPlayer` non-animals as colonists unless the UI wording is changed from "colonists" to "player faction pawns".

## 13. Edge Ordering Adds Avoidable Hot-Path Cost

### Problem

Every frame with edge indicators sorts all edge draw commands. There are only five logical buckets, so a fixed bucket pass would produce deterministic order without `O(n log n)` sorting.

In addition, `MarkerCache.MaterialFor()` now creates a separate `edgeDot` material for every pawn that has marker textures, even if that pawn never currently draws an edge indicator.

Relevant code:

- `Source/DotDrawer.cs`: `edgeDrawCommands.Sort(...)`
- `Source/DotDrawer.cs`: `EdgeLayerFor`
- `Source/MarkerCache.cs`: creation of `dot` and `edgeDot`
- `docs/PERFORMANCE_REVIEW_BASELINE.md`: hot path notes

### Why It Matters

The last two commits before the color fix were specifically about speed and CPU usage. Sorting hundreds of edge markers every draw, and doubling dot materials for pawns that may never be off-screen, works against that direction.

### Manual Test Case

Goal: quantify whether edge sorting/material creation regresses the known edge stress scenario.

1. Build a normal release.
2. Start RimWorld with the current Camera+ build.
3. Load or create a high-pawn test save with many off-screen markers. The existing `CameraPlusPerf_962Pawns_EdgeDots` scenario described in `docs/PERFORMANCE_SCENARIOS.md` is ideal if available.
4. Enable Camera+ perf collection if using the existing perf build workflow.
5. Set the camera so hundreds of edge indicators are visible.
6. Let the game run for the same number of draws used in the baseline, for example 600 draws.
7. Capture metrics for:
   - `DotDrawer.DrawDots`
   - `DotDrawer.DrawClipped`
   - `dotdrawer.edge_draws`
   - `marker_cache.misses`
   - `marker_cache.hits`
8. Compare against the baseline numbers in `docs/PERFORMANCE_REVIEW_BASELINE.md` and `docs/PERFORMANCE_SCENARIOS.md`.

Expected behavior:

- Edge ordering should not significantly regress the edge-heavy baseline.

Potential failure/effect:

- `DotDrawer.DrawDots` average increases in high-edge scenarios.
- Material cache memory usage increases because every pawn gets an extra edge material at creation time.

Functional control:

1. Repeat with edge indicators off.
2. Repeat with a small number of pawns.
3. The cost should scale primarily with number of edge indicators.

Cleanup:

1. Remove any spawned perf pawns or reload the original save.

### Solution Proposal

Replace per-frame sorting with fixed priority buckets and create edge materials lazily. The visual order has a small fixed number of layers, so it does not need general-purpose sorting.

Preferred fix:

1. Replace `edgeDrawCommands.Sort(...)` with five reusable lists or one reusable array of lists keyed by edge layer.
2. Add commands directly to their bucket during pawn scanning.
3. Draw buckets from lowest priority to highest priority, preserving stable insertion or `thingIDNumber` order inside a bucket if deterministic tie-breaking is still needed.
4. Split dot and edge material creation so `edgeDot` is created only when an edge indicator is actually about to be drawn.
5. Re-run the edge-heavy perf scenario after the functional edge-order fixes land.

Avoid:

- Do not leave sorting in place just because it is simple. The bucket model is also simple and better matches the fixed priority rule.
- Do not pre-create edge materials for every pawn if edge indicators are off or if the pawn never leaves the view.

## 14. Static Marker Cache Can Retain Pawns And Materials Across Lifecycle Changes

### Problem

`MarkerCache.cache` is a strong dictionary keyed by `Pawn`. I only found cache clears for outline-factor changes and custom marker reloads. If pawns despawn, maps unload, saves change, or games are loaded repeatedly in one RimWorld session, old pawn keys and materials can remain alive.

The new `edgeDot` material doubles part of the retained material cost.

Relevant code:

- `Source/MarkerCache.cs`: `cache`
- `Source/MarkerCache.cs`: `Clear`
- `Source/MarkerCache.cs`: `Remove`
- `Source/Settings.cs`: outline-size clear
- `Source/Assets.cs`: custom marker reload clear

### Why It Matters

Long play sessions, caravan/map changes, raids, and repeated test loads can accumulate stale Unity materials. This is especially risky for a marker-heavy mod where every visible pawn can allocate several materials.

### Manual Test Case

Goal: observe whether marker cache entries survive after pawns or maps are gone.

1. Add temporary debug logging or a debug action that prints:
   - `MarkerCache.cache.Count`
   - Number of cached pawns whose `Map` is null
   - Number of cached pawns not in any current `map.mapPawns.AllPawnsSpawned`
2. Build and start RimWorld.
3. Load a save with many pawns and zoom out until markers are created.
4. Record the cache count.
5. Leave the map or load a different save in the same RimWorld process.
6. Record the cache count again.
7. Spawn a group of pawns, zoom out to create marker materials, then despawn/delete them.
8. Record the cache count again.

Expected correct behavior:

- Cache should shrink or clear when pawns are no longer relevant.
- No cached entries should remain for pawns from an unloaded game/map.

Failure behavior to look for:

- Cache count keeps increasing across loads/spawns/despawns.
- Entries remain for pawns with null maps or no current map membership.

Cleanup:

1. Remove temporary debug logging/actions before committing.
2. Restart RimWorld to clear static state.

### Solution Proposal

Add explicit lifecycle cleanup for marker and color caches, plus in-game culling signals for long sessions. Static caches should not depend on process restart or save/load to stay healthy.

Preferred fix:

1. Clear `MarkerCache`, marker decision caches, and appearance/color caches when a game or world is unloaded or a different world becomes active.
2. Add carefully scoped Harmony patches on low-volume RimWorld lifecycle signals such as pawn despawn/destroy and map removal, and call `MarkerCache.Remove(pawn)` for affected pawns.
3. Keep a low-frequency safety prune as a fallback for missed signals, removing pawns with null maps, destroyed state, or no membership in any active map's spawned pawn list.
4. Destroy Unity materials through the existing `MarkerCache.Remove()` path whenever pruning a pawn.
5. Keep cache cleanup separate from rule migrations so lifecycle cleanup is safe even when no settings changed.

Avoid:

- Do not wait for material cache mismatches to remove despawned pawns. Despawned pawns may never be queried again.
- Do not use weak references for Unity materials as the primary cleanup mechanism. The cache owns materials and should destroy them deterministically.
- Do not rely only on per-frame scanning. It is safe as a fallback, but event-driven removal is lower impact for long play sessions.

Interactions:

- Cache culling should land after marker material signatures are fixed, so `Remove` and rebuild behavior stays deterministic.

## 15. "Pawn-Colored Edge Indicators" Label Is Broader Than The Behavior

### Problem

The setting label says pawn-colored edge indicators, but the implementation only applies the pawn-color fallback for animals. Non-animal pawns with transparent fill do not use their pawn color.

Relevant code:

- `Source/Settings.cs`: checkbox label
- `Source/DotTools.cs`: `GetEdgeFillColor`
- `Languages/*/Keyed/Text.xml`: translated setting text

### Why It Matters

The label now implies a general pawn behavior. The code implements animal-specific behavior, which was the original bug target. That is a small UX mismatch, but it becomes visible if a user creates transparent-fill rules for humans, mechs, entities, or modded non-animal pawns.

### Manual Test Case

Goal: show that the setting only affects animals.

1. Load a map with one colonist and one animal.
2. Open Camera+ rules.
3. Add a high-priority colonist rule:
   - Condition: `Colonist`
   - Mode: `Camera+ silhouettes` or `Camera+ dots`
   - Normal fill: fully transparent
   - Normal outline: black
   - Edge: enabled
4. Add or keep an animal rule with transparent fill and edge enabled.
5. Turn `Pawn-colored edge indicators` on.
6. Move the camera so both the colonist and animal are off-screen and edge indicators are visible.
7. Compare the fill behavior.

Expected behavior from the label:

- Both pawn edge indicators follow their pawn colors when their rule fill is transparent.

Current behavior:

- Animal edge indicator uses pawn color.
- Colonist edge indicator keeps the transparent fill behavior, which usually appears as a white/unfilled marker depending on texture/shader.

Control test:

1. Turn `Pawn-colored edge indicators` off.
2. The animal should stop using pawn color.
3. The colonist behavior should not change.

Cleanup:

1. Remove the test colonist rule.

### Solution Proposal

Rename the setting to match its current and intended scope instead of broadening it to every pawn type. The problem was animal edge indicators losing body color; non-animal pawns already use state/rule colors where "pawn color" is ambiguous.

Preferred fix:

1. Rename the key and translations from `Pawn-colored edge indicators` to an animal-specific wording, such as `Animal-colored edge indicators` or `Animal edge colors`.
2. Keep the implementation animal-only unless a separate design explicitly defines what "pawn color" means for colonists, enemies, mechs, entities, and modded pawns.
3. If backward-compatible settings serialization matters, keep the serialized bool name `pawnColoredEdgeIndicators` and only change the display text.
4. Update `FIX.md`/release notes after implementation to state that the setting controls transparent-fill animal edge markers.

Avoid:

- Do not broaden the feature to all pawns now. That would conflict with selected, drafted, downed, hostile, and custom-rule colors.
- Do not leave the current wording. It invites users to expect behavior the code intentionally does not provide.

## 16. Rule Mode `Off` Can Suppress Vanilla Rendering Without Drawing A Marker

### Problem

The rule editor exposes `DotStyle.Off`, and `MarkerDecision.ShouldSuppressVanilla()` treats any rule-backed mode other than `VanillaDefault` as eligible for vanilla suppression when `useInside` is true and the zoom threshold is met. `DotDrawer.DrawDots()` has no draw case for `DotStyle.Off`, so a matching rule can suppress the vanilla pawn body and then draw no Camera+ marker.

Relevant code:

- `Source/DotStyle.cs`: `Off = -1`
- `Source/Dialog_Customization.cs`: rule mode menu includes all modes except `Custom`
- `Source/MarkerDecision.cs`: rule-backed vanilla suppression
- `Source/DotDrawer.cs`: draw switch only handles classic dots, silhouettes, and custom markers

### Why It Matters

Users can reasonably interpret rule mode `Off` as "Camera+ should not handle this pawn". The current behavior can instead mean "hide this pawn at marker zoom", which is a dangerous hidden-pawn mode without explicit UX.

### Manual Test Case

Goal: show that a rule mode can suppress vanilla rendering without producing a replacement marker.

1. Load a map with a visible colonist or animal.
2. Open `Options -> Mod Settings -> Camera+ -> Rules`.
3. Add a high-priority rule matching that pawn, for example `Colonist` or `Animal`.
4. Set the rule mode to `Off`.
5. Leave `Inside` enabled.
6. Close the editor and zoom out until the marker threshold is crossed.
7. Observe the pawn body.

Expected correct behavior:

- The pawn should fall through to vanilla rendering when Camera+ is off for that rule.

Failure behavior to look for:

- The pawn body disappears and no Camera+ marker replaces it.

Cleanup:

1. Remove the test rule.

### Solution Proposal

Define rule mode `Off` as "do not replace this pawn; fall through to vanilla rendering". If a future feature should intentionally hide pawns, it should be a separate explicit rule mode or checkbox with clear wording.

Preferred fix:

1. Treat `DotStyle.Off` like `VanillaDefault` for vanilla suppression: suppression should be false.
2. Treat `DotStyle.Off` as no inside marker and no edge marker unless the UI later defines separate edge behavior for off rules.
3. Consider hiding `useInside`/color controls for `Off` in the rule editor if they do not apply.
4. Add a regression test/manual check that a matching `Off` rule leaves vanilla pawn rendering visible.

Avoid:

- Do not preserve hidden-pawn semantics for `Off`. The UI does not communicate that behavior.
- Do not fix this only in `DotDrawer`; suppression must be corrected at the decision layer so all Harmony prefixes agree.

## 17. Animal Marker Policy Is Split Across Settings, Rules, And Marker Textures

### Problem

Animal marker behavior is currently distributed across global settings, default rules, rule matching, texture selection, edge rendering, and color fallback. The separate issues above are symptoms of the same structural problem: there is no single place that answers what Camera+ intends to do with a given animal.

Examples:

- `customNameStyle` controls animal inclusion, hidden UI state, and marker texture family.
- `includeNotTamedAnimals` is only honored when no rule matched.
- Default animal rules can override global settings without the main settings UI making that clear.
- Edge marker textures still depend on animal marker style even when in-map silhouettes are active.
- Animal edge color fallback uses rule fill transparency and a separate global checkbox.

### Why It Matters

Fixing the symptoms one by one can keep producing new inconsistencies. A small policy layer would make the intended behavior explicit and reduce the chance of future UI/cache/rendering mismatches.

### Manual Test Case

Goal: expose the current split by changing one animal setting and observing multiple unrelated surfaces.

1. Load a map with a named colony animal and an unnamed wild animal.
2. Use `Camera+ silhouettes`.
3. Toggle `Animals have the same marker`, `Animals have a different marker`, and `Animals have no marker`, using `Camera+ dots` temporarily if needed to reach the hidden third state.
4. Toggle `Include not tamed animals`.
5. Move animals on-screen and off-screen.
6. Compare in-map silhouettes, edge marker shape, edge fill color, vanilla body suppression, and rule editor defaults.

Expected correct behavior:

- Each setting should have a clear, consistent scope.
- A user should be able to predict whether animals are included, which marker texture family is used, and whether rules apply.

Failure behavior to look for:

- Settings affect hidden edge marker behavior.
- Default rules continue applying after a global exclusion setting.
- The UI cannot display the active state.

### Solution Proposal

Introduce a small animal marker policy helper and route all animal-specific decisions through it. This should be a consolidation, not a large UX rewrite.

Preferred fix:

1. Add a helper that returns an animal policy for a pawn: included/excluded, tame/wild status, marker texture family, default-rule eligibility, edge-color eligibility, and visible UI state.
2. Use that helper from rule selection, marker decision creation, default marker texture selection, and edge color fallback.
3. Keep existing public settings, but clarify their semantics through the helper.
4. Let custom rules still style animals, but only after the global inclusion policy says the pawn is eligible.
5. Use the same helper to decide whether caches must be cleared when animal settings change.

Avoid:

- Do not solve this with scattered one-line checks in every caller. That is how the current conflicts developed.
- Do not replace the rule system wholesale. The needed change is a small policy boundary around animal-specific behavior.

## 18. Camera Height Can Silence Close-Range World Sounds

### Problem

GitHub issue `#41` reports that Royalty musical instruments are audible without Camera+, but become silent with only Camera+ plus the official DLCs enabled. The reporter tested the Camera+ sound-nearness setting at `0%`, `50%`, and `100%`, and it did not restore the instrument sound.

This points to a Camera+ behavior bug, not a user error. It is also unlikely to be a third-party mod conflict if the reproduction really uses only DLCs and Camera+.

Relevant vanilla code, checked against the live RimWorld assembly with the decompiler:

- `RimWorld.JobDriver_PlayMusicalInstrument` starts `Building_MusicalInstrument.StartPlaying(pawn)`.
- `RimWorld.Building_MusicalInstrument.Tick()` spawns `def.soundPlayInstrument.TrySpawnSustainer(SoundInfo.InMap(new TargetInfo(Position, Map), MaintenanceType.PerTick))`.
- `Verse.Sound.SampleSustainer.TryMakeAndPlay()` parents the Unity `AudioSource` to the instrument's world-root object, sets `spatialBlend = 1f`, and uses the sound def's `distRange`.
- Royalty `Harp_Play`, `Harpsichord_Play`, and `Piano_Play` all use `distRange` `5~25`.
- Vanilla `Verse.CameraDriver.ApplyPositionToGameObject()` places the camera at y=`15` at closest zoom and y=`65` at vanilla furthest zoom.

Camera+ currently replaces that camera height with:

```csharp
float idealY = newOrthographicSize * 1.6f + 25.6f;
float minYForSound = CameraPlusSettings.minRootResult * 1.6f + 25.6f;
currentPos.y = Mathf.Lerp(idealY, minYForSound, Settings.soundNearness);
```

With the default `zoomedInPercent = 1`, `minRootResult = 2`, so the closest Camera+ camera height is `28.8`. That is already above the `25` max distance of the Royalty instrument sustainers before any horizontal distance is added. Because the Unity audio listener is on the camera object, the instrument can fade to silence even when visually zoomed in on the instrument.

The current `soundNearness` setting cannot fix this, because its `100%` target is also the Camera+ closest height (`28.8` by default), not the vanilla closest listener height (`15`).

### Why It Matters

This is a general audio-listener compatibility problem, not just a piano problem. Any world sound with a small `distRange` can become quieter or silent when Camera+ raises the camera object above vanilla's listener-height envelope.

It also makes the existing `Sound nearness` option misleading. The option sounds like it should help with local sounds, but under the default settings it cannot bring the listener back inside the vanilla close-range audio envelope.

### Manual Test Case

Goal: reproduce issue `#41` and show that the failure follows Camera+ camera height rather than user setup.

1. Start RimWorld with `Harmony`, `Core`, `Royalty`, and Camera+ enabled. Keep other third-party mods disabled for the control pass.
2. Start or load any map with a colonist.
3. Enable dev mode.
4. Spawn a `Piano`, `Harpsichord`, or `Harp`.
5. Use god mode or dev tools to make a pawn play the instrument, or select the instrument and use its dev gizmo if available.
6. Zoom fully in so the instrument is centered on screen.
7. Listen for the instrument sustainer.

Expected vanilla behavior:

- With Camera+ disabled, the instrument should be audible when zoomed in near it.

Failure behavior to look for:

- With Camera+ enabled and default settings, the instrument is silent or much quieter even at full visual close zoom.

Control tests:

1. Repeat with Camera+ disabled and the same DLC set.
2. Repeat with Camera+ enabled and `Sound nearness` set to `0%`, `50%`, and `100%`.
3. Repeat with the instrument centered under the camera and then several cells away horizontally.
4. Repeat with a non-instrument close-range world sound if a convenient one is available, to confirm this is not specific to `Building_MusicalInstrument`.

Useful diagnostic observations:

- Royalty instrument sounds have max audio distance `25`.
- Vanilla closest camera y is `15`.
- Current Camera+ default closest camera y is `28.8`.
- If a test build logs `Find.Camera.transform.position.y`, the sound should correlate with whether the listener is inside or outside the sound's distance envelope.

Cleanup:

1. Remove the spawned instrument if testing in a real save.
2. Restore the normal mod list after the control pass.

### Solution Proposal

Restore vanilla-compatible listener height semantics while keeping Camera+'s visual zoom range. The first pass should be small and measured, because `CameraDriver.ApplyPositionToGameObject` affects camera feel, clipping, mouse-to-map projection, reverb, and audio.

Preferred fix:

1. Replace the current audio-height target with a vanilla-height calculation based on the raw `driver.rootSize` input range:
   - closest input root size maps to y=`15`
   - furthest vanilla input root size maps to y=`65`
2. Apply `Sound nearness` by lerping from that vanilla-compatible height toward y=`15`, not toward `minRootResult * 1.6f + 25.6f`.
3. Keep `camera.orthographicSize = Tools.LerpRootSize(driver.rootSize)` so Camera+ visual zoom remains extended.
4. Verify that close, middle, far, and furthest zoom still have correct map projection, mouse-to-map position, selection, culling, clipping, edge scrolling, and screenshot framing.
5. Verify that Royalty `Piano_Play`, `Harpsichord_Play`, and `Harp_Play` are audible at close zoom with default Camera+ settings.
6. If the visual camera genuinely requires a higher transform y for extreme zoom-out clipping, split the problem explicitly: keep rendering correct, but move or manage the audio listener separately so world sounds still use a vanilla-compatible listener height.

Avoid:

- Do not patch Royalty instrument sound defs or increase their `distRange`. The issue is Camera+'s listener position and can affect other close-range world sounds.
- Do not make users rely on `Sound nearness = 100%` to restore vanilla close-zoom audio. The default close-zoom behavior should already be compatible.
- Do not add another user-facing sound workaround before the existing `Sound nearness` semantics are corrected.

Interactions:

- Changing camera transform y may affect `UI.MouseMapPosition()`, camera clipping planes, and any mod that reads `Find.Camera.transform.position.y`.
- `SoundParamSource_CameraAltitude` reads `Find.Camera.transform.position.y`, so changing the height formula can affect sounds that intentionally map parameters from camera altitude. This is still preferable to keeping local world sounds outside their vanilla distance envelope, but it must be tested.
- `reverbDummy` is positioned by vanilla `ApplyPositionToGameObject`; Camera+ currently does not preserve that exact behavior. Any fix should check whether reverb dummy positioning still makes sense after listener height is corrected.

## Suggested Fix Order

1. Fix marker ownership first: `MarkerDecision` must not suppress vanilla rendering unless Camera+ will draw a real replacement marker, and `DotStyle.Off` must fall through to vanilla.
2. Consolidate animal policy next: define global inclusion, visible animal marker choices, default-rule eligibility, edge marker texture family, and animal edge-color behavior in one helper.
3. Fix the animal settings UI and restore-defaults behavior once all meaningful animal states are visible and policy-backed.
4. Make rule tags null-safe and fix nullable-bool negation precedence.
5. Fix camera/audio listener height separately from the marker work, with visual camera regression checks before and after the audio test.
6. Add conservative rule/default migrations for known historical defaults, then remove or narrow the selected-animal white-fill runtime guard.
7. Improve color and material cache signatures, and add cache invalidation for settings that affect rendering.
8. Add runtime cache culling through lifecycle clears, low-impact pawn/map signals, and a low-frequency fallback prune.
9. Tighten edge ordering category semantics, then replace sorting and eager edge material creation with bucketed/lazy behavior.
10. Rename the animal edge-color setting text after the behavior scope is settled.
