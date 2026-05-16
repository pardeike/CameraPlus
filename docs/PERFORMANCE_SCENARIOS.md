# Performance Scenarios

These scenarios are preparation assets for the Camera+ 3.4 performance review. They are saved RimWorld games driven through RimBridgeServer so profiling can start from a known map state instead of rebuilding stress cases by hand.

## Primary Scenario

`CameraPlusPerf_962Pawns_EdgeDots`

- Saved at `/Users/ap/Library/Application Support/RimWorld/Saves/CameraPlusPerf_962Pawns_EdgeDots.rws`.
- Built from a debug game with local mod list `Harmony`, `RimBridgeServer`, `Core`, and local `Camera+`.
- Uses the `CameraPlusPerf=true` build so the perf-only debug action and metrics are available.
- Contains 962 spawned pawns during the verified run.
- Contains a mix of generated animals, named player-faction animals, and 80 player colonists.
- Camera was saved around map cell `(128, 122)` at root size `39.172226`, which keeps many pawns visible while also exercising edge indicators.
- Replacement baseline metrics from `2026-05-15T12:39Z` showed all 962 pawns visible, all 962 in-map markers drawn, and 439-468 edge dots per draw.

This is the main visual-render stress scenario. It is intentionally heavier than normal gameplay because the goal is to expose per-pawn, per-marker, and edge-dot costs quickly.

## Supporting Scenario

`CameraPlusPerf_722Pawns`

- Saved at `/Users/ap/Library/Application Support/RimWorld/Saves/CameraPlusPerf_722Pawns.rws`.
- Earlier center-heavy setup before edge and tame-player additions.
- Useful as a fallback if the edge-dot scenario is too heavy for quick smoke checks.

## Perf Build

Build the mod with metrics and scenario helpers:

```sh
dotnet build Source/CameraPlus.csproj -c Release -p:CameraPlusPerf=true
```

The `CameraPlusPerf` property adds the `CAMERAPLUS_PERF` define. Normal builds do not include the metrics writer, slow-core simulation patches, or perf-only scenario debug actions.

## Metrics Output

The perf build writes CSV metrics to:

```text
~/Library/Application Support/RimWorld/Config/CameraPlusPerf/
```

Summarize the latest run:

```sh
scripts/perf-summary.sh
```

Summarize a specific CSV:

```sh
scripts/perf-summary.sh ~/Library/Application\ Support/RimWorld/Config/CameraPlusPerf/cameraplus-perf-YYYYMMDD-HHMMSS.csv
```

## Slow-Core Simulation

Perf builds also look for:

```text
~/Library/Application Support/RimWorld/Config/CameraPlusPerf/slow-core.txt
```

All values default to `0`, so simulation is disabled unless the file sets delays. Supported keys:

```text
dynamicDrawUs=0
pawnRenderInternalUs=0
pawnGuiOverlayUs=0
pawnLabelUs=0
```

Values are microseconds per call and are clamped to `100000`. The file is reloaded every 120 frames. Use this only in perf builds to model slow computers or expensive vanilla/modded draw paths while keeping normal releases untouched.

## Current Measurement Loop

1. Build with `CameraPlusPerf=true`.
2. Restart RimWorld so the running game loads the perf DLL.
3. Run the RimBridge Lua fixture `scripts/rimbridge/cameraplus_perf_run.lua` with `rimbridge/run_lua_file`.
4. Use parameters `saveName=CameraPlusPerf_962Pawns_EdgeDots`, `durationMs=6000`, and `speed=Ultrafast`.
5. If using GABS directly, call `rimworld.load_game_ready`, verify `rimworld.get_camera_state`, then call `rimworld.play_for`.
6. Stop RimWorld immediately after the fixture returns so paused rendering does not keep appending to the same metrics CSV.
7. Summarize the latest CSV with `scripts/perf-summary.sh`.
7. Compare the key sections before and after code changes:
   - `DynamicDrawManager.DrawDynamicThings.Postfix`
   - `DotDrawer.DrawDots`
   - `DotDrawer.DrawClipped`
   - `DotDrawer.DrawMarker`
   - `DotTools.ShouldShowMarker`
   - `DotTools.GetMarkerColors`
   - `MarkerCache.MaterialFor`

Normal-speed samples are still useful for visual sanity checks, but `Superfast` and `Ultrafast` are better for producing enough frames quickly.

The Lua fixture asserts the saved camera state before playback:

```text
expectedRootSize=39.172226
expectedX=128
expectedZ=122
```

## Visual Regression Scenario

`CameraPlusCloseup`

- Saved at `/Users/ap/Library/Application Support/RimWorld/Saves/CameraPlusCloseup.rws`.
- Contains 5 animals close to the camera on light grey carpet.
- Camera+ silhouettes are configured to remain visible at the close zoom level.
- Use this before trusting graphics-path optimizations because faint texture-padding artifacts are easy to see on the carpet.

Verified visual artifacts from the prep pass:

```text
artifacts/visual-checks/cameraplus-closeup-current.png
artifacts/visual-checks/cameraplus-closeup-final.png
artifacts/visual-checks/cameraplus-closeup-color-cache-confined.png
```

`cameraplus-closeup-current.png` shows the old faint rectangular texture frames around silhouettes. The later captures show the cutout-mask fix plus the edge-scale/color-cache/confinement optimizations without the rectangle artifact or the earlier full-screen color corruption.

## Replacement Baseline

Measured after the user adjusted the scenario camera and saved over `CameraPlusPerf_962Pawns_EdgeDots`.

- CSV: `/Users/ap/Library/Application Support/RimWorld/Config/CameraPlusPerf/cameraplus-perf-20260515-123915.csv`
- Scenario: `CameraPlusPerf_962Pawns_EdgeDots`
- Camera: root size `39.172226`, map position `(128, 122)`, view rect `minX=-13`, `maxX=269`, `minZ=45`, `maxZ=198`
- Playback: `rimworld.play_for`, `speed=Ultrafast`
- Summary mode: first `DotDrawer.DrawDots` row at or beyond 600 draws

600-draw section averages:

```text
DynamicDrawManager.DrawDynamicThings.Postfix  calls=600    avg=3630.557 us  max=20.580 ms
DotDrawer.DrawDots                            calls=600    avg=3629.618 us  max=20.161 ms
DotDrawer.DrawClipped                         calls=273899 avg=1.003 us     max=0.028 ms
DotDrawer.DrawMarker                          calls=578162 avg=0.733 us     max=0.061 ms
DotTools.ShouldShowMarker                     calls=669641 avg=0.624 us     max=4.567 ms
DotTools.GetMarkerColors                      calls=908234 avg=0.601 us     max=8.289 ms
MarkerCache.MaterialFor                       calls=578162 avg=0.192 us     max=1.018 ms
```

600-draw samples and counters:

```text
dotdrawer.all_pawns_spawned  latest=962 max=962 avg=962.000
dotdrawer.visible_pawns      latest=962 max=962 avg=962.000
dotdrawer.marker_draws       latest=962 max=962 avg=962.000
dotdrawer.edge_draws         latest=439 max=468 avg=455.739
dotdrawer.draw_calls         total=601
marker_cache.hits            total=577200
marker_cache.misses          total=962
marker_cache.refreshes       total=1924
quota_cache.DotConfig.requests  total=1577875
quota_cache.DotConfig.refreshes total=27181
```

## Current Optimized Checkpoint

Measured after the first safe optimization pass and visual fixes.

- CSV: `/Users/ap/Library/Application Support/RimWorld/Config/CameraPlusPerf/cameraplus-perf-20260515-171049.csv`
- Scenario: `CameraPlusPerf_962Pawns_EdgeDots`
- Camera: root size `39.172226`, map position `(128, 122)`, view rect `minX=-13`, `maxX=269`, `minZ=45`, `maxZ=198`
- Playback: direct GABS `rimworld.play_for`, `speed=Ultrafast`
- Result: scenario paused itself after advancing 2483 ticks; the 600-draw snapshot was present and comparable.

600-draw section averages:

```text
DynamicDrawManager.DrawDynamicThings.Postfix  calls=600    avg=2364.144 us  max=30.777 ms
DotDrawer.DrawDots                            calls=600    avg=2363.312 us  max=30.431 ms
DotDrawer.DrawClipped                         calls=281268 avg=0.253 us     max=0.030 ms
DotDrawer.DrawMarker                          calls=578162 avg=0.790 us     max=0.059 ms
DotTools.ShouldShowMarker                     calls=667165 avg=0.592 us     max=3.821 ms
DotTools.GetMarkerColors                      calls=905762 avg=0.127 us     max=0.160 ms
MarkerCache.MaterialFor                       calls=578162 avg=0.192 us     max=2.686 ms
```

600-draw samples and counters:

```text
dotdrawer.all_pawns_spawned  latest=962 max=962 avg=962.000
dotdrawer.visible_pawns      latest=962 max=962 avg=962.000
dotdrawer.marker_draws       latest=962 max=962 avg=962.000
dotdrawer.edge_draws         latest=468 max=468 avg=468.000
dotdrawer.draw_calls         total=601
marker_cache.hits            total=577200
marker_cache.misses          total=962
marker_cache.refreshes       total=1924
quota_cache.DotConfig.requests  total=1572927
quota_cache.DotConfig.refreshes total=27152
```

Compared with the replacement baseline, `DotDrawer.DrawDots` improved from `3629.618 us` to `2363.312 us` per draw in the 600-draw snapshot, about a 34.9% reduction. `DotDrawer.DrawClipped` moved from `1.003 us` to `0.253 us` per edge marker, and `DotTools.GetMarkerColors` moved from `0.601 us` to `0.127 us` per call.

## Second-Pass Perf-Gated Checkpoint

Measured after adding the shared marker decision cache, state-based material invalidation, and the `CAMERAPLUS_PERF`-only renderer-phase skip experiment.

- CSV: `/Users/ap/Library/Application Support/RimWorld/Config/CameraPlusPerf/cameraplus-perf-20260515-190955.csv`
- Scenario: `CameraPlusPerf_962Pawns_EdgeDots`
- Camera: root size `39.172226`, map position `(128, 122)`, view rect `minX=-13`, `maxX=269`, `minZ=45`, `maxZ=198`
- Playback: RimBridge Lua fixture `scripts/rimbridge/cameraplus_perf_run.lua`, `speed=Ultrafast`
- Result: the scripted run advanced 2483 ticks, then the scenario paused itself. Use the 600-draw row from the file for comparison because a later closeup screenshot was captured in the same RimWorld process.

600-draw section averages:

```text
DynamicDrawManager.DrawDynamicThings.Postfix  calls=600    avg=2343.504 us  max=38.352 ms
DotDrawer.DrawDots                            calls=600    avg=2342.038 us  max=37.570 ms
DotDrawer.DrawClipped                         calls=266929 avg=0.255 us     max=0.025 ms
DotDrawer.DrawMarker                          calls=578162 avg=0.845 us     max=4.005 ms
DotTools.GetMarkerColors                      calls=578162 avg=0.121 us     max=0.224 ms
MarkerCache.MaterialFor                       calls=578162 avg=0.254 us     max=3.460 ms
```

600-draw samples and counters:

```text
dotdrawer.all_pawns_spawned       latest=962 max=962 avg=962.000
dotdrawer.visible_pawns           latest=962 max=962 avg=962.000
dotdrawer.marker_draws            latest=962 max=962 avg=962.000
dotdrawer.edge_draws              latest=444 max=468 avg=444.141
dotdrawer.draw_calls              total=601
marker_decision.cache_misses      total=578162
marker_decision.cache_hits        total=1362569
renderer_phase.skip.EnsureInitialized total=341700
renderer_phase.skip.ParallelPreDraw   total=341700
renderer_phase.skip.Draw              total=341700
marker_cache.hits                 total=577200
marker_cache.misses               total=962
quota_cache.DotConfig.requests    total=578162
quota_cache.DotConfig.refreshes   total=10582
```

Compared with the replacement baseline, `DotDrawer.DrawDots` improved from `3629.618 us` to `2342.038 us` at the 600-draw snapshot, about a 35.5% reduction. Compared with the first optimized checkpoint, this is roughly flat for `DotDrawer.DrawDots`; the main measured effect is the reduced `DotConfig` request volume and the perf-build-only renderer-phase skip signal.

The same run also captured the closeup visual fixture through RimBridge:

```text
/Users/ap/Library/Application Support/RimWorld/Screenshots/cameraplus-closeup-second-pass.png
```

## High-Edge Sanity Check

The closeup visual save can leave Camera+ settings in a more aggressive state before loading the 962-pawn save. That state produced 948 edge dots per draw and is useful as an extra stress pass, but it should not replace the cold-start baseline above.

Relevant CSVs:

```text
before current safe edge/color/confinement fixes: cameraplus-perf-20260515-143830.csv
after current safe edge/color/confinement fixes:  cameraplus-perf-20260515-154225.csv
```

At the 600-draw snapshot, the high-edge run improved from `4427.361 us` to `3054.064 us` for `DotDrawer.DrawDots` while preserving the closeup visual check.
```
