# Camera+ UX v2

This document specifies the next main settings UI for Camera+. It is meant to be implementation guidance, not a loose design sketch.

The goal is to make the main Camera+ mod settings dialog readable, educational, familiar to current users, and robust in longer translations while keeping the existing RimWorld mod settings window size. The rule editor, color picker, and rule customization sub-dialogs stay separate. Keyboard shortcut settings are inline in the main dialog so the old extra shortcut button is no longer needed.

## Why This Exists

The current main settings dialog is a single hard-coded two-column `Listing_Standard` flow in `CameraPlusSettings.DoWindowContents()`. It mixes zoom, movement, label, marker, animal, edge, appearance, rules, and shortcut actions into one cramped surface.

That layout worked when Camera+ had fewer options. It now hides too much meaning in short labels and suffixes such as "overwritten by ...", and it has little room for translated labels that are longer than English.

UX v2 should solve that by using:

- topic navigation on the left;
- one scrollable settings column in the middle;
- contextual help on the right;
- command buttons outside the topic-filtered content.

## Continuity For Existing Users

Current Camera+ users should feel reasonably at home in UX v2. The redesign should make the settings easier to understand, not make users relearn Camera+ from scratch.

Rules:

- Keep existing setting concepts and defaults recognizable.
- Keep familiar labels where they are already clear enough.
- Rename labels only when the current wording is misleading or too cramped to explain the real behavior.
- Keep the default `All` view close to the current mental order: zoom, movement, camera behavior, audio, labels, markers, animals, edge indicators, appearance, then keyboard shortcuts.
- Keep `Keyboard shortcuts`, `Rules`, and `Restore defaults` easy to find from the first screen.
- Do not move rule editing into the main settings flow.
- Do not introduce new user-facing setting categories unless they help map existing settings to a clearer structure.
- When a setting changes wording because of a `FIX.md` behavior clarification, the help text should connect it to the familiar old concept.

Examples:

- `Zoom`, `Zoom to mouse`, `Disable camera shake`, `Keyboard shortcuts`, and `Rules` can keep their existing names.
- `Pawn-colored edge indicators` should be renamed only if the behavior remains animal-specific or otherwise narrower than the label.
- `Animals have a different marker` may become clearer as `Animals use animal markers`, but it must still be visibly the same animal-marker choice current users know.

## Constraints

- Keep using RimWorld's normal mod settings window. Do not require a larger custom settings window for this pass.
- The useful content area is limited by `Dialog_ModSettings`; the design must fit roughly inside the existing `900 x 700` window.
- Refactor only the main Camera+ settings dialog in this pass.
- Keep `Rules`, color picking, and rule editing in their existing sub-dialogs.
- Move keyboard shortcut settings into the main dialog as a normal settings topic.
- Do not change settings serialization solely for UX v2.
- Do not make the rule editor carry basic settings explanations. The main dialog should explain the global settings.
- Prefer clear wrapped text over compact English-only label tricks.

## Target Layout

Use a three-column layout inside the main settings content area:

1. Left: topic navigation.
2. Middle: scrollable settings content.
3. Right: contextual help.

The top row should be outside the three-column layout:

- left or center: optional small title/status area if useful;
- right: `Restore defaults`;
- nearby: `Rules`.

The top row action buttons are commands, not settings. They must stay visible regardless of the selected topic.

Recommended initial dimensions:

- Navigation column: `150-170 px`.
- Column spacing: `12 px`.
- Help column: `200-230 px`.
- Settings column: all remaining width.
- Top action row: `34-40 px`.

If the settings content becomes tight, reduce the navigation column before reducing the settings column. The middle column is the primary work area.

Do not add a visible "Topics" heading unless the navigation column looks unclear during implementation. The selected navigation item and the help title should provide enough context.

## Navigation Topics

Use these topic ids and labels for the first implementation:

| Topic id | Label | Purpose |
| --- | --- | --- |
| `all` | `All` | Shows every settings group in the canonical order. |
| `zoom` | `Zoom` | Visual zoom range and zoom curve. |
| `movement` | `Movement` | Keyboard/mouse scroll speed and screen-edge scrolling. |
| `audio` | `Audio` | Bring sounds closer / listener height behavior. |
| `camera` | `Camera` | Zoom-to-mouse and camera shake. |
| `labels` | `Labels` | Pawn, stack, dead pawn labels, and mouse label reveal. |
| `markers` | `Markers` | Marker style and marker threshold. |
| `animals` | `Animals` | Animal marker policy and untamed animal inclusion. |
| `edges` | `Edges` | Edge indicators and edge-color behavior. |
| `appearance` | `Appearance` | Marker sizes, edge distance, and outline width. |
| `keyboard` | `Keyboard` | Keyboard shortcut settings for opening settings and saving/loading views. |

Selection behavior:

- `All` is the default.
- Selecting a topic filters the middle column to the groups assigned to that topic.
- When changing topic, reset the middle scroll position to the top. This is simpler and avoids stale scroll offsets that show a blank area.
- The selected topic should be visually obvious.
- Topics with disabled settings should remain selectable. The disabled state should be explained in the middle column or help column, not hidden from navigation.

## Settings Groups

The middle column is a vertical scroll list of groups. Each group has:

- stable id;
- topic id;
- title;
- controls;
- default help text;
- optional setting-specific help text;
- optional rule-override note.

Canonical group order:

1. `zoom-range`
2. `zoom-curve`
3. `movement-speed`
4. `edge-scroll`
5. `camera-behavior`
6. `audio-listener`
7. `label-visibility`
8. `marker-style`
9. `animal-policy`
10. `edge-indicators`
11. `marker-appearance`
12. `keyboard-shortcuts`

### Zoom Range

Topic: `zoom`

Controls:

- Maximum zoomed in factor (`zoomedInPercent`)
- Maximum zoomed out factor (`zoomedOutPercent`)

Behavior:

- Keep the current clamping: zoomed-in value cannot exceed zoomed-out value.
- Preserve current behavior that snaps the current map camera to min/max root input when those limits change in-game.

Help intent:

- Explain that these values change Camera+'s visual zoom range, not the vanilla input zoom range.
- Warn gently that extreme values can affect visibility, clipping, or performance.

### Zoom Curve

Topic: `zoom`

Controls:

- Exponential zoom speed (`exponentiality`)

Behavior:

- Preserve the current `0 = Off` wording.
- Keep value rounding compatible with current behavior.

Help intent:

- Explain that this changes how quickly zoom accelerates across the range, not the min/max limits.

### Movement Speed

Topic: `movement`

Controls:

- Scroll speed for high zoom (`zoomedInDollyPercent`)
- Scroll speed for low zoom (`zoomedOutDollyPercent`)

Behavior:

- Do not use a nested mini two-column layout if labels become cramped.
- Prefer stacked controls with short value labels.

Help intent:

- Explain that these control keyboard/mouse drag camera movement speed at each zoom end.

### Edge Scroll

Topic: `movement`

Controls:

- Edge scroll factor for high zoom (`zoomedInScreenEdgeDollyFactor`)
- Edge scroll factor for low zoom (`zoomedOutScreenEdgeDollyFactor`)

Behavior:

- Preserve the current stored half-factor implementation detail unless the logic is cleaned up separately.
- Display values as user-facing multipliers.

Help intent:

- Explain that screen-edge scrolling is separate from normal camera scroll speed.

### Audio Listener

Topic: `audio`

Controls:

- Bring distant sounds closer (`soundNearness`)

Behavior:

- The UX must match the intended post-fix semantics from `FIX.md`: the setting adjusts audio listener nearness, not visual zoom.
- If the camera-height fix is not implemented yet, the help text should avoid promising behavior that the code does not yet provide.

Help intent:

- Explain that higher values make local world sounds behave as if the listener is closer to the map.
- Mention that it does not change visual zoom.

### Camera Behavior

Topic: `camera`

Controls:

- Zoom to mouse (`zoomToMouse`)
- Disable camera shake (`disableCameraShake`)

Help intent:

- Explain each as direct camera behavior.
- Keep this group small; do not mix label reveal or edge indicators into it.

### Label Visibility

Topic: `labels`

Controls:

- Mouse reveals labels (`mouseOverShowsLabels`)
- Hide pawn labels below (`hidePawnLabelBelow`)
- Hide stack labels below (`hideThingLabelBelow`)
- Hide dead pawns below (`hideDeadPawnsBelow`)

Behavior:

- If marker style is `Vanilla default`, label-threshold controls that require Camera+ marker mode should be disabled or clearly marked as not applicable.
- `Never` should stay available for zero values.
- Rule override notes must not be appended to the main label.

Help intent:

- Explain the difference between hiding labels and replacing pawns with markers.
- Explain that mouse reveal only applies where Camera+ is controlling label visibility.

### Marker Style

Topic: `markers`

Controls:

- Marker style radio group:
  - Vanilla default
  - Camera+ dots
  - Camera+ silhouettes
- Show as marker below (`dotSize`)

Behavior:

- Keep `Off` and `Custom` out of the global marker-style radio group unless a future feature gives them clear global semantics.
- When global style is `Vanilla default`, dependent marker controls should be visibly disabled with a short reason.
- Rule override notes must use secondary text.

Help intent:

- Explain that global marker style is the default for pawns not handled differently by rules.
- Explain that rules can override this for matching pawns.

### Animal Policy

Topic: `animals`

Controls:

- Animal marker radio group:
  - Animals use normal marker
  - Animals use animal marker
  - Animals use no marker
- Include untamed animals (`includeNotTamedAnimals`)

Behavior:

- Always show all meaningful animal choices for all marker styles where the value affects any visible behavior, including edge marker textures.
- This specifically fixes the hidden `AnimalsDifferent` state in silhouette mode.
- After `Restore defaults`, one visible animal choice must always be selected.
- The wording should make the global policy clear. Prefer "animal marker" over "different marker" if it reads better in implementation.

Help intent:

- Explain that this is a global animal inclusion/marker policy.
- Explain that rules may style eligible animals, but the global "include untamed animals" gate is meant to exclude unnamed wild animals before default animal rules pull them in.

### Edge Indicators

Topic: `edges`

Controls:

- Edge indicators (`edgeIndicators`)
- Animal edge colors, or final chosen label for `pawnColoredEdgeIndicators`

Behavior:

- The color-follow setting should be nested visually below edge indicators.
- If edge indicators are off, the color-follow setting should be disabled.
- The label must match actual scope. If the behavior remains animal-only, use animal-specific wording.
- Rule override notes must be secondary text.

Help intent:

- Explain that edge indicators are off-screen markers.
- Explain that edge indicators do not count as in-map marker replacements and must not imply vanilla pawn-body suppression.

### Marker Appearance

Topic: `appearance`

Controls:

- Dot/silhouette size (`dotRelativeSize`)
- Edge dot size (`clippedRelativeSize`)
- Edge distance (`clippedBorderDistanceFactor`)
- Outline width (`outlineFactor`)

Behavior:

- Preserve cache clearing when outline width changes.
- Display values as percentages where current UI does so.

Help intent:

- Explain that these are visual scale/spacing settings after marker selection has already happened.

### Keyboard Shortcuts

Topic: `keyboard`

Controls:

- Camera+ settings shortcut
- Load view shortcut
- Save view shortcut

Behavior:

- Shortcut rows live in the main settings column and use the same key picker controls as the old shortcut window.
- Labels should align vertically with their shortcut buttons.
- `Rules` and `Restore defaults` remain outside topic filtering in the top action row.

Help intent:

- Explain that view slots use number keys plus the configured modifier keys.
- Explain that this only changes Camera+ shortcuts, not RimWorld's general keyboard bindings.

## Control Patterns

### Sliders

Each slider row should have:

- label line;
- current value aligned predictably;
- actual slider below or beside the value depending on available width;
- optional secondary note line.

Labels must wrap. They must not be squeezed into a fixed English-width area.

### Checkboxes

Checkbox labels should wrap, with the checkbox anchored at the first line.

Dependent checkboxes should stay visible but disabled when their parent setting is off. Example: animal edge colors remains visible but disabled when edge indicators are off.

### Radio Groups

Radio options should be stacked vertically. Do not force radio labels into one horizontal row.

Every stored enum state that can affect visible behavior must have a visible option. Hidden stored enum values are not allowed in UX v2.

### Override Notes

Replace current suffixes such as:

```text
Mouse reveals labels (overwritten by 2 rules)
```

with secondary text:

```text
Mouse reveals labels
Overridden by 2 rules.
```

Rules:

- The main label should not include dynamic override text.
- The note should be visually muted.
- Use concise wording:
  - `Overridden by 1 rule.`
  - `Overridden by {0} rules.`
- Do not show a note when no rules override the setting.

### Disabled States

Disabled settings should explain why in either:

- a short inline note; or
- the help column when hovered.

Do not hide disabled settings if hiding them would make the setting model harder to understand.

## Help Column

The right column is contextual documentation.

Default states:

- If a control is hovered or focused, show that control's help.
- If no control is hovered, show the selected topic's help.
- If `All` is selected and nothing is hovered, show a short overview of Camera+ settings and recommend using the topics.

Layout:

- Header: `Help`.
- Subheading: `About {setting or topic}`.
- Body: wrapped small text.
- The body should fit inside the visible help column height. Do not add a second scroll view in the first implementation.

Style:

- Direct and practical.
- No tutorial prose.
- No marketing.
- No repeated explanation of obvious widgets.

Recommended first-pass help topics:

| Help id | Short content intent |
| --- | --- |
| `help.zoom` | Visual zoom range and curve. |
| `help.movement` | Normal movement speed versus edge scrolling. |
| `help.audio` | Audio listener nearness; does not affect visual zoom. |
| `help.camera` | Camera behavior toggles. |
| `help.labels` | Label visibility and mouse reveal. |
| `help.markers` | Global marker replacement behavior and rule overrides. |
| `help.animals` | Global animal marker policy and untamed animal gate. |
| `help.edges` | Off-screen markers; independent from in-map replacement. |
| `help.appearance` | Marker size, edge spacing, and outline width. |
| `help.shortcuts` | Shortcut keys for settings and saved views. |

## Translation Policy

UX v2 must assume translations are longer than English.

Implementation should be English-first:

- First implement the layout, wording, help text, and behavior using English keys.
- During the English-first pass, verify the UI model, topic grouping, help behavior, and setting semantics.
- Do not block the first UX implementation on final translations for every existing language.
- After the English UX is stable, add translated keys for all languages already present in the repo.
- Before release, every existing language should have translations for the new visible labels, notes, and help text, or an explicit fallback decision documented in the release notes.

Rules:

- Do not depend on exact English label width.
- Allow labels and notes to wrap.
- Do not concatenate large translated phrases with dynamic suffixes.
- Prefer separate translation keys for label, value text, override note, and help body.
- Keep help body keys short enough to translate safely, but informative enough to replace guesswork.
- Avoid punctuation-sensitive constructions where translators must place dynamic values inside long sentences.

Recommended key families:

```text
SettingsTopic_All
SettingsTopic_Zoom
SettingsGroup_ZoomRange
SettingsHelp_ZoomRange
SettingsHelp_ZoomedInPercent
SettingsNote_OverriddenByRule
SettingsNote_OverriddenByRules
```

Do not remove existing translation keys until all existing languages have replacement keys and the old UI no longer references them.

## Implementation Guidance

The implementation should replace the body of `CameraPlusSettings.DoWindowContents()` with a small UI model instead of adding more one-off drawing code.

Recommended internal structures:

```csharp
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
    Advanced
}

sealed class SettingsGroup
{
    public string Id;
    public SettingsTopicId Topic;
    public string TitleKey;
    public string HelpKey;
    public Action<SettingsUiContext> Draw;
}

sealed class SettingsUiContext
{
    public Rect Rect;
    public string CurrentHelpKey;
    public void SetHelp(string helpKey);
}
```

The exact implementation does not need to use these names, but it must preserve the model:

- topic list is data-driven;
- groups are data-driven;
- controls report help keys while drawing;
- drawing helpers own wrapping, value labels, disabled states, and override notes.

State to keep:

- selected topic;
- scroll position for the middle content;
- current help key;
- optionally last help key, to avoid flicker when moving over gaps.

For v1, use one scroll position and reset it to zero on topic change.

## Current Settings Mapping

| Current field | UX v2 group |
| --- | --- |
| `zoomedInPercent` | Zoom Range |
| `zoomedOutPercent` | Zoom Range |
| `exponentiality` | Zoom Curve |
| `zoomedInDollyPercent` | Movement Speed |
| `zoomedOutDollyPercent` | Movement Speed |
| `zoomedInScreenEdgeDollyFactor` | Edge Scroll |
| `zoomedOutScreenEdgeDollyFactor` | Edge Scroll |
| `soundNearness` | Audio Listener |
| `zoomToMouse` | Camera Behavior |
| `disableCameraShake` | Camera Behavior |
| `mouseOverShowsLabels` | Label Visibility |
| `hidePawnLabelBelow` | Label Visibility |
| `hideThingLabelBelow` | Label Visibility |
| `hideDeadPawnsBelow` | Label Visibility |
| `dotStyle` | Marker Style |
| `dotSize` | Marker Style |
| `customNameStyle` | Animal Policy |
| `includeNotTamedAnimals` | Animal Policy |
| `edgeIndicators` | Edge Indicators |
| `pawnColoredEdgeIndicators` | Edge Indicators |
| `dotRelativeSize` | Marker Appearance |
| `clippedRelativeSize` | Marker Appearance |
| `clippedBorderDistanceFactor` | Marker Appearance |
| `outlineFactor` | Marker Appearance |
| `cameraSettingsKey` / `cameraSettingsMod` | Keyboard Shortcuts |
| `cameraSettingsLoad` | Keyboard Shortcuts |
| `cameraSettingsSave` | Keyboard Shortcuts |
| rules dialog button | top action row |
| restore defaults button | top action row |

## Relationship To FIX.md

UX v2 should be written before implementing the `FIX.md` overhaul, but not every `FIX.md` item should wait for the UI refactor.

Items that should directly inform UX v2:

- Animal marker policy and hidden `AnimalsDifferent` state.
- Restore defaults leaving no selected animal option.
- `Include untamed animals` as a real global gate.
- Edge-indicator color wording and scope.
- `Sound nearness` semantics after the camera-height fix.
- Override notes currently embedded into labels.

Items that can be fixed before or alongside UX v2 because they are primarily logic/rendering:

- Null-safe rule tags.
- `Can Cast` negation behavior.
- Marker/color/material cache signatures.
- Runtime cache lifecycle.
- Edge ordering and edge rendering performance.

Recommended sequencing:

1. Write and agree on this UX v2 spec.
2. Implement low-risk policy/logic fixes from `FIX.md` that do not require the new main UI.
3. Implement the new main settings UI against the corrected setting semantics in English first.
4. Validate that current Camera+ users can still find the familiar settings in `All` and topic views.
5. Add final translations for all existing languages.
6. Run visual/manual UI checks across English and translated languages.

## Non-Goals

- No new search field in the settings dialog.
- No custom resizable settings window.
- No live preview panel.
- No rule editor redesign.
- No separate shortcut dialog redesign. Keyboard shortcut rows move into the main dialog and keep the existing key picking behavior.
- No migration of existing settings names solely for UX.
- No new advanced rule capabilities just to explain main settings.
- No requirement to complete all translations before the English UX is implemented and validated.

## Acceptance Criteria

The UX v2 implementation is done when:

- The main settings dialog uses left navigation, middle scroll content, and right contextual help.
- `All` shows every settings group in the canonical order.
- Each topic filters to the correct group or groups.
- `Keyboard shortcuts` are available as a left-column topic and appear in `All`; `Rules` and `Restore defaults` remain visible and usable outside filtered content.
- Existing users can find all current main-dialog settings from the `All` view without learning new feature names.
- Long labels in existing non-English languages wrap without overlapping controls.
- Override notes are displayed as secondary text, not label suffixes.
- Every meaningful animal marker state is visible in the UI.
- Restore defaults leaves every radio group with a visible selected value.
- Help text updates on hover/focus and falls back to topic help.
- Existing settings still read/write the same fields.
- The rule editor still opens as before, and the inline shortcut key picker still works.

## Manual Test Plan

Run these after implementing UX v2:

1. Open Camera+ settings from the main menu.
2. Open Camera+ settings in a loaded save.
3. Switch through all topics and confirm the middle column filters correctly.
4. Select `All` and confirm every group appears in the canonical order.
5. Hover each group and at least one control per group; confirm the help column updates.
6. Move the mouse over empty space; confirm help returns to the selected topic.
7. Change every setting once and confirm the value persists after closing and reopening settings.
8. Click `Restore defaults` and confirm radio groups visibly select defaults.
9. Select the `Keyboard` topic and confirm shortcut rows are visible and aligned.
10. Open `Rules` from the main menu and from a loaded save.
11. In English, confirm familiar settings are still easy to locate from the `All` view and from their topics.
12. After translations are added, test German, French, Russian, Japanese, Chinese Simplified, Chinese Traditional, Spanish, and Spanish Latin for label wrapping.
13. Verify that disabled dependent settings are still readable and explain why they are disabled.
14. Check the RimWorld log for UI exceptions.

## Build And Verification

After implementation:

```sh
dotnet build Source/CameraPlus.csproj -c Release
```

For documentation-only edits to this file, no build is required.
