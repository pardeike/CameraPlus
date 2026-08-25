# Camera+ Performance Tests

Camera+ can draw hundreds of map and edge markers at once when the camera is
zoomed far out. This report records the deliberately extreme test used to find
and reduce that cost in Camera+ 3.4.6.

The measurements were collected in a live RimWorld game through
[RimBridgeServer](https://github.com/pardeike/RimBridgeServer), with
[Dubs Performance Analyzer](https://steamcommunity.com/sharedfiles/filedetails/?id=2038874626)
measuring the relevant update and rendering methods.

## Results at a Glance

- The test map contained **962 spawned pawns** with extensive map and edge
  marker rendering.
- Camera+'s measured marker-rendering time fell from **5.30 ms to 2.32 ms per
  frame**, a **56% reduction**.
- Six balanced comparison runs covered every RimWorld speed from 1× through
  4×, with 48 measured samples in total.
- At 2× through 4× speed, the Camera+ and vanilla-rendering results stayed
  within the simulation's normal run-to-run variation.
- At 1× speed, the marker-heavy Camera+ scene consistently cost about **4 ticks
  per second** compared with vanilla pawn rendering.

This is an intentionally excessive stress case, not a typical colony. Its
purpose is to make small per-pawn rendering costs visible quickly.

## Simulation Speed

The table shows the mean of six runs. Each Camera+ sample was paired with the
same saved game using vanilla pawn rendering.

| RimWorld speed | Camera+ | Vanilla pawn rendering | Difference |
| --- | ---: | ---: | ---: |
| 1× Normal | 54.7 TPS | 58.6 TPS | -3.9 TPS (-6.7%) |
| 2× Fast | 114.6 TPS | 115.6 TPS | -1.0 TPS (-0.9%) |
| 3× Superfast | 138.1 TPS | 140.0 TPS | -2.0 TPS (-1.4%) |
| 4× Ultrafast | 149.0 TPS | 147.8 TPS | +1.2 TPS (+0.8%) |

TPS means game ticks per second. Higher is better, up to the speed's target.

Only the 1× difference was consistent across all runs. The paired results at
2× and 4× crossed zero, while 3× varied especially widely as RimWorld ran as
fast as the loaded simulation allowed. The small averages at those speeds
should therefore be read as **no reliably measurable difference**, not as a
claim that Camera+ makes RimWorld faster.

## Marker-Rendering Cost

Dubs Performance Analyzer measured `CameraPlus.DotDrawer.DrawDots`, the method
that scans the current map and draws Camera+ map and edge markers.

| RimWorld speed | Before optimization | Camera+ 3.4.6 | Reduction |
| --- | ---: | ---: | ---: |
| 1× Normal | 5.61 ms/frame | 2.24 ms/frame | 60% |
| 2× Fast | 5.31 ms/frame | 2.30 ms/frame | 57% |
| 3× Superfast | 5.16 ms/frame | 2.40 ms/frame | 54% |
| 4× Ultrafast | 5.14 ms/frame | 2.36 ms/frame | 54% |
| **Average** | **5.30 ms/frame** | **2.32 ms/frame** | **56%** |

The original values came from the pre-optimization comparison matrix. The
final values are the means of the six balanced runs. The directly measured
rendering time remained much more stable than total TPS, making it the clearest
evidence of the optimization itself.

The production changes behind the result are deliberately conservative:

- Prepared pawn markers are reused until RimWorld reports that the pawn's
  graphics changed.
- Stable pawn colors are cached through the same graphics lifecycle.
- Unchanged edge-marker colors are not repeatedly sent to the same material.
- Mods that provide dynamic Camera+ marker textures still receive per-frame
  texture validation.

No experimental renderer-phase shortcut was enabled in the tested production
build.

## Test Method

The test used:

- Camera+ `3.4.6.0`, release assembly SHA-256
  `726c8f9c1bc08bce1f9aa9f2bf519839a5195bae8ad0a4bc6e4d925efe0c618c`.
- RimWorld 1.6 on macOS 27.0, using a 16-core Apple M4 Max Mac Studio with
  64 GB memory.
- Harmony, RimBridgeServer, Dubs Performance Analyzer, Camera+, Core, Royalty,
  Ideology, Biotech, Anomaly, and Odyssey.
- The `CameraPlusPerf_962Pawns_EdgeDots` save, with 962 spawned pawns and a
  fixed far-zoom camera position chosen to exercise both map and edge markers.
- A deterministic fluctuating CPU delay of 0.5-2.5 ms per game tick, moving
  from light to heavy and back over 300 ticks. This represents unrelated work
  from a busy late-game simulation and other mods without depending on one
  specific mod pack.

For every sample, the benchmark:

1. Reloaded the same save and paused at a visually ready map.
2. Selected either Camera+ marker rendering or vanilla pawn rendering.
3. Warmed up for one second at the requested game speed.
4. Reset Dubs Performance Analyzer and restarted the synthetic load wave.
5. Measured four seconds of real play time.
6. Recorded advanced ticks, elapsed time, and the Dubs profiler snapshot.

All four speeds were tested in each run. Three runs used one A/B order and
three used the reverse order, so each rendering mode ran first equally often at
every speed. This limits cache warmth, heat, and scheduling drift from favoring
one side.

The benchmark implementation is available in
[`CameraPlusPerformanceBridgeTools.cs`](../Source/BridgeTools/CameraPlusPerformanceBridgeTools.cs).
It is a development companion tool and is not included in the public Camera+
mod payload.

## What “Vanilla” Means Here

The comparison enables Camera+'s public custom-rendering bypass. Pawn bodies,
labels, and silhouettes return to RimWorld's vanilla rendering while Camera+
remains loaded in the same process.

That controlled switch isolates the cost of Camera+'s marker-rendering feature
without changing the save, mod list, or running game between paired samples.
It is **not** a benchmark with the entire Camera+ mod unloaded, so the TPS table
should not be presented as a complete vanilla-versus-mod installation test.

## Correctness Checks

Performance changes were accepted only after live RimBridge checks confirmed
that:

- Cached colors and marker materials are discarded when RimWorld reports pawn
  graphics as dirty.
- The caches repopulate correctly on the next draw.
- East- and west-facing silhouettes select the correct prepared marker.
- Existing camera, label, floating-text, animal, shortcut, and edge behavior
  still passes the Camera+ runtime validation suite.
- The final 962-pawn frame rendered without the earlier marker-edge artifacts.
- The captured RimWorld error log was empty.

The repository and installed Camera+ assembly had identical hashes for the
final run. RimWorld 1.5 and earlier assets and assemblies were not changed.

## Reading the Results Responsibly

This test is useful because it is repeatable, profiler-backed, and much harsher
than an ordinary colony. It does not predict an exact TPS cost for every
computer or mod list. Real results depend on pawn count, zoom level, marker
rules, visible edge markers, other rendering mods, hardware, and whichever
systems are already limiting the simulation.

The strongest conclusion is narrow: **Camera+ 3.4.6 reduced its measured
marker-rendering workload by 56% in the 962-pawn stress scene.** Under the same
heavy simulated load, no additional TPS cost could be separated reliably from
normal variation at 2× through 4× speed; the extreme 1× scene retained a small,
consistent cost.

Maintainer-level measurements and earlier checkpoints remain documented in
[`PERFORMANCE_REVIEW_BASELINE.md`](PERFORMANCE_REVIEW_BASELINE.md) and
[`PERFORMANCE_SCENARIOS.md`](PERFORMANCE_SCENARIOS.md).
