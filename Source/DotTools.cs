using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	class DotTools
	{
		[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
		[HarmonyPatch(new Type[] { typeof(Vector3), typeof(Rot4?), typeof(bool) })]
		static class PawnRenderer_RenderPawnAt_Patch
		{
			[HarmonyPriority(10000)]
			public static bool Prefix(Pawn ___pawn)
			{
				if (skipCustomRendering)
					return true;

				if (___pawn.Dead)
					return FastUI.CurUICellSize > Settings.hideDeadPawnsBelow;

				return ShouldShowMarker(___pawn) == false;
			}
		}

		[HarmonyPatch]
		static class VehicleRenderer_RenderPawnAt_Patch
		{
			public static bool Prepare() => TargetMethod() != null;
			public static MethodBase TargetMethod() => AccessTools.Method("Vehicles.VehicleRenderer:RenderPawnAt");

			[HarmonyPriority(10000)]
			public static bool Prefix(Pawn ___vehicle)
			{
				if (skipCustomRendering)
					return true;

				if (___vehicle.Dead)
					return FastUI.CurUICellSize > Settings.hideDeadPawnsBelow;

				return ShouldShowMarker(___vehicle) == false;
			}
		}

		[HarmonyPatch(typeof(SelectionDrawer), nameof(SelectionDrawer.DrawSelectionBracketFor))]
		static class SelectionDrawer_DrawSelectionBracketFor_Patch
		{
			[HarmonyPriority(10000)]
			public static bool Prefix(object obj)
			{
				if (obj is not Pawn pawn)
					return true;
				return ShouldShowMarker(pawn) == false;
			}
		}

		[HarmonyPatch(typeof(PawnUIOverlay), nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
		static class PawnUIOverlay_DrawPawnGUIOverlay_Patch
		{
			[HarmonyPriority(10000)]
			public static bool Prefix(Pawn ___pawn)
			{
				if (skipCustomRendering)
					return true;

				if (___pawn.Dead)
					return FastUI.CurUICellSize > Settings.hideDeadPawnsBelow;

				if (GetMarkerColors(___pawn, out _, out _) == false)
					return true;

				return ShouldShowMarker(___pawn) == false;
			}
		}

		[HarmonyPatch(typeof(SilhouetteUtility), nameof(SilhouetteUtility.ShouldDrawSilhouette))]
		static class SilhouetteUtility_ShouldDrawSilhouette_Patch
		{
			static bool Prefix(Thing thing, ref bool __result)
			{
				if (thing is Pawn pawn && ShouldShowMarker(pawn))
				{
					__result = false;
					return false;
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(GenMapUI), nameof(GenMapUI.DrawPawnLabel))]
		[HarmonyPatch(new Type[] { typeof(Pawn), typeof(Vector2), typeof(float), typeof(float), typeof(Dictionary<string, string>), typeof(GameFont), typeof(bool), typeof(bool) })]
		static class GenMapUI_DrawPawnLabel_Patch
		{
			[HarmonyPriority(10000)]
			public static bool Prefix(Pawn pawn, float truncateToWidth)
			{
				if (skipCustomRendering)
					return true;

				if (truncateToWidth != 9999f)
					return true;

				return Tools.ShouldShowLabel(pawn);
			}
		}

		//

		public static bool ShouldShowMarker(Pawn pawn)
		{
			if (Tools.IsHiddenFromPlayer(pawn))
				return false;

			var dotConfig = Caches.dotConfigCache.Get(pawn);
			if (dotConfig != null)
			{
				if (dotConfig.mode == DotStyle.VanillaDefault)
					return false;

				var dotSize = dotConfig.showBelowPixels;
				if (FastUI.CurUICellSize > (dotSize == -1 ? Settings.dotSize : dotSize))
					return false;

				if (dotConfig.mouseReveals && Tools.MouseDistanceSquared(pawn.DrawPos, true) <= 2.25f) // TODO
					return false;

				return dotConfig.useInside;
			}

			if (FastUI.CurUICellSize > Settings.dotSize)
				return false;

			if (Settings.dotStyle == DotStyle.VanillaDefault)
				return false;

			if (Settings.customNameStyle == LabelStyle.HideAnimals && pawn.RaceProps.Animal)
				return false;

			if (Settings.mouseOverShowsLabels && Tools.MouseDistanceSquared(pawn.DrawPos, true) <= 2.25f) // TODO
				return false;

			var tamedAnimal = pawn.RaceProps.Animal && pawn.Name != null;
			return Settings.includeNotTamedAnimals || pawn.RaceProps.Animal == false || tamedAnimal;
		}

		// returning true will prefer markers over labels
		public static bool GetMarkerColors(Pawn pawn, out Color innerColor, out Color outerColor)
		{
			var selected = Find.Selector.IsSelected(pawn) ? 1 : 0;

			var dotConfig = Caches.dotConfigCache.Get(pawn);
			if (dotConfig != null)
			{
				innerColor = selected == 1 ? dotConfig.fillSelectedColor : dotConfig.fillColor;
				outerColor = selected == 1 ? dotConfig.lineSelectedColor : dotConfig.lineColor;
				return true;
			}

			var cameraDelegate = Caches.GetCachedCameraDelegate(pawn);
			if (cameraDelegate.GetCameraColors != null)
			{
				var colors = cameraDelegate.GetCameraColors(pawn);
				if (colors == null || colors.Length != 2)
				{
					innerColor = default;
					outerColor = default;
					return false;
				}
				innerColor = Settings.customInnerColors[selected].color ?? colors[0];
				outerColor = Settings.customOuterColors[selected].color ?? colors[1];
				return true;
			}

			var isAnimal = pawn.RaceProps.Animal && pawn.Name != null;
			var hideAnimalMarkers = Settings.customNameStyle == LabelStyle.HideAnimals;
			if (isAnimal && hideAnimalMarkers)
			{
				innerColor = default;
				outerColor = default;
				return false;
			}

			if (isAnimal || pawn.Faction != Faction.OfPlayer)
			{
				innerColor = Settings.defaultInnerColors[selected].color ?? Tools.GetMainColor(pawn);
				outerColor = pawn.Faction == Faction.OfPlayer ? Settings.playerNormalOuterColors[selected].Value : Settings.defaultOuterColors[selected].color ?? PawnNameColorUtility.PawnNameColorOf(pawn);
				return true;
			}

			if (pawn.IsColonistPlayerControlled == false)
			{
				outerColor = Settings.playerNormalOuterColors[selected].Value;
				innerColor = Settings.playerNormalInnerColors[selected].Value;
			}
			else if (pawn.IsPlayerControlled == false)
			{
				outerColor = Settings.playerMentalOuterColors[selected].Value;
				innerColor = Settings.playerMentalInnerColors[selected].Value;
			}
			else if (pawn.Downed)
			{
				outerColor = Settings.playerDownedOuterColors[selected].Value;
				innerColor = Settings.playerDownedInnerColors[selected].Value;
			}
			else if (pawn.Drafted)
			{
				outerColor = Settings.playerDraftedOuterColors[selected].Value;
				innerColor = Settings.playerDraftedInnerColors[selected].Value;
			}
			else
			{
				outerColor = Settings.playerNormalOuterColors[selected].Value;
				innerColor = Settings.playerNormalInnerColors[selected].Value;
			}

			return true;
		}

		public static bool GetMarkerTextures(Pawn pawn, out Texture2D innerTexture, out Texture2D outerTexture)
		{
			var cameraDelegate = Caches.GetCachedCameraDelegate(pawn);
			if (cameraDelegate.GetCameraMarkers != null)
			{
				var textures = cameraDelegate.GetCameraMarkers(pawn);
				if (textures == null || textures.Length != 2)
				{
					innerTexture = default;
					outerTexture = default;
					return false;
				}
				innerTexture = textures[0];
				outerTexture = textures[1];
				return true;
			}

			Tools.DefaultMarkerTextures(pawn, out innerTexture, out outerTexture);
			return true;
		}
	}
}