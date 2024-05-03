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
	[DefOf]
	public static class Defs
	{
		public static SoundDef SnapBack;
		public static SoundDef ApplySnap;
	}

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
			=> Settings.DoWindowContents(inRect);

		public override string SettingsCategory() => "Camera+";
	}

	[HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.Update))]
	static class CameraDriver_Update_Patch
	{
		static readonly MethodInfo m_SetRootSizeOriginal = AccessTools.PropertySetter(typeof(CameraDriver), nameof(CameraDriver.RootSize));
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

			if (driver.rootSize != rootSize)
			{
				driver.ApplyPositionToGameObject();
				var oldMousePos = FastUI.MouseMapPosition;
				driver.rootSize = rootSize;
				driver.ApplyPositionToGameObject();
				driver.rootPos += oldMousePos - UI.MouseMapPosition(); // dont use FastUI.MouseMapPosition here
			}
		}

		public static void Prefix(CameraDriver __instance)
		{
			if (CameraPlusMain.Settings.disableCameraShake)
				__instance.shaker.curShakeMag = 0;
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			=> instructions.MethodReplacer(m_SetRootSizeOriginal, m_SetRootSize);
	}

	[HarmonyPatch(typeof(TimeControls), nameof(TimeControls.DoTimeControlsGUI))]
	static class TimeControls_DoTimeControlsGUI_Patch
	{
		public static void Prefix() => Tools.HandleHotkeys();
	}

	[HarmonyPatch(typeof(CameraDriver), nameof(CameraDriver.CalculateCurInputDollyVect))]
	static class CameraDriver_CalculateCurInputDollyVect_Patch
	{
		public static void Postfix(ref Vector2 __result)
		{
			if (CameraPlusMain.orthographicSize != -1f)
				__result *= Tools.GetScreenEdgeDollyFactor(CameraPlusMain.orthographicSize);
		}
	}

	[HarmonyPatch(typeof(MoteMaker), nameof(MoteMaker.ThrowText))]
	[HarmonyPatch([typeof(Vector3), typeof(Map), typeof(string), typeof(Color), typeof(float)])]
	static class MoteMaker_ThrowText_Patch
	{
		public static bool Prefix(Vector3 loc)
		{
			if (CameraPlusMain.skipCustomRendering)
				return true;

			var settings = CameraPlusMain.Settings;

			if (settings.dotStyle == DotStyle.VanillaDefault)
				return true;

			if (Current.cameraDriverInt.CurrentZoom == CameraZoomRange.Closest)
				return true;

			if (settings.mouseOverShowsLabels)
				return Tools.MouseDistanceSquared(loc, true) <= 2.25f;

			return false;
		}
	}

	[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
	[HarmonyPatch(new Type[] { typeof(Vector3), typeof(Rot4?), typeof(bool) })]
	static class PawnRenderer_RenderPawnAt_Patch
	{
		[HarmonyPriority(10000)]
		public static bool Prefix(Pawn ___pawn)
		{
			if (CameraPlusMain.skipCustomRendering)
				return true;
			return Tools.ShouldShowMarker(___pawn, true) == false;
		}
	}

	[HarmonyPatch(typeof(PawnUIOverlay), nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
	static class PawnUIOverlay_DrawPawnGUIOverlay_Patch
	{
		[HarmonyPriority(10000)]
		public static bool Prefix(Pawn ___pawn)
		{
			if (CameraPlusMain.skipCustomRendering)
				return true;

			if (Tools.GetMarkerColors(___pawn, out _, out _) == false)
				return true;

			return Tools.ShouldShowMarker(___pawn, true) == false;
		}
	}
	//
	// Dubs Performance Analyzer patch on PawnUIOverlay.DrawPawnGUIOverlay needs to be turned off
	//
	[HarmonyPatch]
	static class Analyzer_Fixes_H_DrawNamesFix_Prefix_Patch
	{
		public static bool Prepare() => TargetMethod() != null;
		public static MethodInfo TargetMethod() => AccessTools.Method("Analyzer.Fixes.H_DrawNamesFix:Prefix");
		public static bool Prefix(ref bool __result)
		{
			__result = true;
			return false;
		}
	}

	[HarmonyPatch(typeof(SilhouetteUtility), nameof(SilhouetteUtility.ShouldDrawSilhouette))]
	static class SilhouetteUtility_ShouldDrawSilhouette_Patch
	{
		static bool Prefix(Thing thing, ref bool __result)
		{
			if (thing is Pawn pawn && Tools.ShouldShowMarker(pawn, true))
			{
				__result = false;
				return false;
			}
			return true;
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

			if (Tools.ShouldShowMarker(pawn, true))
				return Tools.GetMarkerColors(pawn, out _, out _) == false;

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
			if (rootSize < 11f)
				return GameFont.Medium;
			if (rootSize < 15f)
				return GameFont.Small;
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
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var firstInstruction = true;
			foreach (var instruction in instructions)
			{
				if (firstInstruction && instruction.LoadsConstant(0))
				{
					yield return new CodeInstruction(OpCodes.Call, Refs.p_CameraDriver);
					yield return new CodeInstruction(OpCodes.Call, Refs.p_RootSize);
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
		static readonly float size0 = sizes[0], size1 = sizes[1], size2 = sizes[2], size3 = sizes[3];

		public static bool Prefix(ref CameraZoomRange __result, float ___rootSize)
		{
			var lerped = Tools.LerpRootSize(___rootSize);
			if (lerped < size0)
				__result = CameraZoomRange.Closest;
			else if (lerped < size1)
				__result = CameraZoomRange.Close;
			else if (lerped < size2)
				__result = CameraZoomRange.Middle;
			else if (lerped < size3)
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

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ret)
					instruction.opcode = OpCodes.Ldarg_0;
				yield return instruction;
			}
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
		static readonly MethodInfo m_Main_LerpRootSize = SymbolExtensions.GetMethodInfo(() => Tools.LerpRootSize(default));

		public static IEnumerable<CodeInstruction> Transpiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
		{
			var v_lerpedRootSize = generator.DeclareLocal(typeof(float));

			// store lerped rootSize in a new local var
			//
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Call, Refs.p_RootSize);
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
					if (instruction.Calls(Refs.p_RootSize))
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

		public static bool Prepare() => ModLister.HasActiveModWithName("Save Our Ship 2");

		static FieldInfo PlanetMaterialField(string typeName)
		{
			var type = AccessTools.TypeByName($"SaveOurShip2.{typeName}");
			if (type == null)
				return null;
			return AccessTools.Field(type, "PlanetMaterial");
		}

		static void FixSoSMaterial()
		{
			done = true;
			var fPlanetMaterial = PlanetMaterialField("RenderPlanetBehindMap") ?? PlanetMaterialField("ResourceBank");
			if (fPlanetMaterial == null)
				return;
			var mat = fPlanetMaterial.GetValue(null) as Material;
			if (mat == null)
				return;
			mat.mainTextureOffset = new Vector2(0.3f, 0.3f);
			mat.mainTextureScale = new Vector2(0.4f, 0.4f);
		}

		public static void Postfix(Map __instance)
		{
			if (done)
				return;
			if (WorldRendererUtility.WorldRenderedNow)
				return;
			if (Find.CurrentMap != __instance)
				return;
			FixSoSMaterial();
		}
	}

	[HarmonyPatch(typeof(KeyBindingDef))]
	[HarmonyPatch(nameof(KeyBindingDef.KeyDownEvent))]
	[HarmonyPatch(MethodType.Getter)]
	static class KeyBindingDef_KeyDownEvent_Patch
	{
		static bool wasDown = false;
		static bool downInsideTick = false;

		public static void CleanupAtEndOfFrame()
		{
			if (Current.ProgramState != ProgramState.Playing)
				return;

			if (Event.current.type == EventType.KeyUp && KeyBindingDefOf.TogglePause.IsDown == false)
				wasDown = false;

			downInsideTick = false;
		}

		public static bool Prefix(KeyBindingDef __instance, ref bool __result)
		{
			if (__instance != KeyBindingDefOf.TogglePause)
				return true;

			if (__instance.IsDown && wasDown == false)
			{
				wasDown = true;
				downInsideTick = true;
			}

			__result = downInsideTick;
			return false;
		}
	}

	[HarmonyPatch(typeof(Root))]
	[HarmonyPatch(nameof(Root.OnGUI))]
	static class Root_OnGUI_Patch
	{
		public static void Postfix() => KeyBindingDef_KeyDownEvent_Patch.CleanupAtEndOfFrame();
	}

	[HarmonyPatch(typeof(Game))]
	[HarmonyPatch(nameof(Game.UpdatePlay))]
	static class Game_UpdatePlay_Patch
	{
		static DateTime lastChange = DateTime.MinValue;
		static bool eventFired = false;

		public static void Postfix()
		{
			if (Tools.HasSnapback && Current.gameInt.tickManager.Paused == false)
				Tools.RestoreSnapback();

			if (KeyBindingDefOf.TogglePause.IsDown && Current.gameInt.tickManager.Paused)
			{
				var now = DateTime.Now;
				if (lastChange == DateTime.MinValue)
					lastChange = now;
				else if (eventFired == false && now.Subtract(lastChange).TotalSeconds > 1)
				{
					Tools.CreateSnapback();
					eventFired = true;
				}
			}
			else
			{
				lastChange = DateTime.MinValue;
				eventFired = false;
			}
		}
	}

	[HarmonyPatch(typeof(TickManager))]
	[HarmonyPatch(nameof(TickManager.TogglePaused))]
	static class TickManager_TogglePaused_Patch
	{
		public static void Postfix(TickManager __instance)
		{
			if (Tools.HasSnapback && __instance.Paused == false)
				Tools.RestoreSnapback();
		}
	}

	[HarmonyPatch(typeof(MainTabWindow_Menu))]
	[HarmonyPatch(nameof(MainTabWindow_Menu.PreOpen))]
	static class MainTabWindow_Menu_PreOpen_Patch
	{
		public static void Postfix() => Tools.ResetSnapback();
	}

	[HarmonyPatch(typeof(UIRoot_Play))]
	[HarmonyPatch(nameof(UIRoot_Play.UIRootOnGUI))]
	static class UIRoot_Play_UIRootOnGUI_Patch
	{
		public static void Postfix()
		{
			KeyBindingDef_KeyDownEvent_Patch.CleanupAtEndOfFrame();

			if (Tools.HasSnapback == false)
				return;

			var rect = new Rect(0, 0, UI.screenWidth, UI.screenHeight);
			var color = GUI.color;
			GUI.color = new Color(0, 0, 0, 0.5f);
			Widgets.DrawBox(rect, 20);
			GUI.color = color;
		}
	}
}