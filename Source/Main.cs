using Verse;
using Harmony;
using UnityEngine;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CameraPlus
{
	[StaticConstructorOnStartup]
	class Main
	{
		static Main()
		{
			var harmony = HarmonyInstance.Create("net.pardeike.rimworld.mod.camera+");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}

	[HarmonyPatch(typeof(GenMapUI))]
	[HarmonyPatch("DrawThingLabel")]
	[HarmonyPatch(new Type[] { typeof(Vector2), typeof(string), typeof(Color) })]
	static class GenMapUI_DrawThingLabel_Patch
	{
		static GameFont GetAdaptedGameFont()
		{
			var rootSize = Traverse.Create(Find.CameraDriver).Field("rootSize").GetValue<float>();
			if (rootSize < 14f) return GameFont.Medium;
			if (rootSize < 19f) return GameFont.Small;
			return GameFont.Tiny;
		}

		// we replace the first "GameFont.Tiny" with our "GetAdaptedGameFont()"
		//
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> AdaptedGameFontReplacerPatch(IEnumerable<CodeInstruction> instructions)
		{
			bool firstInstruction = true;
			foreach (var instruction in instructions)
			{
				if (firstInstruction && instruction.opcode == OpCodes.Ldc_I4_0)
				{
					var method = AccessTools.Method(typeof(GenMapUI_DrawThingLabel_Patch), "GetAdaptedGameFont");
					yield return new CodeInstruction(OpCodes.Call, method);
				}
				else
					yield return instruction;

				firstInstruction = false;
			}
		}
	}

	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch("get_CurrentZoom")]
	static class CameraDriver_get_CurrentZoom_Patch
	{
		// Normal values: 12, 13.8, 42, 57
		//
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> LerpCurrentZoom(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldc_R4)
				{
					var f = (float)instruction.operand;
					f = GenMath.LerpDouble(12, 57, 30, 60, f);
					instruction.operand = f;
					yield return instruction;
				}
				else
					yield return instruction;
			}
		}
	}

	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch("ApplyPositionToGameObject")]
	static class CameraDriver_ApplyPositionToGameObject_Patch
	{
		static void Postfix(CameraDriver __instance)
		{
			var trv = Traverse.Create(__instance);
			var p_MyCamera = trv.Property("MyCamera").GetValue<Camera>();
			var pos = p_MyCamera.transform.position;
			var y = p_MyCamera.transform.position.y;
			var o = p_MyCamera.orthographicSize;

			var y2 = GenMath.LerpDouble(65, 15, 65, 32, y);
			var o2 = GenMath.LerpDouble(60, 11, 60, 0.5f, o);
			var dolly = GenMath.LerpDouble(65, 15, 1, 3, y);
			dolly = 85 - dolly * dolly * dolly * dolly;

			p_MyCamera.transform.position = new Vector3(pos.x, y2, pos.z);
			p_MyCamera.orthographicSize = o2;
			__instance.config.dollyRateKeys = dolly;
		}
	}

    [HarmonyPatch(typeof(CameraDriver))]
    [HarmonyPatch("get_CurrentViewRect")]
    static class CameraDriver_get_CurrentViewRect_Patch
    {
        static FieldInfo FieldInfo_CamerDriver_rootSize = AccessTools.Field(typeof(CameraDriver), "rootSize");
        static MethodInfo MethodInfo_LerpRootSize = AccessTools.Method(typeof(CameraDriver_get_CurrentViewRect_Patch), nameof(LerpRootSize));

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> LerpCurrentZoom(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldfld && instruction.operand == FieldInfo_CamerDriver_rootSize)
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Call, MethodInfo_LerpRootSize);
                }
                else
                    yield return instruction;
            }
        }

        static float LerpRootSize(float rootSize) => GenMath.LerpDouble(60, 11, 60, 0.5f, rootSize);
    }

}