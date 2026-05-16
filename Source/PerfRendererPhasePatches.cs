#if CAMERAPLUS_PERF
using HarmonyLib;
using UnityEngine;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.DynamicDrawPhaseAt))]
	[HarmonyPatch([typeof(DrawPhase), typeof(Vector3), typeof(Rot4?), typeof(bool)])]
	static class PerfRendererPhase_PawnRenderer_DynamicDrawPhaseAt_Patch
	{
		[HarmonyPriority(10000)]
		public static bool Prefix(Pawn ___pawn, DrawPhase phase)
		{
			if (skipCustomRendering || ___pawn == null || ___pawn.Dead)
				return true;

			if (MarkerDecisionCache.Get(___pawn).suppressVanilla == false)
				return true;

			PerfMetrics.Count($"renderer_phase.skip.{phase}");
			return false;
		}
	}
}
#endif
