using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using static Harmony.AccessTools;

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
			ModCounter.Trigger();
		}
	}

	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch("Update")]
	static class CameraDriver_Update_Patch
	{
		static readonly FieldRef<CameraDriver, Vector3> rootPosRef = FieldRefAccess<CameraDriver, Vector3>("rootPos");
		static readonly FieldRef<CameraDriver, float> rootSizeRef = FieldRefAccess<CameraDriver, float>("rootSize");
		static readonly MethodInfo m_ApplyPositionToGameObject = Method(typeof(CameraDriver), "ApplyPositionToGameObject");
		static readonly FastInvokeHandler applyPositionToGameObjectInvoker = MethodInvoker.GetHandler(m_ApplyPositionToGameObject);

		static void SetRootSize(CameraDriver driver, float rootSize)
		{
			if (Event.current.shift || CameraPlusMain.Settings.zoomToMouse == false)
			{
				rootSizeRef(driver) = rootSize;
				return;
			}
			var rootPos = rootPosRef(driver);
			applyPositionToGameObjectInvoker(driver, new object[0]);
			var oldMousePos = UI.MouseMapPosition();
			rootSizeRef(driver) = rootSize;
			applyPositionToGameObjectInvoker(driver, new object[0]);
			rootPos += oldMousePos - UI.MouseMapPosition();
			rootPosRef(driver) = rootPos;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var f_CameraDriver_rootSize = Field(typeof(CameraDriver), "rootSize");
			if (f_CameraDriver_rootSize == null)
				Log.Error("Cannot find field CameraDriver.rootSize");

			var m_SetRootSize = SymbolExtensions.GetMethodInfo(() => SetRootSize(null, 0f));
			var found = false;
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Stfld && instruction.operand == f_CameraDriver_rootSize)
				{
					instruction.opcode = OpCodes.Call;
					instruction.operand = m_SetRootSize;
					found = true;
				}
				yield return instruction;
			}
			if (found == false)
				Log.Error("Cannot find field Stdfld rootSize in CameraDriver.Update");
		}
	}

	// open Camera+ preferences by pressing Shift-Tab
	// (for now disabled because this might conflict with other usages of that key combo)
	/*
	[HarmonyPatch(typeof(MainTabsRoot))]
	[HarmonyPatch(nameof(MainTabsRoot.HandleLowPriorityShortcuts))]
	static class MainTabsRoot_HandleLowPriorityShortcuts_Patch
	{
		static readonly FieldRef<Dialog_ModSettings, Mod> selModByRef = FieldRefAccess<Dialog_ModSettings, Mod>("selMod");

		static void Postfix()
		{
			if (Input.GetKey(KeyCode.Tab) == false)
				return;
			if (Input.GetKey(KeyCode.LeftShift) == false)
				return;

			var stack = Find.WindowStack;
			if (stack.IsOpen<Dialog_ModSettings>())
				return;

			var dialog = new Dialog_ModSettings();
			var me = LoadedModManager.GetMod<CameraPlusMain>();
			selModByRef(dialog) = me;
			stack.Add(dialog);
		}
	}*/

	[HarmonyPatch(typeof(MoteMaker))]
	[HarmonyPatch("ThrowText")]
	[HarmonyPatch(new Type[] { typeof(Vector3), typeof(Map), typeof(string), typeof(Color), typeof(float) })]
	static class MoteMaker_ThrowText_Patch
	{
		static bool Prefix(Vector3 loc)
		{
			if (CameraPlusMain.Settings.hideNamesWhenZoomedOut == false)
				return true;

			if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
				return true;

			// show if mouse is nearby
			var v1 = UI.MouseCell().ToVector3().MapToUIPosition();
			var v2 = loc.MapToUIPosition();
			return Vector2.Distance(v1, v2) <= 28f;
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
		public static Texture2D downedTexture = ContentFinder<Texture2D>.Get("DownedMarker", true);
		public static Texture2D draftedTexture = ContentFinder<Texture2D>.Get("DraftedMarker", true);
		public static Color downedColor = new Color(0.9f, 0f, 0f);
		public static Color draftedColor = new Color(0f, 0.5f, 0f);

		static bool Prefix(Pawn pawn, float truncateToWidth, ref GameFont font)
		{
			if (CameraPlusMain.Settings.hideNamesWhenZoomedOut == false || truncateToWidth != 9999f)
				return true;

			if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
				return true;

			var pos = pawn.DrawPos;
			var v1 = UI.MouseCell().ToVector3().MapToUIPosition();
			var v2 = pos.MapToUIPosition();
			if (Vector2.Distance(v1, v2) <= 28f)
				return true;

			v1 = (pos - new Vector3(0.75f, 0f, 0.75f)).MapToUIPosition().Rounded();
			v2 = (pos + new Vector3(0.75f, 0f, 0.75f)).MapToUIPosition().Rounded();
			var r = new Rect(v1, v2 - v1);

			GUI.color = Find.Selector.IsSelected(pawn) ? Color.black : Color.white;
			GUI.DrawTexture(r, outerTexture, ScaleMode.ScaleToFit, true);
			GUI.color = PawnNameColorUtility.PawnNameColorOf(pawn);
			GUI.DrawTexture(r, innerTexture, ScaleMode.ScaleToFit, true);
			if (pawn.Downed)
			{
				GUI.color = downedColor;
				GUI.DrawTexture(r, downedTexture, ScaleMode.ScaleToFit, true);
			}
			else if (pawn.Drafted)
			{
				GUI.color = draftedColor;
				GUI.DrawTexture(r, draftedTexture, ScaleMode.ScaleToFit, true);
			}

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
		static readonly MethodInfo m_GetAdaptedGameFont = SymbolExtensions.GetMethodInfo(() => GetAdaptedGameFont(0f));
		static readonly MethodInfo m_get_CameraDriver = Method(typeof(Find), "get_CameraDriver");
		static readonly FieldInfo f_rootSize = Field(typeof(CameraDriver), "rootSize");

		static GameFont GetAdaptedGameFont(float rootSize)
		{
			if (rootSize < 11f) return GameFont.Medium;
			if (rootSize < 15f) return GameFont.Small;
			return GameFont.Tiny;
		}

		// we replace the first "GameFont.Tiny" with "GetAdaptedGameFont()"
		//
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var firstInstruction = true;
			foreach (var instruction in instructions)
			{
				if (firstInstruction && instruction.opcode == OpCodes.Ldc_I4_0)
				{
					yield return new CodeInstruction(OpCodes.Call, m_get_CameraDriver);
					yield return new CodeInstruction(OpCodes.Ldfld, f_rootSize);
					yield return new CodeInstruction(OpCodes.Call, m_GetAdaptedGameFont);
					firstInstruction = false;
				}
				else
					yield return instruction;
			}
		}
	}

	// map our new camera settings to meaningful enum values
	//
	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch("CurrentZoom", MethodType.Getter)]
	static class CameraDriver_CurrentZoom_Patch
	{
		static bool Prefix(ref CameraZoomRange __result, float ___rootSize)
		{
			// these values are from vanilla
			// we remap them to the range 30 - 60
			var sizes = new[] { 12f, 13.8f, 42f, 57f }
				.Select(f => GenMath.LerpDouble(12, 57, 30, 60, f))
				.ToArray();

			__result = CameraZoomRange.Furthest;
			for (var i = 0; i < 4; i++)
				if (CameraPlusSettings.LerpRootSize(___rootSize) < sizes[i])
				{
					__result = (CameraZoomRange)i;
					break;
				}
			return false;
		}
	}

	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch("ApplyPositionToGameObject")]
	static class CameraDriver_ApplyPositionToGameObject_Patch
	{
		static readonly MethodInfo m_ApplyZoom = SymbolExtensions.GetMethodInfo(() => ApplyZoom(null, null));
		static readonly MethodInfo m_get_MyCamera = Method(typeof(CameraDriver), "get_MyCamera");

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
			driver.config.camSpeedDecayFactor = CameraPlusSettings.GetDollySpeedDecay(orthSize);
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			if (m_get_MyCamera == null)
				Log.Error("Cannot find property CameraDriver.MyCamera");

			foreach (var instruction in instructions)
				if (instruction.opcode != OpCodes.Ret)
					yield return instruction;

			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, m_get_MyCamera);
			yield return new CodeInstruction(OpCodes.Call, m_ApplyZoom);
			yield return new CodeInstruction(OpCodes.Ret);
		}
	}

	/* increase clipping distance
	//
	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch("Awake")]
	static class CameraDriver_Awake_Patch
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
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
		static readonly FieldInfo f_CameraDriver_rootSize = Field(typeof(CameraDriver), "rootSize");
		static readonly MethodInfo m_Main_LerpRootSize = SymbolExtensions.GetMethodInfo(() => CameraPlusSettings.LerpRootSize(0f));

		static IEnumerable<CodeInstruction> Transpiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
		{
			if (f_CameraDriver_rootSize == null)
				Log.Error("Cannot find field CameraDriver.rootSize");

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