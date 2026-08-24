using HarmonyLib;
using RimWorld;
using Verse;

namespace CameraPlus
{
	[HarmonyPatch(typeof(Game), nameof(Game.DeinitAndRemoveMap))]
	static class Game_DeinitAndRemoveMap_Patch
	{
		static void Prefix(Map map)
		{
			MarkerCache.RemoveForMap(map);
		}
	}

	[HarmonyPatch(typeof(Pawn), nameof(Pawn.DeSpawn))]
	static class Pawn_DeSpawn_Patch
	{
		static void Prefix(Pawn __instance)
		{
			MarkerCache.Remove(__instance);
		}
	}

	[HarmonyPatch(typeof(Pawn), nameof(Pawn.Destroy))]
	static class Pawn_Destroy_Patch
	{
		static void Prefix(Pawn __instance)
		{
			MarkerCache.Remove(__instance);
		}
	}

	[HarmonyPatch(typeof(SilhouetteUtility), nameof(SilhouetteUtility.NotifyGraphicDirty))]
	static class SilhouetteUtility_NotifyGraphicDirty_Patch
	{
		static void Postfix(Thing thing)
		{
			if (thing is Pawn pawn)
				MarkerCache.Remove(pawn);
		}
	}
}
