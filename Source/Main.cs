using Brrainz;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class CameraPlusMain : Mod
	{
		public static CameraPlusSettings Settings;
		public static float orthographicSize = -1f;

		// for other mods: set temporarily to true to skip any hiding
		public static bool skipCustomRendering = false;

		public CameraPlusMain(ModContentPack content) : base(content)
		{
			Settings = GetSettings<CameraPlusSettings>();

			var harmony = new Harmony("net.pardeike.rimworld.mod.camera+");
			harmony.PatchAll();

			CrossPromotion.Install(76561197973010050);
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

	[HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.Update))]
	static class CameraDriver_Update_Patch
	{
		static readonly MethodInfo m_SetRootSize = SymbolExtensions.GetMethodInfo(() => SetRootSize(null, 0f));

		static void SetRootSize(CameraDriver driver, float rootSize)
		{
			if (driver == null)
			{
				var info = Harmony.GetPatchInfo(AccessTools.Method(typeof(CameraDriver), nameof(CameraDriver.Update)));
				var owners = "Maybe one of the mods that patch CameraDriver.Update(): ";
				info.Owners.Do(owner => owners += owner + " ");
				Log.ErrorOnce("Unexpected null camera driver. Looks like a mod conflict. " + owners, 506973465);
				return;
			}

			if (Event.current.shift || CameraPlusMain.Settings.zoomToMouse == false)
			{
				driver.rootSize = rootSize;
				return;
			}

			driver.ApplyPositionToGameObject();
			var oldMousePos = FastUI.MouseMapPosition;
			driver.rootSize = rootSize;
			driver.ApplyPositionToGameObject();
			driver.rootPos += oldMousePos - UI.MouseMapPosition(); // dont use FastUI.MouseMapPosition here
		}

		static void Prefix(CameraDriver __instance)
		{
			if (CameraPlusMain.Settings.disableCameraShake)
				__instance.shaker.curShakeMag = 0;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var found = false;
			foreach (var instruction in instructions)
			{
				if (instruction.StoresField(Refs.f_rootSize))
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

	[HarmonyPatch(typeof(TimeControls), nameof(TimeControls.DoTimeControlsGUI))]
	static class TimeControls_DoTimeControlsGUI_Patch
	{
		static void Prefix()
		{
			Tools.HandleHotkeys();
		}
	}

	[HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.CalculateCurInputDollyVect))]
	static class CameraDriver_CalculateCurInputDollyVect_Patch
	{
		static void Postfix(ref Vector2 __result)
		{
			if (CameraPlusMain.orthographicSize != -1f)
				__result *= Tools.GetScreenEdgeDollyFactor(CameraPlusMain.orthographicSize);
		}
	}

	[HarmonyPatch(typeof(MoteMaker), nameof(MoteMaker.ThrowText))]
	[HarmonyPatch(new Type[] { typeof(Vector3), typeof(Map), typeof(string), typeof(Color), typeof(float) })]
	static class MoteMaker_ThrowText_Patch
	{
		static bool Prefix(Vector3 loc)
		{
			if (CameraPlusMain.skipCustomRendering)
				return true;

			if (CameraPlusMain.Settings.hideNamesWhenZoomedOut == false)
				return true;

			if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
				return true;

			if (CameraPlusMain.Settings.mouseOverShowsLabels)
				return Tools.MouseDistanceSquared(loc, true) <= 2.25f;

			return false;
		}
	}

	[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
	[HarmonyPatch(new Type[] { typeof(Vector3), typeof(Rot4?), typeof(bool) })]
	static class PawnRenderer_RenderPawnAt_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Pawn ___pawn)
		{
			if (CameraPlusMain.skipCustomRendering)
				return true;

			return Tools.ShouldShowDot(___pawn) == false;
		}

		[HarmonyPriority(10000)]
		static void Postfix(Pawn ___pawn)
		{
			if (CameraPlusMain.Settings.hideNamesWhenZoomedOut && CameraPlusMain.Settings.customNameStyle != LabelStyle.HideAnimals)
				_ = Tools.GetMainColor(___pawn); // trigger caching
		}
	}

	[HarmonyPatch(typeof(PawnUIOverlay), nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
	static class PawnUIOverlay_DrawPawnGUIOverlay_Patch
	{
		// fake everything being humanlike so Prefs.AnimalNameMode is ignored (we handle it ourselves)
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var mHumanlike = AccessTools.PropertyGetter(typeof(RaceProperties), nameof(RaceProperties.Humanlike));
			foreach (var code in instructions)
			{
				yield return code;
				if (code.Calls(mHumanlike))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return new CodeInstruction(OpCodes.Ldc_I4_1);
				}
			}
		}

		[HarmonyPriority(10000)]
		public static bool Prefix(Pawn ___pawn)
		{
			if (CameraPlusMain.skipCustomRendering)
				return true;

			if (!___pawn.Spawned || ___pawn.Map.fogGrid.IsFogged(___pawn.Position))
				return true;
			if (___pawn.RaceProps.Humanlike)
				return true;
			if (___pawn.Name != null)
				return true;

			var useMarkers = Tools.GetMarkerColors(___pawn, out var innerColor, out var outerColor);
			if (useMarkers == false)
				return true; // use label

			if (Tools.ShouldShowDot(___pawn))
			{
				Tools.DrawDot(___pawn, innerColor, outerColor);
				return false;
			}
			return Tools.CorrectLabelRendering(___pawn);
		}
	}

	[HarmonyPatch(typeof(GenMapUI), nameof(GenMapUI.DrawPawnLabel))]
	[HarmonyPatch(new Type[] { typeof(Pawn), typeof(Vector2), typeof(float), typeof(float), typeof(Dictionary<string, string>), typeof(GameFont), typeof(bool), typeof(bool) })]
	static class GenMapUI_DrawPawnLabel_Patch
	{
		[HarmonyPriority(10000)]
		public static bool Prefix(Pawn pawn, float truncateToWidth)
		{
			if (CameraPlusMain.skipCustomRendering)
				return true;

			if (truncateToWidth != 9999f)
				return true;

			if (Tools.ShouldShowDot(pawn))
			{
				var useMarkers = Tools.GetMarkerColors(pawn, out var innerColor, out var outerColor);
				if (useMarkers == false)
					return Tools.CorrectLabelRendering(pawn);

				Tools.DrawDot(pawn, innerColor, outerColor);
				return false;
			}

			// we fake "show all" so we need to skip if original could would not render labels
			if (ReversePatches.PerformsDrawPawnGUIOverlay(pawn.Drawer.ui) == false)
				return false;

			return Tools.ShouldShowLabel(pawn);
		}
	}

	// if we zoom in a lot, tiny font labels look very out of place
	// so we make them bigger with the available fonts
	//
	[HarmonyPatch(typeof(GenMapUI), nameof(GenMapUI.DrawThingLabel))]
	[HarmonyPatch(new Type[] { typeof(Vector2), typeof(string), typeof(Color) })]
	static class GenMapUI_DrawThingLabel_Patch
	{
		static readonly MethodInfo m_GetAdaptedGameFont = SymbolExtensions.GetMethodInfo(() => GetAdaptedGameFont(0f));

		static GameFont GetAdaptedGameFont(float rootSize)
		{
			if (rootSize < 11f) return GameFont.Medium;
			if (rootSize < 15f) return GameFont.Small;
			return GameFont.Tiny;
		}

		[HarmonyPriority(10000)]
		public static bool Prefix(Vector2 screenPos)
		{
			if (CameraPlusMain.skipCustomRendering)
				return true;

			return Tools.ShouldShowLabel(null, screenPos);
		}

		// we replace the first "GameFont.Tiny" with "GetAdaptedGameFont()"
		//
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var firstInstruction = true;
			foreach (var instruction in instructions)
			{
				if (firstInstruction && instruction.LoadsConstant(0))
				{
					yield return new CodeInstruction(OpCodes.Call, Refs.p_CameraDriver);
					yield return new CodeInstruction(OpCodes.Ldfld, Refs.f_rootSize);
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
	[HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.CurrentZoom), MethodType.Getter)]
	static class CameraDriver_CurrentZoom_Patch
	{
		// these values are from vanilla. we remap them to the range 30 - 60
		static readonly float[] sizes = new[] { 12f, 13.8f, 42f, 57f }
				.Select(f => Tools.LerpDoubleSafe(12, 57, 30, 60, f))
				.ToArray();

		public static bool Prefix(ref CameraZoomRange __result, float ___rootSize)
		{
			var lerped = Tools.LerpRootSize(___rootSize);
			if (lerped < sizes[0])
				__result = CameraZoomRange.Closest;
			else if (lerped < sizes[1])
				__result = CameraZoomRange.Close;
			else if (lerped < sizes[2])
				__result = CameraZoomRange.Middle;
			else if (lerped < sizes[3])
				__result = CameraZoomRange.Far;
			else
				__result = CameraZoomRange.Furthest;
			return false;
		}
	}

	[HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.ApplyPositionToGameObject))]
	static class CameraDriver_ApplyPositionToGameObject_Patch
	{
		static readonly MethodInfo m_ApplyZoom = SymbolExtensions.GetMethodInfo(() => ApplyZoom(null, null));

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

			var orthSize = Tools.LerpRootSize(camera.orthographicSize);
			camera.orthographicSize = orthSize;
			driver.config.dollyRateKeys = Tools.GetDollyRateKeys(orthSize);
			driver.config.dollyRateScreenEdge = Tools.GetDollyRateMouse(orthSize);
			driver.config.camSpeedDecayFactor = Tools.GetDollySpeedDecay(orthSize);
			CameraPlusMain.orthographicSize = orthSize;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
				if (instruction.opcode != OpCodes.Ret)
					yield return instruction;

			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, Refs.p_MyCamera);
			yield return new CodeInstruction(OpCodes.Call, m_ApplyZoom);
			yield return new CodeInstruction(OpCodes.Ret);
		}
	}

	// here, we basically add a "var lerpedRootSize = Main.LerpRootSize(this.rootSize);" to
	// the beginning of this method and replace every "this.rootSize" witn "lerpedRootSize"
	//
	[HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.CurrentViewRect), MethodType.Getter)]
	static class CameraDriver_CurrentViewRect_Patch
	{
		static readonly MethodInfo m_Main_LerpRootSize = SymbolExtensions.GetMethodInfo(() => Tools.LerpRootSize(0f));

		public static IEnumerable<CodeInstruction> Transpiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
		{
			var v_lerpedRootSize = generator.DeclareLocal(typeof(float));

			// store lerped rootSize in a new local var
			//
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldfld, Refs.f_rootSize);
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
					if (instruction.LoadsField(Refs.f_rootSize))
						instruction = new CodeInstruction(OpCodes.Ldloc, v_lerpedRootSize);
					else
						yield return new CodeInstruction(OpCodes.Ldarg_0); // repeat the code we did not emit in the first check
				}

				yield return instruction;
			}
		}
	}

	[HarmonyPatch]
	static class SaveOurShip2BackgroundPatch
	{
		public static bool Prepare() => TargetMethod() != null;
		public static MethodBase TargetMethod() { return AccessTools.Method("SaveOurShip2.MeshRecalculateHelper:RecalculateMesh"); }
		public static readonly MethodInfo mCenter = AccessTools.PropertyGetter(AccessTools.TypeByName("SaveOurShip2.SectionThreadManager"), "Center");

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var state = 0;
			foreach (var code in instructions)
			{
				switch (state)
				{
					case 0:
						if (code.opcode == OpCodes.Ldsflda && code.operand is MethodInfo method && method == mCenter)
							state = 1;
						break;
					case 1:
						if (code.opcode == OpCodes.Sub)
							state = 2;
						break;
					case 2:
						state = 3;
						break;
					case 3:
						yield return new CodeInstruction(OpCodes.Ldc_R4, 4f);
						yield return new CodeInstruction(OpCodes.Mul);
						state = 0;
						break;
				}
				yield return code;
			}
		}
	}

	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch(nameof(Map.MapUpdate))]
	static class Map_MapUpdate_Patch
	{
		static bool done = false;
		static void FixSoSMaterial()
		{
			done = true;
			var type = AccessTools.TypeByName("SaveOurShip2.RenderPlanetBehindMap");
			if (type != null)
			{
				var mat = Traverse.Create(type).Field("PlanetMaterial").GetValue<Material>();
				mat.mainTextureOffset = new Vector2(0.3f, 0.3f);
				mat.mainTextureScale = new Vector2(0.4f, 0.4f);
			}
		}

		static void Postfix(Map __instance)
		{
			if (done) return;
			if (WorldRendererUtility.WorldRenderedNow) return;
			if (Find.CurrentMap != __instance) return;
			FixSoSMaterial();
		}
	}
}
