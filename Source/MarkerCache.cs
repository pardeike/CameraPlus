using System.Collections.Generic;
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

		public static Materials MaterialFor(Pawn pawn)
		{
			if (cache.TryGetValue(pawn, out var materials))
			{
				materials.refreshTick--;
				if (materials.refreshTick > 0)
					return materials;
				Remove(pawn);
			}

			var dotConfig = pawn.GetDotConfig();
			var outlineFactor = dotConfig?.outlineFactor ?? Settings.outlineFactor;

			var silhouette = MaterialAllocator.Create(Assets.BorderedShader);
			silhouette.SetTexture("_MainTex", GetTexture(pawn));
			silhouette.SetFloat("_OutlineFactor", outlineFactor);
			silhouette.renderQueue = (int)RenderQueue.Overlay;

			Material custom = null;
			if (dotConfig?.mode == DotStyle.Custom && Assets.customMarkers.TryGetValue(dotConfig.customDotStyle, out var texture))
			{
				custom = MaterialAllocator.Create(Assets.BorderedShader);
				custom.SetTexture("_MainTex", texture);
				custom.SetFloat("_OutlineFactor", outlineFactor);
				custom.renderQueue = (int)RenderQueue.Overlay;
			}

			Material dot = null;
			if (DotTools.GetMarkerTextures(pawn, out var dotTexture, out _))
			{
				dot = MaterialAllocator.Create(Assets.BorderedShader);
				dot.SetTexture("_MainTex", dotTexture);
				dot.SetFloat("_OutlineFactor", outlineFactor);
				dot.renderQueue = (int)RenderQueue.Overlay;
			}

			materials = new Materials { dot = dot, silhouette = silhouette, custom = custom, refreshTick = 300 };

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
		// TODO maybe make a reverse patch?
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