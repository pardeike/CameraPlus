using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	class DotTools
	{
		static readonly Color[] playerAnimalOuterColors = [Color.black, Color.white];

		[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
		[HarmonyPatch([typeof(Vector3), typeof(Rot4?), typeof(bool)])]
		static class PawnRenderer_RenderPawnAt_Patch
		{
			[HarmonyPriority(10000)]
			public static bool Prefix(Pawn ___pawn)
				=> ShouldRenderPawn(___pawn);
		}

		[HarmonyPatch]
		static class VehicleRenderer_DynamicDrawPhaseAt_Patch
		{
			public static bool Prepare()
			{
				var method = TargetMethod();
				var vehicleField = method == null ? null : AccessTools.Field(method.DeclaringType, "vehicle");
				return vehicleField != null && typeof(Pawn).IsAssignableFrom(vehicleField.FieldType);
			}

			public static MethodBase TargetMethod()
				=> AccessTools.Method("Vehicles.Rendering.VehicleRenderer:DynamicDrawPhaseAt");

			[HarmonyPriority(10000)]
			public static bool Prefix(Pawn ___vehicle, DrawPhase phase)
				=> phase != DrawPhase.Draw || ShouldRenderPawn(___vehicle);
		}

		[HarmonyPatch(typeof(SelectionDrawer), nameof(SelectionDrawer.DrawSelectionBracketFor))]
		static class SelectionDrawer_DrawSelectionBracketFor_Patch
		{
			[HarmonyPriority(10000)]
			public static bool Prefix(object obj, Material overrideMat)
			{
				// A non-null material belongs to another selection system (for example,
				// Multiplayer's remote-player brackets) and has no marker equivalent.
				if (skipCustomRendering || overrideMat != null || obj is not Pawn pawn)
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
				if (skipCustomRendering || ___pawn == null)
					return true;

				if (___pawn.health?.Dead == true)
					return FastUI.CurUICellSize > Settings.hideDeadPawnsBelow;

				var decision = MarkerDecisionCache.Get(___pawn);
				if (decision.hasMarkerColors == false)
					return true;

				return decision.suppressVanilla == false;
			}
		}

		[HarmonyPatch(typeof(SilhouetteUtility), nameof(SilhouetteUtility.ShouldDrawSilhouette))]
		static class SilhouetteUtility_ShouldDrawSilhouette_Patch
		{
			static bool Prefix(Thing thing, ref bool __result)
			{
				if (skipCustomRendering)
					return true;

				if (thing is Pawn pawn)
				{
					var decision = MarkerDecisionCache.Get(pawn);
					if (decision.suppressVanilla)
					{
						__result = false;
						return false;
					}
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(GenMapUI), nameof(GenMapUI.DrawPawnLabel))]
		[HarmonyPatch([typeof(Pawn), typeof(Vector2), typeof(float), typeof(float), typeof(Dictionary<string, string>), typeof(GameFont), typeof(bool), typeof(bool)])]
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

		public static bool ShouldShowMarker(Pawn pawn, DotConfig dotConfig = null)
		{
			using var measure = PerfMetrics.Measure("DotTools.ShouldShowMarker");
			PerfMetrics.Count("should_show_marker.calls");

			return MarkerDecisionCache.Get(pawn, dotConfig).suppressVanilla;
		}

		static bool ShouldRenderPawn(Pawn pawn)
		{
			if (skipCustomRendering || pawn == null)
				return true;

			if (pawn.health?.Dead == true)
				return FastUI.CurUICellSize > Settings.hideDeadPawnsBelow;

			return ShouldShowMarker(pawn) == false;
		}

		// returning true will prefer markers over labels
		public static bool GetMarkerColors(Pawn pawn, out Color innerColor, out Color outerColor)
			=> GetMarkerColors(pawn, Caches.dotConfigCache.Get(pawn), out innerColor, out outerColor);

		public static bool GetMarkerColors(Pawn pawn, DotConfig dotConfig, out Color innerColor, out Color outerColor)
		{
			using var measure = PerfMetrics.Measure("DotTools.GetMarkerColors");
			PerfMetrics.Count("get_marker_colors.calls");

			var animalPolicy = AnimalMarkerPolicy.For(pawn);
			if (pawn == null || animalPolicy.included == false)
			{
				innerColor = default;
				outerColor = default;
				return false;
			}

			var selected = Find.Selector?.IsSelected(pawn) == true ? 1 : 0;

			if (dotConfig != null)
			{
				innerColor = selected == 1 ? dotConfig.fillSelectedColor : dotConfig.fillColor;
				outerColor = selected == 1 ? dotConfig.lineSelectedColor : dotConfig.lineColor;
				return true;
			}

			var cameraDelegate = Caches.GetCachedCameraDelegate(pawn);
			if (cameraDelegate.TryGetCameraColors(pawn, out var colors))
			{
				if (colors?.Length == 2)
				{
					innerColor = colors[0];
					outerColor = colors[1];
					return true;
				}
				if (colors != null)
					cameraDelegate.WarnInvalidResult("GetCameraPlusColors", "exactly two colors or null");
			}

			if (animalPolicy.isAnimal || pawn.Faction != Faction.OfPlayer)
			{
				innerColor = Tools.GetMainColor(pawn);
				outerColor = pawn.Faction == Faction.OfPlayer ? playerAnimalOuterColors[selected] : PawnNameColorUtility.PawnNameColorOf(pawn);
				return true;
			}

			if (pawn.health?.Downed == true)
				GetDefaultColonistColors(selected, Settings.defaultColonistDownedOutline, Settings.defaultColonistDownedFill, Settings.defaultColonistDownedSelectedOutline, Settings.defaultColonistDownedSelectedFill, out innerColor, out outerColor);
			else if (pawn.Drafted)
				GetDefaultColonistColors(selected, Settings.defaultColonistDraftedOutline, Settings.defaultColonistDraftedFill, Settings.defaultColonistDraftedSelectedOutline, Settings.defaultColonistDraftedSelectedFill, out innerColor, out outerColor);
			else if (pawn.health?.Dead == false && pawn.mindState?.mentalStateHandler?.CurStateDef != null)
				GetDefaultColonistColors(selected, Settings.defaultColonistMentalOutline, Settings.defaultColonistMentalFill, Settings.defaultColonistMentalSelectedOutline, Settings.defaultColonistMentalSelectedFill, out innerColor, out outerColor);
			else
				GetDefaultColonistColors(selected, Settings.defaultColonistNormalOutline, Settings.defaultColonistNormalFill, Settings.defaultColonistNormalSelectedOutline, Settings.defaultColonistNormalSelectedFill, out innerColor, out outerColor);

			return true;
		}

		static void GetDefaultColonistColors(int selected, Color outline, Color fill, Color selectedOutline, Color selectedFill, out Color innerColor, out Color outerColor)
		{
			var isSelected = selected == 1;
			outerColor = isSelected ? selectedOutline : outline;
			innerColor = isSelected ? selectedFill : fill;
		}

		public static Color GetEdgeFillColor(Pawn pawn, Color fillColor)
		{
			using var measure = PerfMetrics.Measure("DotTools.GetEdgeFillColor");
			PerfMetrics.Count("get_edge_fill_color.calls");

			var animalPolicy = AnimalMarkerPolicy.For(pawn);
			if (animalPolicy.useAnimalEdgeColor == false || fillColor.a > 0f)
				return fillColor;

			var pawnColor = Tools.GetMainColor(pawn);
			return pawnColor.a > 0f ? pawnColor : fillColor;
		}

		public static bool GetMarkerTextures(Pawn pawn, out Texture2D innerTexture, out Texture2D outerTexture)
			=> GetMarkerTextures(pawn, out innerTexture, out outerTexture, out _);

		public static bool GetMarkerTextures(Pawn pawn, out Texture2D innerTexture, out Texture2D outerTexture, out bool dynamicMarkerTextures)
		{
			using var measure = PerfMetrics.Measure("DotTools.GetMarkerTextures");
			if (pawn == null)
			{
				innerTexture = null;
				outerTexture = null;
				dynamicMarkerTextures = false;
				return false;
			}

			var cameraDelegate = Caches.GetCachedCameraDelegate(pawn);
			dynamicMarkerTextures = cameraDelegate.HasCameraMarkers;
			if (cameraDelegate.TryGetCameraMarkers(pawn, out var textures))
			{
				if (textures?.Length == 2 && textures[0] != null && textures[1] != null)
				{
					innerTexture = textures[0];
					outerTexture = textures[1];
					return true;
				}

				// The published integration contract uses null to request Camera+'s defaults.
				if (textures != null)
					cameraDelegate.WarnInvalidResult("GetCameraPlusMarkers", "two non-null textures or null");
			}

			dynamicMarkerTextures = cameraDelegate.HasCameraMarkers;
			Tools.DefaultMarkerTextures(pawn, out innerTexture, out outerTexture);
			return innerTexture != null && outerTexture != null;
		}
	}
}
