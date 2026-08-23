# CameraPlus TODO

This file tracks only the actionable residuals from the camera-input and
settings-dependency audit. Completed foundations, historical investigation,
accepted dependencies, and validation transcripts have been removed.

## 1. Decouple keyboard movement from edge scrolling

**Outcome: fix. Priority: highest.**

Edge-scroll values must affect only screen-edge movement. Setting both edge
endpoints to `0x` must disable edge scrolling without disabling WASD, arrow
keys, or rebound `MapDolly` keys.

- [x] Remove edge scaling from the combined
      `CameraDriver.CalculateCurInputDollyVect()` result.
- [x] Derive `config.dollyRateKeys` from ordinary movement settings only.
- [x] Apply the edge multiplier only to `config.dollyRateScreenEdge`.
- [x] Preserve the current default keyboard and edge-scroll calibration.
- [x] Rename the screen-edge helper and correct the movement/edge help text.
- [ ] Validate zero/nonzero edge endpoints, close/middle/far zoom, Shift,
      rebound movement keys, and middle-mouse drag.

## 2. Make pawn-label behavior rule-aware

**Outcome: fix. Priority: high.**

Pawn bodies and markers already use the cached `MarkerDecision`; pawn-label
suppression still uses global marker settings. Labels must follow the effective
per-pawn rule without losing the independent `hidePawnLabelBelow` threshold.

- [x] Base pawn-label marker suppression and mouse reveal on the cached
      `MarkerDecision`.
- [x] Keep thing/stack labels on their existing global path.
- [x] Preserve labels when a custom marker asset is missing.
- [ ] Validate global/rule marker modes, thresholds, mouse reveal, animal
      policy, and missing custom-marker fallback.

## 3. Expose floating-text suppression explicitly

**Outcome: fix representation without changing existing defaults.**

General map floating text is currently suppressed outside the closest zoom
when a Camera+ marker style is active. Give that behavior an independent,
clearly named setting, defaulting to the current enabled behavior for existing
players.

- [x] Add and persist an explicit floating-text suppression setting.
- [x] Stop using marker style as the setting's implicit on/off switch.
- [x] Preserve the existing closest-zoom and mouse-reveal behavior while the
      setting is enabled.
- [ ] Validate enabled/disabled behavior across zoom levels and mouse distance.

## 4. Keep the dead-pawn threshold editable

**Outcome: fix the UI state.**

`hideDeadPawnsBelow` remains active at runtime even when the global marker style
is Vanilla, so its settings row must remain enabled and describe its effect on
corpse bodies and related overlays.

- [x] Remove the global-marker-style enablement dependency from the row.
- [ ] Validate switching between Vanilla and Camera+ marker styles.

## 5. Represent rule-enabled animal edge colors correctly

**Outcome: fix the residual UI state.**

Animal edge-color rendering already works. The remaining mismatch is that the
color control is disabled when global edges are off even though a matching rule
can still enable an animal edge.

- [x] Keep the color option editable whenever rules may enable edges, or leave
      it always editable and explain when it matters.
- [x] Keep rule-fill override notes accurate when only rules enable edges.
- [ ] Validate global-off/rule-on and global-on/rule-off combinations.

## 6. Make disabled shortcut bindings explicit

**Outcome: clarify the existing safe behavior.**

Two empty modifier slots intentionally disable a shortcut; the settings UI
must not imply that this creates a modifier-free Tab or number binding.

- [x] Display the all-empty modifier state as disabled in the row/help text.
- [x] Preserve the runtime guards against unmodified Tab/number conflicts.
- [x] Warn or document when load and save use the same modifier combination.

## 7. Document the Shift override for zoom-to-mouse

**Outcome: document the existing behavior.**

Holding Shift bypasses zoom-to-mouse even when the setting is enabled.

- [ ] Add the Shift bypass to the setting help.
- [ ] Confirm the modifier remains deliberate and does not conflict with a
      Camera+ shortcut.

## 8. Remove dead `stickyMiddleMouse` persistence

**Outcome: cleanup without migration machinery.**

The historical custom middle-drag implementation is already gone. Only the
unused field and settings XML entry remain; neither has a UI or runtime reader.

- [ ] Remove the field and `Scribe_Values.Look` entry.
- [ ] Remove any remaining production or translation references.
- [ ] Verify older settings XML containing `<stickyMiddleMouse>` still loads
      and the obsolete element disappears on the next save.
- [ ] Confirm ordinary middle-mouse dragging is unchanged.

## Completion boundary

For each behavior-changing slice: update `PENDING_RELEASE_NOTES.md`, run the
focused static/build checks, and commit only that slice. After all slices are
implemented, deploy the combined build once, restart RimWorld, and validate the
complete matrix through RimBridgeServer before removing this file.
