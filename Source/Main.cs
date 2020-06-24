using HarmonyLib;
using RimWorld;
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
	[HarmonyPatch(nameof(Game.FinalizeInit))]
	static class Game_FinalizeInit_Patch
	{
		static void Postfix()
		{
			ModCounter.Trigger();
		}
	}

	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch(nameof(CameraDriver.Update))]
	static class CameraDriver_Update_Patch
	{
		static readonly MethodInfo m_SetRootSize = SymbolExtensions.GetMethodInfo(() => SetRootSize(null, 0f));

		static void SetRootSize(CameraDriver driver, float rootSize)
		{
			if (driver == null)
			{
				var info = Harmony.GetPatchInfo(AccessTools.Method(typeof(CameraDriver), "Update"));
				var owners = "Maybe one of the mods that patch CameraDriver.Update(): ";
				info.Owners.Do(owner => owners += owner + " ");
				Log.ErrorOnce("Unexpected null camera driver. Looks like a mod conflict. " + owners, 506973465);
				return;
			}

			if (Event.current.shift || CameraPlusMain.Settings.zoomToMouse == false)
			{
				Refs.rootSize(driver) = rootSize;
				return;
			}
			var rootPos = Refs.rootPos(driver);
			_ = Refs.applyPositionToGameObjectInvoker(driver, Array.Empty<object>());
			var oldMousePos = UI.MouseMapPosition();
			Refs.rootSize(driver) = rootSize;
			_ = Refs.applyPositionToGameObjectInvoker(driver, Array.Empty<object>());
			rootPos += oldMousePos - UI.MouseMapPosition();
			Refs.rootPos(driver) = rootPos;
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

	[HarmonyPatch(typeof(TimeControls))]
	[HarmonyPatch(nameof(TimeControls.DoTimeControlsGUI))]
	static class TimeControls_DoTimeControlsGUI_Patch
	{
		static void Prefix()
		{
			Tools.HandleHotkeys();
		}
	}

	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch("CalculateCurInputDollyVect")]
	static class CameraDriver_CalculateCurInputDollyVect_Patch
	{
		static void Postfix(ref Vector2 __result)
		{
			if (CameraPlusMain.orthographicSize != -1f)
				__result *= Tools.GetScreenEdgeDollyFactor(CameraPlusMain.orthographicSize);
		}
	}

	[HarmonyPatch(typeof(MoteMaker))]
	[HarmonyPatch(nameof(MoteMaker.ThrowText))]
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

			// show if mouse is nearby
			if (CameraPlusMain.Settings.mouseOverShowsLabels)
				return Tools.MouseDistanceSquared(loc, true) <= 2.25f;

			return false;
		}
	}

	[HarmonyPatch(typeof(PawnRenderer))]
	[HarmonyPatch(nameof(PawnRenderer.RenderPawnAt))]
	[HarmonyPatch(new Type[] { typeof(Vector3), typeof(RotDrawMode), typeof(bool), typeof(bool) })]
	static class PawnRenderer_RenderPawnAt_Patch
	{
		[HarmonyPriority(10000)]
		static bool Prefix(Pawn ___pawn)
		{
			if (CameraPlusMain.skipCustomRendering)
				return true;

			var cameraDelegate = Tools.GetCachedCameraDelegate(___pawn);
			if (cameraDelegate.GetCameraColors == null)
			{
				if (CameraPlusMain.Settings.customNameStyle == LabelStyle.HideAnimals)
					return true;
			}

			if (Tools.PawnHasNoLabel(___pawn))
				return true;

			return Tools.ShouldShowBody(___pawn);
		}

		static void Postfix(Pawn ___pawn)
		{
			if (CameraPlusMain.Settings.hideNamesWhenZoomedOut && CameraPlusMain.Settings.customNameStyle != LabelStyle.HideAnimals)
				_ = Tools.GetMainColor(___pawn); // trigger caching
		}
	}

	[HarmonyPatch(typeof(PawnUIOverlay))]
	[HarmonyPatch(nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
	static class PawnUIOverlay_DrawPawnGUIOverlay_Patch
	{
		static AnimalNameDisplayMode AnimalNameMode()
		{
			if (CameraPlusMain.Settings.includeNotTamedAnimals)
				return AnimalNameDisplayMode.TameAll;
			return Prefs.AnimalNameMode;
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var from = AccessTools.PropertyGetter(typeof(Prefs), nameof(Prefs.AnimalNameMode));
			var into = SymbolExtensions.GetMethodInfo(() => AnimalNameMode());
			return Transpilers.MethodReplacer(instructions, from, into);
		}

		[HarmonyPriority(10000)]
		public static bool Prefix(Pawn ___pawn)
		{
			if (CameraPlusMain.skipCustomRendering)
				return true;

			if (CameraPlusMain.Settings.includeNotTamedAnimals == false)
				return true;

			if (!___pawn.Spawned || ___pawn.Map.fogGrid.IsFogged(___pawn.Position))
				return true;
			if (___pawn.RaceProps.Humanlike)
				return true;
			if (___pawn.Name != null)
				return true;

			return GenMapUI_DrawPawnLabel_Patch.HandlePawn(___pawn);
		}
	}

	[HarmonyPatch(typeof(GenMapUI))]
	[HarmonyPatch(nameof(GenMapUI.DrawPawnLabel))]
	[HarmonyPatch(new Type[] { typeof(Pawn), typeof(Vector2), typeof(float), typeof(float), typeof(Dictionary<string, string>), typeof(GameFont), typeof(bool), typeof(bool) })]
	[StaticConstructorOnStartup]
	static class GenMapUI_DrawPawnLabel_Patch
	{
		static readonly Texture2D downedTexture = ContentFinder<Texture2D>.Get("DownedMarker", true);
		static readonly Texture2D draftedTexture = ContentFinder<Texture2D>.Get("DraftedMarker", true);
		static readonly Color downedColor = new Color(0.9f, 0f, 0f);
		static readonly Color draftedColor = new Color(0f, 0.5f, 0f);

		public static bool HandlePawn(Pawn pawn)
		{
			Tools.ShouldShowLabel(pawn.DrawPos, true, out var showLabel, out var showDot);
			if (showLabel)
				return true;
			if (showDot == false)
				return false;

			var useMarkers = Tools.GetMarkerColors(pawn, out var innerColor, out var outerColor);
			if (useMarkers == false)
				return true; // use label

			_ = Tools.GetMarkerTextures(pawn, out var innerTexture, out var outerTexture);

			var pos = pawn.DrawPos;
			var v1 = (pos - new Vector3(0.75f, 0f, 0.75f)).MapToUIPosition().Rounded();
			var v2 = (pos + new Vector3(0.75f, 0f, 0.75f)).MapToUIPosition().Rounded();
			var markerRect = new Rect(v1, v2 - v1);

			// draw outer marker
			GUI.color = outerColor;
			GUI.DrawTexture(markerRect, outerTexture, ScaleMode.ScaleToFit, true);

			// draw inner marker
			GUI.color = innerColor;
			GUI.DrawTexture(markerRect, innerTexture, ScaleMode.ScaleToFit, true);

			// draw extra marker
			if (pawn.Downed)
			{
				GUI.color = downedColor;
				GUI.DrawTexture(markerRect, downedTexture, ScaleMode.ScaleToFit, true);
			}
			else if (pawn.Drafted)
			{
				GUI.color = draftedColor;
				GUI.DrawTexture(markerRect, draftedTexture, ScaleMode.ScaleToFit, true);
			}

			// skip label
			return false;
		}

		[HarmonyPriority(10000)]
		public static bool Prefix(Pawn pawn, float truncateToWidth)
		{
			if (CameraPlusMain.skipCustomRendering)
				return true;

			if (truncateToWidth != 9999f)
				return true; // use label

			return HandlePawn(pawn);
		}
	}

	// if we zoom in a lot, tiny font labels look very out of place
	// so we make them bigger with the available fonts
	//
	[HarmonyPatch(typeof(GenMapUI))]
	[HarmonyPatch(nameof(GenMapUI.DrawThingLabel))]
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

			Tools.ShouldShowLabel(screenPos, false, out var showLabel, out _);
			return showLabel;
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
	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch(nameof(CameraDriver.CurrentZoom), MethodType.Getter)]
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
				if (Tools.LerpRootSize(___rootSize) < sizes[i])
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
	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch(nameof(CameraDriver.CurrentViewRect), MethodType.Getter)]
	static class CameraDriver_CurrentViewRect_Patch
	{
		static readonly MethodInfo m_Main_LerpRootSize = SymbolExtensions.GetMethodInfo(() => Tools.LerpRootSize(0f));

		static IEnumerable<CodeInstruction> Transpiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
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
}