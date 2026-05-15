# CameraPlus Agent Guide

CameraPlus is a C# RimWorld mod that changes camera zoom/panning and replaces some vanilla pawn-label rendering with configurable map and edge markers. It is a Harmony-heavy runtime mod, so most behavior is driven by patches rather than by new RimWorld defs.

Start here before making non-trivial changes:

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the current subsystem map and runtime data flow.
- [docs/HARMONY_PATCHES.md](docs/HARMONY_PATCHES.md) for every Harmony patch target, why it exists, and where it is risky.
- [docs/BUILD_AND_DEPENDENCIES.md](docs/BUILD_AND_DEPENDENCIES.md) for the build command, generated outputs, and dependency expectations.
- [docs/PERFORMANCE_REVIEW_BASELINE.md](docs/PERFORMANCE_REVIEW_BASELINE.md) for the known hot paths and suggested profiling focus for the upcoming performance review.

## Working Rules

- Build from the repository root with `dotnet build Source/CameraPlus.csproj -c Release` after C# or dependency changes.
- The release build writes the tracked mod assembly at `1.6/Assemblies/CameraPlus.dll`. If `RIMWORLD_MOD_DIR` is set, the build also copies the mod into that RimWorld Mods directory and creates a zip there.
- Do not remove older version folders (`1.1` through `1.6`) unless the release packaging strategy is changed deliberately. `LoadFolders.xml` still points RimWorld at the per-version folders.
- Keep Harmony patch changes narrow. A single patch can affect camera movement, all pawn rendering, all pawn labels, or other popular mods.
- Treat `Source/Main.cs`, `Source/DotTools.cs`, `Source/DotDrawer.cs`, `Source/MarkerCache.cs`, `Source/Caches.cs`, and `Source/FastUI.cs` as performance-sensitive. These run during camera updates, GUI frames, dynamic drawing, or per-pawn rendering.
- For UI or visible in-game validation on macOS, prefer the local `regionshot` workflow when screenshots or app/window inspection are needed.

## Current Dependency Note

`Krafs.Rimworld.Ref` is intentionally pinned to the newest NuGet package observed during this prep pass, `1.6.4817-beta`, so the code compiles against the newest known RimWorld 1.6 API surface before the larger review.
