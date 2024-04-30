using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace CameraPlus
{
	public class Materials
	{
		public Material dot;
		public Material silhouette;
		public int refreshTick;
	}

	public class MarkerCache
	{
		static readonly Dictionary<Pawn, Materials> cache = [];

		public static Materials MaterialFor(Pawn pawn)
		{
			if (cache.TryGetValue(pawn, out var materials))
			{
				materials.refreshTick--;
				if (materials.refreshTick > 0)
					return materials;
				Remove(pawn);
			}

			var useMarkers = Tools.GetMarkerColors(pawn, out var innerColor, out var outerColor);
			if (useMarkers)
			{
				var silhouette = MaterialAllocator.Create(Assets.BorderedShader);
				silhouette.SetTexture("_MainTex", GetTexture(pawn));
				silhouette.SetColor("_FillColor", GetColor(pawn, innerColor));
				silhouette.SetColor("_OutlineColor", outerColor);
				silhouette.SetFloat("_OutlineFactor", 0.15f);
				silhouette.renderQueue = (int)RenderQueue.Overlay;

				Material dot = null;
				if (Tools.GetMarkerTextures(pawn, out var dotTexture, out _))
				{
					dot = MaterialAllocator.Create(Assets.BorderedShader);
					dot.SetTexture("_MainTex", dotTexture);
					dot.SetColor("_FillColor", GetColor(pawn, innerColor));
					dot.SetColor("_OutlineColor", outerColor);
					dot.SetFloat("_OutlineFactor", 0.15f);
					dot.renderQueue = (int)RenderQueue.Overlay;
				}

				materials = new Materials { dot = dot, silhouette = silhouette, refreshTick = 60 };
			}

			cache.Add(pawn, materials);
			return materials;
		}

		public static void Remove(Pawn pawn)
		{
			var dot = cache[pawn].dot;
			if (dot != null)
				Object.Destroy(dot);

			var silhouette = cache[pawn].silhouette;
			if (silhouette != null)
				Object.Destroy(silhouette);

			cache.Remove(pawn);
		}

		//

		static Texture GetTexture(Pawn pawn)
		{
			if (pawn.Drawer.renderer.SilhouetteGraphic != null)
			{
				var (_, material) = SilhouetteUtility.GetCachedSilhouetteData(pawn);
				return material.mainTexture;
			}
			else
			{
				Tools.DefaultMarkerTextures(pawn, out var inner, out _);
				return inner;
			}
		}

		static Color GetColor(Pawn pawn, Color markerColor)
		{
			_ = pawn;
			_ = markerColor;
			//return Color.clear;
			//markerColor = Tools.ColorChange(markerColor, saturationRange, brightnessRange);
			return markerColor;
		}
	}
}