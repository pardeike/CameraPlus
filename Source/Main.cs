using Verse;
using Harmony;
using UnityEngine;
using System.Reflection;
using Harmony.ILCopying;
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

		class AdaptedGameFontReplacer : IILProcessor
		{
			bool firstInstruction = true;

			public List<ILInstruction> Start(ILGenerator generator, MethodBase original)
			{
				return new List<ILInstruction>();
			}

			// we replace the first "GameFont.Tiny" with our "GetAdaptedGameFont()"
			//
			public List<ILInstruction> Process(ILInstruction instruction)
			{
				var result = new List<ILInstruction>();
				if (firstInstruction && instruction.opcode == OpCodes.Ldc_I4_0)
				{
					var method = AccessTools.Method(typeof(GenMapUI_DrawThingLabel_Patch), "GetAdaptedGameFont");
					var call = new ILInstruction(OpCodes.Call);
					call.operand = method;
					call.argument = method;
					result.Add(call);
				}
				else
					result.Add(instruction);

				firstInstruction = false;
				return result;
			}

			public List<ILInstruction> End(ILGenerator generator, MethodBase original)
			{
				return new List<ILInstruction>();
			}
		}

		[HarmonyProcessors]
		static HarmonyProcessor AdaptedGameFontReplacerPatch(MethodBase original)
		{
			var processor = new HarmonyProcessor();
			processor.AddILProcessor(new AdaptedGameFontReplacer());
			return processor;
		}
	}

	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch("get_CurrentZoom")]
	static class CameraDriver_get_CurrentZoom_Patch
	{
		class ZoomLerper : IILProcessor
		{
			public List<ILInstruction> Start(ILGenerator generator, MethodBase original)
			{
				return new List<ILInstruction>();
			}

			// Normal values: 12, 13.8, 42, 57
			//
			public List<ILInstruction> Process(ILInstruction instruction)
			{
				if (instruction.opcode == OpCodes.Ldc_R4)
				{
					var f = (float)instruction.operand;
					f = GenMath.LerpDouble(12, 57, 30, 60, f);
					instruction.operand = f;
					instruction.argument = f;
				}
				return new List<ILInstruction>() { instruction };
			}

			public List<ILInstruction> End(ILGenerator generator, MethodBase original)
			{
				return new List<ILInstruction>();
			}
		}

		[HarmonyProcessors]
		static HarmonyProcessor LerpCurrentZoom(MethodBase original)
		{
			var processor = new HarmonyProcessor();
			processor.AddILProcessor(new ZoomLerper());
			return processor;
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
}