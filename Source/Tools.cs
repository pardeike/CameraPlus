using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CameraPlus
{
	public enum LabelMode
	{
		hide,
		show,
		dot
	}

	[StaticConstructorOnStartup]
	class Tools : CameraPlusSettings
	{
		static readonly Texture2D innerColonistTexture = ContentFinder<Texture2D>.Get("InnerColonistMarker", true);
		static readonly Texture2D outerColonistTexture = ContentFinder<Texture2D>.Get("OuterColonistMarker", true);
		static readonly Texture2D innerAnimalTexture = ContentFinder<Texture2D>.Get("InnerAnimalMarker", true);
		static readonly Texture2D outerAnimalTexture = ContentFinder<Texture2D>.Get("OuterAnimalMarker", true);

		static readonly Texture2D downedTexture = ContentFinder<Texture2D>.Get("DownedMarker", true);
		static readonly Texture2D draftedTexture = ContentFinder<Texture2D>.Get("DraftedMarker", true);
		static readonly Color downedColor = new Color(0.9f, 0f, 0f);
		static readonly Color draftedColor = new Color(0f, 0.5f, 0f);

		public static bool ShouldShowDot(Pawn pawn)
		{
			if (CameraPlusMain.Settings.hideNamesWhenZoomedOut == false)
				return false;

			if (CameraPlusMain.Settings.customNameStyle == LabelStyle.HideAnimals && pawn.RaceProps.Animal)
				return false;

			if (CameraPlusMain.Settings.mouseOverShowsLabels && MouseDistanceSquared(pawn.DrawPos, true) <= 2.25f)
				return false;

			var len = FastUI.CurUICellSize;
			var isSmall = len <= CameraPlusMain.Settings.dotSize;
			var tamedAnimal = pawn.RaceProps.Animal && pawn.Name != null;
			return isSmall && (CameraPlusMain.Settings.includeNotTamedAnimals || pawn.RaceProps.Animal == false || tamedAnimal);
		}

		public static bool ShouldShowLabel(Thing thing, Vector2 screenPos = default)
		{
			if (CameraPlusMain.Settings.hideNamesWhenZoomedOut == false)
				return true;

			var isPawn = thing is Pawn;

			if (CameraPlusMain.Settings.mouseOverShowsLabels && MouseDistanceSquared(thing?.DrawPos ?? screenPos, isPawn) <= 2.25f)
				return true;

			var len = FastUI.CurUICellSize;

			var lower = isPawn ? CameraPlusMain.Settings.hidePawnLabelBelow : CameraPlusMain.Settings.hideThingLabelBelow;
			if (len <= lower)
				return false;

			if (isPawn && CameraPlusMain.Settings.customNameStyle == LabelStyle.HideAnimals && (thing as Pawn).RaceProps.Animal)
				return true;

			if (isPawn && len <= CameraPlusMain.Settings.dotSize)
				return false;

			return true;
		}

		public static void DrawDot(Pawn pawn, Color innerColor, Color outerColor)
		{
			_ = GetMarkerTextures(pawn, out var innerTexture, out var outerTexture);

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
		}

		static readonly Dictionary<string, Color> cachedMainColors = new Dictionary<string, Color>();
		public static Color? GetMainColor(Pawn pawn)
		{
			const int resizedTo = 8;
			const int maxColorAmount = 1;
			const float colorLimiterPercentage = 85f;
			const int uniteColorsTolerance = 5;
			const float minimiumColorPercentage = 10f;

			var graphic = pawn.Drawer.renderer.graphics?.nakedGraphic;
			if (graphic == null)
				return null;

			var key = pawn.GetType().FullName + ":" + graphic.path;
			if (cachedMainColors.TryGetValue(key, out var color) == false)
			{
				var material = graphic.MatEast ?? graphic.MatSingle;
				if (material == null)
				{
					cachedMainColors[key] = Color.gray;
					return null;
				}

				var texture = material.mainTexture;
				if (texture == null)
				{
					cachedMainColors[key] = Color.gray;
					return null;
				}

				var width = texture.width;
				var height = texture.width;
				var outputTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
				var buffer = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
				Graphics.Blit(texture, buffer, material, 0);
				RenderTexture.active = buffer;
				outputTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);

				var color32s = ProminentColor.GetColors32FromImage(outputTexture, resizedTo, maxColorAmount, colorLimiterPercentage, uniteColorsTolerance, minimiumColorPercentage);
				if (color32s == null || color32s.Count == 0)
					color = Color.gray;
				else
					color = new Color(color32s[0].r / 255f, color32s[0].g / 255f, color32s[0].b / 255f);

				cachedMainColors[key] = color;
			}
			return color;
		}

		public static float MouseDistanceSquared(Vector3 pos, bool mapCoordinates)
		{
			if (mapCoordinates)
			{
				var mouse = FastUI.MouseMapPosition;
				var dx1 = (mouse.x - pos.x);
				var dz = (mouse.z - pos.z);
				return dx1 * dx1 + dz * dz;
			}
			else
			{
				var mouse = FastUI.MousePositionOnUIInverted;
				var len = FastUI.CurUICellSize;
				mouse.y += len / 2;
				var dx2 = (mouse.x - pos.x);
				var dy = (mouse.y - pos.y);
				var delta = dx2 * dx2 + dy * dy;
				return delta / len / len;
			}
		}

		static readonly Dictionary<Type, CameraDelegates> cachedCameraDelegates = new Dictionary<Type, CameraDelegates>();
		public static CameraDelegates GetCachedCameraDelegate(Pawn pawn)
		{
			var type = pawn.GetType();
			if (cachedCameraDelegates.TryGetValue(type, out var result) == false)
			{
				result = new CameraDelegates(pawn);
				cachedCameraDelegates[type] = result;
			}
			return result;
		}

		public static bool CorrectLabelRendering(Pawn pawn)
		{
			// we fake "show all" so we need to skip if original could would not render labels
			return ReversePatches.PerformsDrawPawnGUIOverlay(pawn.Drawer.ui) == false;
		}

		// returning true will prefer markers over labels
		static readonly Color dangerousAnimalColor = new Color(0.62f, 0f, 0.05f);
		public static bool GetMarkerColors(Pawn pawn, out Color innerColor, out Color outerColor)
		{
			var cameraDelegate = GetCachedCameraDelegate(pawn);
			if (cameraDelegate.GetCameraColors != null)
			{
				var colors = cameraDelegate.GetCameraColors(pawn);
				if (colors == null || colors.Length != 2)
				{
					innerColor = default;
					outerColor = default;
					return false;
				}
				innerColor = colors[0];
				outerColor = colors[1];
				return true;
			}

			var isAnimal = pawn.RaceProps.Animal && pawn.Name != null;
			var hideAnimalMarkers = CameraPlusMain.Settings.customNameStyle == LabelStyle.HideAnimals;
			if (isAnimal && hideAnimalMarkers)
			{
				innerColor = default;
				outerColor = default;
				return false;
			}

			innerColor = PawnNameColorUtility.PawnNameColorOf(pawn);
			if (pawn.RaceProps.Animal)
			{
				var stateDef = pawn.mindState.mentalStateHandler.CurStateDef;
				var isDangerous = stateDef == MentalStateDefOf.ManhunterPermanent || stateDef == MentalStateDefOf.Manhunter;
				innerColor = isDangerous ? dangerousAnimalColor : (GetMainColor(pawn) ?? innerColor);
			}
			outerColor = Find.Selector.IsSelected(pawn) ? Color.black : Color.white;
			return true;
		}

		public static bool GetMarkerTextures(Pawn pawn, out Texture2D innerTexture, out Texture2D outerTexture)
		{
			var cameraDelegate = GetCachedCameraDelegate(pawn);
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

			var isAnimal = pawn.RaceProps.Animal;
			var customAnimalStyle = CameraPlusMain.Settings.customNameStyle == LabelStyle.AnimalsDifferent;
			innerTexture = isAnimal && customAnimalStyle ? innerAnimalTexture : innerColonistTexture;
			outerTexture = isAnimal && customAnimalStyle ? outerAnimalTexture : outerColonistTexture;
			return true;
		}

		public static float LerpDoubleSafe(float inFrom, float inTo, float outFrom, float outTo, float x)
		{
			if (inFrom == inTo)
				return (outFrom + outTo) / 2;
			return GenMath.LerpDouble(inFrom, inTo, outFrom, outTo, x);
		}

		public static float LerpRootSize(float x)
		{
			var n = CameraPlusMain.Settings.exponentiality;
			if (n == 0)
				return LerpDoubleSafe(minRootInput, maxRootInput, minRootResult, maxRootResult, x);

			if (minRootResult == maxRootResult)
				return minRootResult;
			var factor = (maxRootResult - minRootResult) / Mathf.Pow(maxRootInput - minRootInput, 2 * n);
			var y = minRootResult + Mathf.Pow(x - minRootInput, 2 * n) * factor;
			return (float)y;
		}

		public static float GetDollyRateKeys(float orthSize)
		{
			var f = GetScreenEdgeDollyFactor(orthSize);
			var zoomedIn = orthSize * CameraPlusMain.Settings.zoomedInDollyPercent * 4 / f;
			var zoomedOut = orthSize * CameraPlusMain.Settings.zoomedOutDollyPercent / f;
			return LerpDoubleSafe(minRootResult, maxRootResult, zoomedIn, zoomedOut, orthSize);
		}

		public static float GetScreenEdgeDollyFactor(float orthSize)
		{
			var zoomedIn = CameraPlusMain.Settings.zoomedInScreenEdgeDollyFactor * 30;
			var zoomedOut = CameraPlusMain.Settings.zoomedOutScreenEdgeDollyFactor * 30;
			return LerpDoubleSafe(minRootResult, maxRootResult, zoomedIn, zoomedOut, orthSize);
		}

		public static float GetDollyRateMouse(float orthSize)
		{
			var zoomedIn = 1f * CameraPlusMain.Settings.zoomedInDollyPercent;
			var zoomedOut = 10f * CameraPlusMain.Settings.zoomedOutDollyPercent;
			return LerpDoubleSafe(minRootResult, maxRootResult, zoomedIn, zoomedOut, orthSize);
		}

		public static float GetDollySpeedDecay(float orthSize)
		{
			// 0.15f comes from the old zoomedInDollyFrictionPercent/zoomedOutDollyFrictionPercent
			//
			var minVal = 1f - 0.15f;
			var maxVal = 1f - 0.15f;
			return LerpDoubleSafe(minRootResult, maxRootResult, minVal, maxVal, orthSize);
		}

		public static string ToLabel(KeyCode code)
		{
			return code switch
			{
				// cannot be more optimized because the enum has multiple equal values
				//
				KeyCode.None => "None".Translate(),
				KeyCode.LeftShift => "KeyLeftShift".Translate(),
				KeyCode.LeftAlt => "KeyLeftAlt".Translate(),
				KeyCode.LeftControl => "KeyLeftControl".Translate(),
				KeyCode.LeftCommand => "KeyLeftCommand".Translate(),
				KeyCode.LeftWindows => "KeyLeftWindows".Translate(),
				KeyCode.RightShift => "KeyRightShift".Translate(),
				KeyCode.RightAlt => "KeyRightAlt".Translate(),
				KeyCode.RightControl => "KeyRightControl".Translate(),
				KeyCode.RightCommand => "KeyRightCommand".Translate(),
				KeyCode.RightWindows => "KeyRightWindows".Translate(),
				_ => code.ToStringReadable(),
			};
		}

		public static void KeySettingsButton(Rect rect, bool allKeys, KeyCode setting, KeyCode defaultKey, Action<KeyCode> action)
		{
			if (allKeys)
			{
				TooltipHandler.TipRegionByKey(rect, "BindingButtonToolTip");
				if (Widgets.ButtonText(rect, setting == KeyCode.None ? "" : ToLabel(setting)))
				{
					if (Event.current.button == 0)
					{
						Find.WindowStack.Add(new Dialog_AskForKey(action));
						Event.current.Use();
						return;
					}
					if (Event.current.button == 1)
					{
						var list = new List<FloatMenuOption>
						{
							new FloatMenuOption("ResetBinding".Translate(), () => action(defaultKey), MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
							new FloatMenuOption("ClearBinding".Translate(), () => action(KeyCode.None), MenuOptionPriority.Default, null, null, 0f, null, null, true, 0)
						};
						Find.WindowStack.Add(new FloatMenu(list));
					}
				}
				return;
			}

			if (Widgets.ButtonText(rect, setting == KeyCode.None ? "" : ToLabel(setting)))
			{
				var keys = new List<KeyCode>()
				{
					KeyCode.None,
					KeyCode.LeftShift, KeyCode.LeftAlt, KeyCode.LeftControl, KeyCode.LeftCommand, KeyCode.LeftWindows,
					KeyCode.RightShift, KeyCode.RightAlt, KeyCode.RightControl, KeyCode.RightCommand, KeyCode.RightWindows,
				};
				var choices = keys
					.Select(code => new FloatMenuOption(ToLabel(code), delegate () { action(code); }, MenuOptionPriority.Default, null, null, 0f, null, null))
					.ToList();
				Find.WindowStack.Add(new FloatMenu(choices));
				Event.current.Use();
			}
		}

		static Vector3 snapbackRootPos;
		static float snapbackRootSize = 0;

		public static void CreateSnapback()
		{
			Defs.SnapBack.PlayOneShotOnCamera(null);
			var cameraDriver = Current.cameraDriverInt;
			snapbackRootPos = cameraDriver.rootPos;
			snapbackRootSize = cameraDriver.rootSize;
		}

		public static bool HasSnapback => snapbackRootSize != 0;

		public static void ResetSnapback()
		{
			snapbackRootPos = default;
			snapbackRootSize = default;
		}

		public static void RestoreSnapback()
		{
			var tm = Find.TickManager;
			var savedSpeed = tm.curTimeSpeed;

			IEnumerator ApplyRootPosAndSize()
			{
				yield return new WaitForSeconds(0.35f);
				Current.cameraDriverInt.SetRootPosAndSize(snapbackRootPos, snapbackRootSize);
				ResetSnapback();
				tm.curTimeSpeed = savedSpeed;
			}

			tm.curTimeSpeed = TimeSpeed.Paused;
			Defs.ApplySnap.PlayOneShotOnCamera(null);
			_ = Current.cameraDriverInt.StartCoroutine(ApplyRootPosAndSize());
		}

		public static void HandleHotkeys()
		{
			if (Event.current.type == EventType.Repaint)
				return;

			var settings = CameraPlusMain.Settings;
			KeyCode m1, m2;

			if (Input.GetKey(settings.cameraSettingsKey))
			{
				m1 = settings.cameraSettingsMod[0];
				m2 = settings.cameraSettingsMod[1];
				if (m1 == KeyCode.None && m2 == KeyCode.None)
					return;

				if (m1 == KeyCode.None || Input.GetKey(m1))
					if (m2 == KeyCode.None || Input.GetKey(m2))
					{
						var stack = Find.WindowStack;
						if (stack.IsOpen<Dialog_ModSettings>() == false)
						{
							var me = LoadedModManager.GetMod<CameraPlusMain>();
							var dialog = new Dialog_ModSettings(me);
							stack.Add(dialog);
						}
						Event.current.Use();
						return;
					}
			}

			var numKey = 0;
			for (var i = 1; i <= 9; i++)
				if (Input.GetKey("" + i))
				{
					numKey = i;
					break;
				}
			if (numKey == 0)
				return;

			var map = Current.gameInt.CurrentMap;
			if (map == null)
				return;

			var savedViews = map.GetComponent<SavedViews>();

			m1 = settings.cameraSettingsLoad[0];
			m2 = settings.cameraSettingsLoad[1];
			if (m1 != KeyCode.None || m2 != KeyCode.None)
				if (m1 == KeyCode.None || Input.GetKey(m1))
					if (m2 == KeyCode.None || Input.GetKey(m2))
					{
						var view = savedViews.views[numKey - 1];
						if (view != null)
							Current.cameraDriverInt.SetRootPosAndSize(view.rootPos, view.rootSize);
						Event.current.Use();
					}

			m1 = settings.cameraSettingsSave[0];
			m2 = settings.cameraSettingsSave[1];
			if (m1 != KeyCode.None || m2 != KeyCode.None)
				if (m1 == KeyCode.None || Input.GetKey(m1))
					if (m2 == KeyCode.None || Input.GetKey(m2))
					{
						var cameraDriver = Current.cameraDriverInt;
						savedViews.views[numKey - 1] = new RememberedCameraPos(map)
						{
							rootPos = cameraDriver.rootPos,
							rootSize = cameraDriver.rootSize
						};
						Event.current.Use();
					}
		}
	}
}
