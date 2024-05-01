using System.Collections.Generic;
using RimWorld;
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
				silhouette.SetColor("_FillColor", innerColor);
				silhouette.SetColor("_OutlineColor", outerColor);
				silhouette.SetFloat("_OutlineFactor", 0.15f);
				silhouette.renderQueue = (int)RenderQueue.Overlay;

				Material dot = null;
				if (Tools.GetMarkerTextures(pawn, out var dotTexture, out _))
				{
					dot = MaterialAllocator.Create(Assets.BorderedShader);
					dot.SetTexture("_MainTex", dotTexture);
					dot.SetColor("_FillColor", innerColor);
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

		// copied from RenderPawnAt(Vector3 drawLoc, Rot4? rotOverride, bool neverAimWeapon)
		static void UpdateSilhouetteCache(Pawn pawn)
		{
			var renderer = pawn.Drawer.renderer;
			var graphic = pawn.RaceProps.Humanlike
				? pawn.ageTracker.CurLifeStage.silhouetteGraphicData.Graphic
				: (pawn.ageTracker.CurKindLifeStage.silhouetteGraphicData == null
					? renderer.BodyGraphic
					: pawn.ageTracker.CurKindLifeStage.silhouetteGraphicData.Graphic
					);
			var bodyPos = renderer.GetBodyPos(pawn.DrawPos, PawnPosture.Standing, out _);
			renderer.SetSilhouetteData(graphic, bodyPos);
		}

		static Texture GetTexture(Pawn pawn)
		{
			UpdateSilhouetteCache(pawn);
			if (pawn.Drawer.renderer.SilhouetteGraphic != null)
			{
				var (_, material) = SilhouetteUtility.GetCachedSilhouetteData(pawn);
				return material.mainTexture;
			}

			Tools.DefaultMarkerTextures(pawn, out var inner, out _);
			return inner;
		}
	}
}