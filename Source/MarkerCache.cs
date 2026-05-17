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
		const float defaultSilhouetteCutoff = 0.5f;

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
			SetMarkerTexture(material, texture, canMutateTexture);
			material.SetFloat("_OutlineFactor", outlineFactor);
			material.renderQueue = (int)RenderQueue.Overlay;
			return material;
		}

		static void SetMarkerTexture(Material material, Texture texture, bool canMutateTexture = false)
		{
			if (canMutateTexture && texture != null)
				texture.wrapMode = TextureWrapMode.Clamp;
			material.SetTexture("_MainTex", texture);
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
