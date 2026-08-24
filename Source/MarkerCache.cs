using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	public class MarkerCache
	{
		public static readonly Dictionary<Pawn, Materials> cache = [];
		static readonly Dictionary<int, Texture2D> silhouetteTextureCache = [];
		static readonly Dictionary<int, RenderTexture> markerTextureCache = [];
		static readonly Dictionary<int, RenderTexture> outlineTextureCache = [];
		static readonly HashSet<int> failedMarkerTextureKeys = [];
		static readonly HashSet<int> failedOutlineTextureKeys = [];
		static Material outlineGeneratorMaterial;
		const float defaultSilhouetteCutoff = 0.5f;
		const int markerTextureGuardPixels = 2;

		public static Materials MaterialFor(Pawn pawn)
			=> MaterialFor(pawn, Caches.dotConfigCache.Get(pawn));

		public static Materials MaterialFor(Pawn pawn, DotConfig dotConfig, bool needInside = true, bool needEdge = false)
		{
			using var measure = PerfMetrics.Measure("MarkerCache.MaterialFor");
			var inputs = MaterialInputs.For(pawn, dotConfig);
			if (cache.TryGetValue(pawn, out var materials))
			{
				PerfMetrics.Count("marker_cache.hits");
				if (materials.Matches(inputs.signature))
				{
					EnsureMaterials(materials, inputs, needInside, needEdge);
					return materials;
				}

				PerfMetrics.Count("marker_cache.refreshes");
				Remove(pawn);
			}
			else
				PerfMetrics.Count("marker_cache.misses");

			materials = new Materials
			{
				signature = inputs.signature
			};
			EnsureMaterials(materials, inputs, needInside, needEdge);

			cache.Add(pawn, materials);
			return materials;
		}

		public static void Clear()
		{
			var pawns = cache.Keys.ToList();
			foreach (var pawn in pawns)
				Remove(pawn);
			cache.Clear();

			foreach (var texture in silhouetteTextureCache.Values)
				UnityEngine.Object.Destroy(texture);
			silhouetteTextureCache.Clear();

			ReleaseRenderTextures(markerTextureCache.Values);
			markerTextureCache.Clear();
			failedMarkerTextureKeys.Clear();

			ReleaseRenderTextures(outlineTextureCache.Values);
			outlineTextureCache.Clear();
			failedOutlineTextureKeys.Clear();

			if (outlineGeneratorMaterial != null)
			{
				MaterialAllocator.Destroy(outlineGeneratorMaterial);
				outlineGeneratorMaterial = null;
			}
		}

		static void ReleaseRenderTextures(IEnumerable<RenderTexture> textures)
		{
			foreach (var texture in textures)
			{
				texture.Release();
				UnityEngine.Object.Destroy(texture);
			}
		}

		public static void Remove(Pawn pawn)
		{
			if (pawn == null || cache.TryGetValue(pawn, out var materials) == false)
				return;

			var dot = materials.dot;
			if (dot != null)
				MaterialAllocator.Destroy(dot);

			var edgeDot = materials.edgeDot;
			if (edgeDot != null)
				MaterialAllocator.Destroy(edgeDot);

			var silhouette = materials.silhouette;
			if (silhouette != null)
				MaterialAllocator.Destroy(silhouette);

			var custom = materials.custom;
			if (custom != null)
				MaterialAllocator.Destroy(custom);

			cache.Remove(pawn);
		}

		public static void RemoveForMap(Map map)
		{
			if (map == null || cache.Count == 0)
				return;

			var pawns = cache.Keys.ToList();
			foreach (var pawn in pawns)
				if (pawn?.Map == map || map.mapPawns.AllPawnsSpawned.Contains(pawn))
					Remove(pawn);
		}

		public static void PruneInvalid()
		{
			if (cache.Count == 0)
				return;

			var maps = Find.Maps;
			if (maps == null || maps.Count == 0)
			{
				Clear();
				return;
			}

			var livePawns = new HashSet<Pawn>();
			foreach (var map in maps)
				foreach (var pawn in map.mapPawns.AllPawnsSpawned)
					livePawns.Add(pawn);

			var cachedPawns = cache.Keys.ToList();
			foreach (var pawn in cachedPawns)
				if (pawn == null || pawn.Destroyed || pawn.Map == null || livePawns.Contains(pawn) == false)
					Remove(pawn);
		}

		static void EnsureMaterials(Materials materials, MaterialInputs inputs, bool needInside, bool needEdge)
		{
			var mode = inputs.signature.mode;
			var outlineFactor = inputs.signature.outlineFactor;

			if (needInside)
			{
				switch (mode)
				{
					case DotStyle.ClassicDots when materials.dot == null && inputs.dotTexture != null:
						materials.dot = CreateMarkerMaterial(inputs.pawn, "dot", inputs.dotTexture, outlineFactor, canMutateTexture: true);
						break;
					case DotStyle.BetterSilhouettes when materials.silhouette == null:
						materials.silhouette = CreateMarkerMaterial(inputs.pawn, "silhouette", inputs.silhouetteTexture, outlineFactor);
						break;
					case DotStyle.Custom when materials.custom == null && inputs.customTexture != null:
						materials.custom = CreateMarkerMaterial(inputs.pawn, "custom", inputs.customTexture, outlineFactor, canMutateTexture: true);
						break;
				}
			}

			if (needEdge && materials.edgeDot == null && inputs.dotTexture != null)
				materials.edgeDot = CreateMarkerMaterial(inputs.pawn, "edge-dot", inputs.dotTexture, outlineFactor, canMutateTexture: true);
		}

		static Material CreateMarkerMaterial(Pawn pawn, string suffix, Texture texture, float outlineFactor, bool canMutateTexture = false)
		{
			var material = MaterialAllocator.Create(Assets.BorderedShader);
			material.name = $"{pawn.ThingID}-{suffix}";
			SetMarkerTextures(material, texture, outlineFactor, canMutateTexture);
			material.SetFloat("_OutlineFactor", outlineFactor);
			material.renderQueue = (int)RenderQueue.Overlay;
			return material;
		}

		static void SetMarkerTextures(Material material, Texture texture, float outlineFactor, bool canMutateTexture = false)
		{
			if (canMutateTexture && texture != null)
				texture.wrapMode = TextureWrapMode.Clamp;

			var markerTexture = MarkerTextureFor(texture);
			material.SetTexture("_MainTex", markerTexture);
			var markerUVScale = texture == null || markerTexture == null
				? new Vector4(1f, 1f, 0f, 0f)
				: new Vector4((float)texture.width / markerTexture.width, (float)texture.height / markerTexture.height, 0f, 0f);
			material.SetVector("_MainUVScale", markerUVScale);

			var outlineTexture = OutlineTextureFor(texture, outlineFactor);
			material.SetTexture("_OutlineTex", outlineTexture);
			var outlineUVScale = texture == null || outlineTexture == null
				? new Vector4(1f, 1f, 0f, 0f)
				: new Vector4((float)texture.width / outlineTexture.width, (float)texture.height / outlineTexture.height, 0f, 0f);
			material.SetVector("_OutlineUVScale", outlineUVScale);
		}

		static Texture MarkerTextureFor(Texture sourceTexture)
		{
			if (sourceTexture == null)
				return null;

			var key = sourceTexture.GetInstanceID();
			if (markerTextureCache.TryGetValue(key, out var cachedTexture))
				return cachedTexture;
			if (failedMarkerTextureKeys.Contains(key))
				return sourceTexture;

			try
			{
				cachedTexture = CreateMarkerTexture(sourceTexture);
				markerTextureCache[key] = cachedTexture;
				PerfMetrics.Count("marker_texture.cache_misses");
				return cachedTexture;
			}
			catch (Exception exception)
			{
				failedMarkerTextureKeys.Add(key);
				Log.Warning($"CameraPlus failed to prepare marker texture '{sourceTexture.name}': {exception}");
				return sourceTexture;
			}
		}

		// Copy once on the GPU into transparent guard pixels without mipmaps. Projected map
		// quads can otherwise pull opaque alpha from the source texture's generated mip levels.
		static RenderTexture CreateMarkerTexture(Texture sourceTexture)
		{
			var sourceWidth = sourceTexture.width;
			var sourceHeight = sourceTexture.height;
			if (sourceWidth <= 0 || sourceHeight <= 0)
				throw new InvalidOperationException($"Invalid marker texture size {sourceWidth}x{sourceHeight}.");
			if (Assets.OutlineMaskShader == null)
				throw new InvalidOperationException("The OutlineMask shader is not loaded.");

			var width = sourceWidth + 2 * markerTextureGuardPixels;
			var height = sourceHeight + 2 * markerTextureGuardPixels;
			outlineGeneratorMaterial ??= MaterialAllocator.Create(Assets.OutlineMaskShader);
			outlineGeneratorMaterial.SetVector("_SourceUVScale", new Vector4((float)width / sourceWidth, (float)height / sourceHeight, 0f, 0f));
			var markerTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
			{
				name = $"{sourceTexture.name}-CameraPlusMarker",
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Bilinear,
				anisoLevel = 0,
				useMipMap = false,
				autoGenerateMips = false
			};

			try
			{
				markerTexture.Create();
				Graphics.Blit(sourceTexture, markerTexture, outlineGeneratorMaterial, 3);
				return markerTexture;
			}
			catch
			{
				markerTexture.Release();
				UnityEngine.Object.Destroy(markerTexture);
				throw;
			}
		}

		static Texture OutlineTextureFor(Texture sourceTexture, float outlineFactor)
		{
			if (sourceTexture == null || outlineFactor <= 0f)
				return sourceTexture;

			var factorKey = Mathf.RoundToInt(outlineFactor * 10_000f);
			var key = Gen.HashCombineInt(sourceTexture.GetInstanceID(), factorKey);
			if (outlineTextureCache.TryGetValue(key, out var cachedTexture))
				return cachedTexture;
			if (failedOutlineTextureKeys.Contains(key))
				return sourceTexture;

			try
			{
				cachedTexture = CreateOutlineTexture(sourceTexture, outlineFactor);
				outlineTextureCache[key] = cachedTexture;
				PerfMetrics.Count("outline_texture.cache_misses");
				return cachedTexture;
			}
			catch (Exception exception)
			{
				failedOutlineTextureKeys.Add(key);
				Log.Warning($"CameraPlus failed to prepare marker outline texture '{sourceTexture.name}': {exception}");
				return sourceTexture;
			}
		}

		// Generate the radial mask once on the GPU; normal marker draws only sample the cached result.
		static RenderTexture CreateOutlineTexture(Texture sourceTexture, float outlineFactor)
		{
			var sourceWidth = sourceTexture.width;
			var sourceHeight = sourceTexture.height;
			if (sourceWidth <= 0 || sourceHeight <= 0)
				throw new InvalidOperationException($"Invalid marker texture size {sourceWidth}x{sourceHeight}.");
			if (Assets.OutlineMaskShader == null)
				throw new InvalidOperationException("The OutlineMask shader is not loaded.");

			var paddedScale = 1f + 2f * outlineFactor;
			var width = Mathf.CeilToInt(sourceWidth * paddedScale);
			var height = Mathf.CeilToInt(sourceHeight * paddedScale);
			var sourceUVScale = new Vector4((float)width / sourceWidth, (float)height / sourceHeight, 0f, 0f);
			outlineGeneratorMaterial ??= MaterialAllocator.Create(Assets.OutlineMaskShader);
			outlineGeneratorMaterial.SetVector("_SourceUVScale", sourceUVScale);
			var nearestA = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
			var nearestB = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
			var previous = RenderTexture.active;
			RenderTexture outlineTexture = null;
			try
			{
				nearestA.filterMode = FilterMode.Point;
				nearestA.wrapMode = TextureWrapMode.Clamp;
				nearestB.filterMode = FilterMode.Point;
				nearestB.wrapMode = TextureWrapMode.Clamp;

				Graphics.Blit(sourceTexture, nearestA, outlineGeneratorMaterial, 0);
				var current = nearestA;
				var next = nearestB;
				for (var step = Mathf.NextPowerOfTwo(Mathf.Max(width, height)) / 2; step >= 1; step /= 2)
				{
					outlineGeneratorMaterial.SetFloat("_Step", step);
					Graphics.Blit(current, next, outlineGeneratorMaterial, 1);
					(current, next) = (next, current);
				}

				outlineTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
				{
					name = $"{sourceTexture.name}-CameraPlusOutline",
					wrapMode = TextureWrapMode.Clamp,
					filterMode = sourceTexture.filterMode,
					anisoLevel = sourceTexture.anisoLevel,
					useMipMap = false,
					autoGenerateMips = false
				};
				outlineTexture.Create();
				outlineGeneratorMaterial.SetFloat("_OutlineFactor", outlineFactor);
				Graphics.Blit(current, outlineTexture, outlineGeneratorMaterial, 2);
				return outlineTexture;
			}
			catch
			{
				if (outlineTexture != null)
				{
					outlineTexture.Release();
					UnityEngine.Object.Destroy(outlineTexture);
				}
				throw;
			}
			finally
			{
				RenderTexture.active = previous;
				RenderTexture.ReleaseTemporary(nearestA);
				RenderTexture.ReleaseTemporary(nearestB);
			}
		}

		readonly struct MaterialInputs
		{
			public readonly Pawn pawn;
			public readonly MaterialSignature signature;
			public readonly Texture dotTexture;
			public readonly Texture silhouetteTexture;
			public readonly Texture customTexture;

			MaterialInputs(Pawn pawn, MaterialSignature signature, Texture dotTexture, Texture silhouetteTexture, Texture customTexture)
			{
				this.pawn = pawn;
				this.signature = signature;
				this.dotTexture = dotTexture;
				this.silhouetteTexture = silhouetteTexture;
				this.customTexture = customTexture;
			}

			public static MaterialInputs For(Pawn pawn, DotConfig dotConfig)
			{
				var mode = dotConfig?.mode ?? Settings.dotStyle;
				var outlineFactor = dotConfig?.outlineFactor ?? Settings.outlineFactor;

				Texture dotTexture = null;
				if (DotTools.GetMarkerTextures(pawn, out var markerTexture, out _))
					dotTexture = markerTexture;

				Texture silhouetteTexture = null;
				if (mode == DotStyle.BetterSilhouettes)
					silhouetteTexture = GetTexture(pawn);

				Texture customTexture = null;
				if (mode == DotStyle.Custom && Assets.customMarkers.TryGetValue(dotConfig?.customDotStyle, out var texture))
					customTexture = texture;

				var signature = new MaterialSignature(
					mode,
					dotConfig?.customDotStyle,
					outlineFactor,
					TextureId(dotTexture),
					TextureId(silhouetteTexture),
					TextureId(customTexture));
				return new MaterialInputs(pawn, signature, dotTexture, silhouetteTexture, customTexture);
			}

			static int TextureId(Texture texture)
				=> texture?.GetInstanceID() ?? 0;
		}

		static Graphic GetSilhouetteGraphic(Pawn pawn)
		{
			var renderer = pawn.Drawer.renderer;
			renderer.renderTree.EnsureInitialized(PawnRenderFlags.DrawNow);
			return pawn.RaceProps.Humanlike
				? pawn.ageTracker.CurLifeStage.silhouetteGraphicData.Graphic
				: (pawn.ageTracker.CurKindLifeStage.silhouetteGraphicData == null
					? renderer.BodyGraphic
					: pawn.ageTracker.CurKindLifeStage.silhouetteGraphicData.Graphic
					);
		}

		// copied from RenderPawnAt(Vector3 drawLoc, Rot4? rotOverride, bool neverAimWeapon)
		// TODO maybe make a reverse patch?
		static void UpdateSilhouetteCache(Pawn pawn, Graphic graphic)
		{
			var renderer = pawn.Drawer.renderer;
			var bodyPos = renderer.GetBodyPos(pawn.DrawPos, PawnPosture.Standing, out _);
			renderer.SetSilhouetteData(graphic, bodyPos);
		}

		static Texture GetTexture(Pawn pawn)
		{
			var graphic = GetSilhouetteGraphic(pawn);
			if (graphic == null)
			{
				Tools.DefaultMarkerTextures(pawn, out var fallbackInner, out _);
				return fallbackInner;
			}

			UpdateSilhouetteCache(pawn, graphic);
			if (pawn.Drawer.renderer.SilhouetteGraphic != null)
			{
				var (_, material) = SilhouetteUtility.GetCachedSilhouetteData(pawn);
				return PreparedSilhouetteTexture(material, graphic.color);
			}

			Tools.DefaultMarkerTextures(pawn, out var inner, out _);
			return inner;
		}

		static Texture PreparedSilhouetteTexture(Material sourceMaterial, Color graphicTint)
		{
			var sourceTexture = sourceMaterial?.mainTexture;
			if (sourceTexture == null)
				return null;

			var cutoff = SilhouetteAlphaCutoff(sourceMaterial);
			var cutoffByte = Mathf.Clamp(Mathf.RoundToInt(cutoff * byte.MaxValue), 1, byte.MaxValue);
			var tint = Tools.EffectiveMaterialTint(sourceMaterial, graphicTint);
			var key = Gen.HashCombineInt(sourceTexture.GetInstanceID(), cutoffByte);
			key = Gen.HashCombineInt(key, Tools.ColorHash(tint));
			if (silhouetteTextureCache.TryGetValue(key, out var cachedTexture))
				return cachedTexture;

			try
			{
				cachedTexture = CreateCutoutTexture(sourceTexture, cutoffByte, tint);
				silhouetteTextureCache[key] = cachedTexture;
				return cachedTexture;
			}
			catch (Exception exception)
			{
				Log.Warning($"CameraPlus failed to prepare silhouette texture '{sourceTexture.name}': {exception}");
				return sourceTexture;
			}
		}

		static float SilhouetteAlphaCutoff(Material material)
		{
			if (material != null && material.HasProperty("_Cutoff"))
				return Mathf.Clamp01(material.GetFloat("_Cutoff"));
			return defaultSilhouetteCutoff;
		}

		static Texture2D CreateCutoutTexture(Texture sourceTexture, int cutoffByte, Color tint)
		{
			var previous = RenderTexture.active;
			var tempRT = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			try
			{
				Graphics.Blit(sourceTexture, tempRT);
				RenderTexture.active = tempRT;

				var texture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.ARGB32, false);
				texture.ReadPixels(new Rect(0, 0, sourceTexture.width, sourceTexture.height), 0, 0);

				var pixels = texture.GetPixels32();
				for (var i = 0; i < pixels.Length; i++)
				{
					var color = pixels[i];
					if (color.a < cutoffByte)
						color = new Color32(0, 0, 0, 0);
					else
					{
						color.r = (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * tint.r), 0, 255);
						color.g = (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * tint.g), 0, 255);
						color.b = (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * tint.b), 0, 255);
						color.a = byte.MaxValue;
					}
					pixels[i] = color;
				}

				texture.SetPixels32(pixels);
				texture.name = $"{sourceTexture.name}-CameraPlusCutout";
				texture.wrapMode = TextureWrapMode.Clamp;
				texture.filterMode = sourceTexture.filterMode;
				texture.anisoLevel = sourceTexture.anisoLevel;
				texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
				return texture;
			}
			finally
			{
				RenderTexture.active = previous;
				RenderTexture.ReleaseTemporary(tempRT);
			}
		}
	}
}
