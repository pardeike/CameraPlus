using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	class CameraPlusMain : Mod
	{
		public static CameraPlusSettings Settings;

		public CameraPlusMain(ModContentPack content) : base(content)
		{
			Settings = GetSettings<CameraPlusSettings>();

			var harmony = HarmonyInstance.Create("net.pardeike.rimworld.mod.camera+");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
			FireStats.Trigger(true);
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Settings.DoWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "Camera+";
		}
	}

	[HarmonyPatch(typeof(Game))]
	[HarmonyPatch("FinalizeInit")]
	static class Game_FinalizeInit_Patch
	{
		static void Postfix()
		{
			FireStats.Trigger(false);
		}
	}

	[HarmonyPatch(typeof(MoteMaker))]
	[HarmonyPatch("ThrowText")]
	[HarmonyPatch(new Type[] { typeof(Vector3), typeof(Map), typeof(string), typeof(Color), typeof(float) })]
	static class MoteMaker_ThrowText_Patch
	{
		static bool Prefix(Vector3 loc)
		{
			if (CameraPlusMain.Settings.hideNamesWhenZoomedOut == false)
				return true;

			var d = Find.CameraDriver.CellSizePixels;
			if (d >= 12f)
				return true;

			var v1 = UI.MouseCell().ToVector3().MapToUIPosition();
			var v2 = loc.MapToUIPosition();
			if (Vector2.Distance(v1, v2) <= 28f)
				return true;

			return false;
		}
	}

	[HarmonyPatch(typeof(GenMapUI))]
	[HarmonyPatch("DrawPawnLabel")]
	[HarmonyPatch(new Type[] { typeof(Pawn), typeof(Vector2), typeof(float), typeof(float), typeof(Dictionary<string, string>), typeof(GameFont), typeof(bool), typeof(bool) })]
	[StaticConstructorOnStartup]
	static class GenMapUI_DrawPawnLabel_Patch
	{
		public static Texture2D innerTexture = ContentFinder<Texture2D>.Get("InnerMarker", true);
		public static Texture2D outerTexture = ContentFinder<Texture2D>.Get("OuterMarker", true);

		static bool Prefix(Pawn pawn, float truncateToWidth)
		{
			if (CameraPlusMain.Settings.hideNamesWhenZoomedOut == false || truncateToWidth != 9999f)
				return true;

			var d = Find.CameraDriver.CellSizePixels;
			if (d >= 12f)
				return true;

			var pos = pawn.DrawPos;
			var v1 = UI.MouseCell().ToVector3().MapToUIPosition();
			var v2 = pos.MapToUIPosition();
			if (Vector2.Distance(v1, v2) <= 28f)
				return true;

			v1 = (pos - new Vector3(0.75f, 0f, 0.75f)).MapToUIPosition().Rounded();
			v2 = (pos + new Vector3(0.75f, 0f, 0.75f)).MapToUIPosition().Rounded();

			GUI.color = Find.Selector.IsSelected(pawn) ? Color.black : Color.white;
			GUI.DrawTexture(new Rect(v1, v2 - v1), outerTexture, ScaleMode.ScaleToFit, true);
			GUI.color = PawnNameColorUtility.PawnNameColorOf(pawn);
			GUI.DrawTexture(new Rect(v1, v2 - v1), innerTexture, ScaleMode.ScaleToFit, true);

			return false;
		}
	}

	// if we zoom in a lot, tiny font labels look very out of place
	// so we make them bigger with the available fonts
	//
	[HarmonyPatch(typeof(GenMapUI))]
	[HarmonyPatch("DrawThingLabel")]
	[HarmonyPatch(new Type[] { typeof(Vector2), typeof(string), typeof(Color) })]
	static class GenMapUI_DrawThingLabel_Patch
	{
		static GameFont GetAdaptedGameFont(float rootSize)
		{
			if (rootSize < 11f) return GameFont.Medium;
			if (rootSize < 15f) return GameFont.Small;
			return GameFont.Tiny;
		}

		// we replace the first "GameFont.Tiny" with "GetAdaptedGameFont(Find.CameraDriver.rootSize)"
		//
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> AdaptedGameFontReplacerPatch(IEnumerable<CodeInstruction> instructions)
		{
			var firstInstruction = true;
			foreach (var instruction in instructions)
			{
				if (firstInstruction && instruction.opcode == OpCodes.Ldc_I4_0)
				{
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Find), "get_CameraDriver"));
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CameraDriver), "rootSize"));
					yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GenMapUI_DrawThingLabel_Patch), "GetAdaptedGameFont"));
				}
				else
					yield return instruction;

				firstInstruction = false;
			}
		}
	}

	// map our new camera settings to meaningful enum values
	//
	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch("CurrentZoom", MethodType.Getter)]
	static class CameraDriver_CurrentZoom_Patch
	{
		// normal values: 12, 13.8, 42, 57
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
		static void ApplyZoom(CameraDriver driver, Camera camera)
		{
			// small note: moving the camera too far out requires adjusting the clipping distance
			//
			var pos = camera.transform.position;
			var cameraSpan = CameraPlusSettings.maxRootOutput - CameraPlusSettings.minRootOutput;
			var f = (pos.y - CameraPlusSettings.minRootOutput) / cameraSpan;
			f *= 1 - CameraPlusMain.Settings.soundNearness;
			pos.y = CameraPlusSettings.minRootOutput + f * cameraSpan;
			camera.transform.position = pos;

			var orthSize = CameraPlusSettings.LerpRootSize(camera.orthographicSize);
			camera.orthographicSize = orthSize;
			driver.config.dollyRateKeys = CameraPlusSettings.GetDollyRate(orthSize);
		}

		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Postfix_ApplyPositionToGameObject(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
				if (instruction.opcode != OpCodes.Ret)
					yield return instruction;

			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CameraDriver), "get_MyCamera"));
			yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CameraDriver_ApplyPositionToGameObject_Patch), "ApplyZoom"));
			yield return new CodeInstruction(OpCodes.Ret);
		}
	}

	/* increase clipping distance
	//
	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch("Awake")]
	static class CameraDriver_Awake_Patch
	{
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Postfix_Awake(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldc_R4)
					instruction.operand = CameraPlusSettings.farOutHeight + 5;
				yield return instruction;
			}
		}
	}*/

	// here, we basically add a "var lerpedRootSize = Main.LerpRootSize(this.rootSize);" to
	// the beginning of this method and replace every "this.rootSize" witn "lerpedRootSize"
	//
	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch("CurrentViewRect", MethodType.Getter)]
	static class CameraDriver_CurrentViewRect_Patch
	{
		static readonly FieldInfo f_CameraDriver_rootSize = AccessTools.Field(typeof(CameraDriver), "rootSize");
		static readonly MethodInfo m_Main_LerpRootSize = AccessTools.Method(typeof(CameraPlusSettings), nameof(CameraPlusSettings.LerpRootSize));

		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> LerpCurrentViewRect(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
		{
			var v_lerpedRootSize = generator.DeclareLocal(typeof(float));

			// store lerped rootSize in a new local var
			//
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldfld, f_CameraDriver_rootSize);
			yield return new CodeInstruction(OpCodes.Call, m_Main_LerpRootSize);
			yield return new CodeInstruction(OpCodes.Stloc, v_lerpedRootSize);

			var previousCodeWasLdArg0 = false;
			foreach (var instr in instructions)
			{
				var instruction = instr; // make it writeable

				if (instruction.opcode == OpCodes.Ldarg_0)
				{
					previousCodeWasLdArg0 = true;
					continue; // do not emit the code
				}

				if (previousCodeWasLdArg0)
				{
					previousCodeWasLdArg0 = false;

					// looking for Ldarg.0 followed by Ldfld rootSize
					//
					if (instruction.opcode == OpCodes.Ldfld && instruction.operand == f_CameraDriver_rootSize)
						instruction = new CodeInstruction(OpCodes.Ldloc, v_lerpedRootSize);
					else
						yield return new CodeInstruction(OpCodes.Ldarg_0); // repeat the code we did not emit in the first check
				}

				yield return instruction;
			}
		}
	}
}