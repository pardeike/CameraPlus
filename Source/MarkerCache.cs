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
		const string colorPropertyName = "_Color";

		public static Materials MaterialFor(Pawn pawn)
			=> MaterialFor(pawn, Caches.dotConfigCache.Get(pawn));

		public static Materials MaterialFor(Pawn pawn, DotConfig dotConfig)
		{
			using var measure = PerfMetrics.Measure("MarkerCache.MaterialFor");
			if (cache.TryGetValue(pawn, out var materials))
			{
				PerfMetrics.Count("marker_cache.hits");
				if (materials.Matches(dotConfig))
					return materials;

				PerfMetrics.Count("marker_cache.refreshes");
				Remove(pawn);
			}
			else
				PerfMetrics.Count("marker_cache.misses");

			var outlineFactor = dotConfig?.outlineFactor ?? Settings.outlineFactor;

			var silhouette = MaterialAllocator.Create(Assets.BorderedShader);
			silhouette.name = $"{pawn.ThingID}-silhouette";
			SetMarkerTexture(silhouette, GetTexture(pawn));
			silhouette.SetFloat("_OutlineFactor", outlineFactor);
			silhouette.renderQueue = (int)RenderQueue.Overlay;

			Material custom = null;
			if (dotConfig?.mode == DotStyle.Custom && Assets.customMarkers.TryGetValue(dotConfig.customDotStyle, out var texture))
			{
				custom = MaterialAllocator.Create(Assets.BorderedShader);
				custom.name = $"{pawn.ThingID}-custom";
				SetMarkerTexture(custom, texture, canMutateTexture: true);
				custom.SetFloat("_OutlineFactor", outlineFactor);
				custom.renderQueue = (int)RenderQueue.Overlay;
			}

			Material dot = null;
			Material edgeDot = null;
			if (DotTools.GetMarkerTextures(pawn, out var dotTexture, out _))
			{
				dot = MaterialAllocator.Create(Assets.BorderedShader);
				dot.name = $"{pawn.ThingID}-dot";
				SetMarkerTexture(dot, dotTexture, canMutateTexture: true);
				dot.SetFloat("_OutlineFactor", outlineFactor);
				dot.renderQueue = (int)RenderQueue.Overlay;

				edgeDot = MaterialAllocator.Create(Assets.BorderedShader);
				edgeDot.name = $"{pawn.ThingID}-edge-dot";
				SetMarkerTexture(edgeDot, dotTexture, canMutateTexture: true);
				edgeDot.SetFloat("_OutlineFactor", outlineFactor);
				edgeDot.renderQueue = (int)RenderQueue.Overlay;
			}

			materials = new Materials
			{
				dot = dot,
				edgeDot = edgeDot,
				silhouette = silhouette,
				custom = custom,
				mode = dotConfig?.mode ?? Settings.dotStyle,
				customDotStyle = dotConfig?.customDotStyle,
				outlineFactor = outlineFactor
			};

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
			var dot = cache[pawn].dot;
			if (dot != null)
				MaterialAllocator.Destroy(dot);

			var edgeDot = cache[pawn].edgeDot;
			if (edgeDot != null)
				MaterialAllocator.Destroy(edgeDot);

			var silhouette = cache[pawn].silhouette;
			if (silhouette != null)
				MaterialAllocator.Destroy(silhouette);

			var custom = cache[pawn].custom;
			if (custom != null)
				MaterialAllocator.Destroy(custom);

			cache.Remove(pawn);
		}

		static void SetMarkerTexture(Material material, Texture texture, bool canMutateTexture = false)
		{
			if (canMutateTexture && texture != null)
				texture.wrapMode = TextureWrapMode.Clamp;
			material.SetTexture("_MainTex", texture);
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
			var tint = SourceMaterialTint(sourceMaterial, graphicTint);
			var key = Gen.HashCombineInt(sourceTexture.GetInstanceID(), cutoffByte);
			key = Gen.HashCombineInt(key, ColorHash(tint));
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

		static Color SourceMaterialTint(Material material, Color graphicTint)
		{
			var color = graphicTint;
			if (material != null && material.HasProperty(colorPropertyName))
			{
				var materialTint = material.GetColor(colorPropertyName);
				if (IsWhite(color))
					color = materialTint;
			}
			color.a = 1f;
			return color;
		}

		static bool IsWhite(Color color)
			=> Mathf.Approximately(color.r, 1f)
			&& Mathf.Approximately(color.g, 1f)
			&& Mathf.Approximately(color.b, 1f);

		static int ColorHash(Color color)
		{
			var color32 = (Color32)color;
			var hash = (int)color32.r;
			hash = Gen.HashCombineInt(hash, color32.g);
			hash = Gen.HashCombineInt(hash, color32.b);
			hash = Gen.HashCombineInt(hash, color32.a);
			return hash;
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
