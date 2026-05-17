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
				if (skipCustomRendering || obj is not Pawn pawn)
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

		// returning true will prefer markers over labels
		public static bool GetMarkerColors(Pawn pawn, out Color innerColor, out Color outerColor)
			=> GetMarkerColors(pawn, Caches.dotConfigCache.Get(pawn), out innerColor, out outerColor);

		public static bool GetMarkerColors(Pawn pawn, DotConfig dotConfig, out Color innerColor, out Color outerColor)
		{
			using var measure = PerfMetrics.Measure("DotTools.GetMarkerColors");
			PerfMetrics.Count("get_marker_colors.calls");

			var animalPolicy = AnimalMarkerPolicy.For(pawn);
			if (animalPolicy.included == false)
			{
				innerColor = default;
				outerColor = default;
				return false;
			}

			var selected = Find.Selector.IsSelected(pawn) ? 1 : 0;

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
				if (colors?.Length == 2)
				{
					innerColor = colors[0];
					outerColor = colors[1];
					return true;
				}
			}

			if (animalPolicy.isAnimal || pawn.Faction != Faction.OfPlayer)
			{
				innerColor = Tools.GetMainColor(pawn);
				outerColor = pawn.Faction == Faction.OfPlayer ? playerAnimalOuterColors[selected] : PawnNameColorUtility.PawnNameColorOf(pawn);
				return true;
			}

			if (pawn.IsColonistPlayerControlled == false)
				GetDefaultColonistColors(selected, Settings.defaultColonistNormalOutline, Settings.defaultColonistNormalFill, Settings.defaultColonistNormalSelectedOutline, Settings.defaultColonistNormalSelectedFill, out innerColor, out outerColor);
			else if (pawn.IsPlayerControlled == false)
				GetDefaultColonistColors(selected, Settings.defaultColonistMentalOutline, Settings.defaultColonistMentalFill, Settings.defaultColonistMentalSelectedOutline, Settings.defaultColonistMentalSelectedFill, out innerColor, out outerColor);
			else if (pawn.Downed)
				GetDefaultColonistColors(selected, Settings.defaultColonistDownedOutline, Settings.defaultColonistDownedFill, Settings.defaultColonistDownedSelectedOutline, Settings.defaultColonistDownedSelectedFill, out innerColor, out outerColor);
			else if (pawn.Drafted)
				GetDefaultColonistColors(selected, Settings.defaultColonistDraftedOutline, Settings.defaultColonistDraftedFill, Settings.defaultColonistDraftedSelectedOutline, Settings.defaultColonistDraftedSelectedFill, out innerColor, out outerColor);
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
		{
			using var measure = PerfMetrics.Measure("DotTools.GetMarkerTextures");
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
