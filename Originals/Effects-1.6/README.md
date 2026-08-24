# CameraPlus 1.6 effects

This Unity project owns only the RimWorld 1.6 marker effects. Its exporter writes
the `effects` bundles to `1.6/Resources/{Win64,Linux,MacOS}`.

The root `Resources` bundles and `Originals/Effects` project are the frozen
legacy assets used by RimWorld 1.5 and earlier. Do not rebuild or modify them
when changing 1.6 rendering.

Open this project with Unity `2019.4.30f1` and run **Assets > Export Camera+ 1.6
Effects**, or invoke `CameraPlus16AssetBundles.BuildAll` in batch mode.
