# Build And Dependencies

## Build Command

Run from the repository root:

```sh
dotnet build Source/CameraPlus.csproj -c Release
```

The current project has no automated test project. Treat a clean build as the minimum check, then use in-game validation for camera, marker, label, and compatibility changes.

Local development builds also produce
`artifacts/BridgeTools/CameraPlus/CameraPlus.BridgeTools.dll`. This companion
contains development-only RimBridgeServer validation tools, including live
readback of the animated edge-marker UI insets. Release builds set
`BuildBridgeTools=false`, so the companion is not part of the public mod.

## Outputs

Release builds write:

- `1.6/Assemblies/CameraPlus.dll`

RimWorld 1.6 effects are built separately from `Originals/Effects-1.6` with
Unity `2019.4.30f1`. Run **Assets > Export Camera+ 1.6 Effects** or invoke
`CameraPlus16AssetBundles.BuildAll`; it writes distinct bundles to
`1.6/Resources/{Win64,Linux,MacOS}/effects`.

Root `Resources` and `Originals/Effects` are frozen legacy content for RimWorld
1.5 and earlier. The 1.6 exporter and runtime loader must never write to or load
from those paths.

The project also has a `CopyToRimworld` MSBuild target that runs when `RIMWORLD_MOD_DIR` is set. That target:

- removes private `0Harmony.dll` copies and Finder `.DS_Store` metadata from the copied mod before packaging, so Harmony is supplied only by the declared Harmony dependency and the ZIP contains no local filesystem artifacts.
- copies version folders, metadata, defs, languages, resources, sounds, textures, license, load folders, README, and README image assets into `$(RIMWORLD_MOD_DIR)\CameraPlus`.
- copies the matching development companion into the sibling
  `$(RIMWORLD_MOD_DIR)\..\BridgeTools\CameraPlus` folder.
- zips that copied mod folder as `$(RIMWORLD_MOD_DIR)\CameraPlus.zip`.

On macOS with a Unix shell, MSBuild still prints Windows-style path separators in the target body because the project file uses backslashes.

## Dependency Update Baseline

During this prep pass on 2026-05-15:

- The project was pinned to `Krafs.Rimworld.Ref` `1.6.4518`.
- NuGet reported `1.6.4633` as the latest stable package.
- NuGet's flat-container index listed `1.6.4817-beta` as the latest package overall.
- The project was updated to `Krafs.Rimworld.Ref` `1.6.4817-beta` to compile against the newest known RimWorld 1.6 API surface before the larger review.
- `dotnet build Source/CameraPlus.csproj -c Release` succeeded after the update.

Primary package source:

- `https://www.nuget.org/packages/Krafs.Rimworld.Ref`
- `https://api.nuget.org/v3-flatcontainer/krafs.rimworld.ref/index.json`

## Package References

Current top-level package references in `Source/CameraPlus.csproj`:

- `Brrainz.RimWorld.CrossPromotion` `1.1.2`
- `Krafs.Rimworld.Ref` `1.6.4817-beta`
- `Lib.Harmony` `2.3.6`, with `ExcludeAssets="runtime"`
- `Microsoft.NETCore.Platforms` `7.0.4`
- `Microsoft.NETFramework.ReferenceAssemblies.net472` `1.0.3`
- `TaskPubliciser` `1.0.3`

`Lib.Harmony` is a compile-time package here. The runtime Harmony dependency is provided by the RimWorld Harmony mod declared in `About/About.xml`.

## Publicised RimWorld Reference

`Source/CameraPlus.csproj` contains two custom targets:

- `MyCode`, before `UpdateReferences`, publicises `$(PkgKrafs_Rimworld_Ref)\ref\net472\Assembly-CSharp.dll` into `Assembly-CSharp_publicised.dll`, then adds the publicised assembly as a reference.
- `UpdateReferences`, after `ResolveLockFileReferences`, removes the original non-publicised `Assembly-CSharp.dll` reference.

This is central to the codebase. Many patches and helpers access non-public RimWorld members through the publicised assembly.

## Version Metadata

`Directory.Build.props` owns:

- `ModName`
- `ModFileName`
- `Repository`
- `ModVersion`
- `ProjectGuid`

The `PostBuildAction` target writes `ModVersion` into:

- `About/About.xml` at `//ModMetaData/modVersion`
- `About/Manifest.xml` at `//Manifest/version`

## RimWorld Payload Files

RimWorld loads common content from the root and per-version assemblies through `LoadFolders.xml`.

Important payload directories:

- `About`: RimWorld metadata, manifest, preview, Steam id.
- `Defs`: sound defs for snapback.
- `Languages`: keyed translations for settings and marker tags.
- `Textures`: marker/editor UI textures loaded through `ContentFinder`.
- `Sounds`: snapback audio clips.
- `Resources`: frozen platform-specific Unity bundles for RimWorld 1.5 and earlier.
- `1.6/Resources`: 1.6-only platform-specific Unity bundles.
