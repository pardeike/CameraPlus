using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	class CameraDelegates
	{
		public Func<Pawn, Color[]> GetCameraColors = null;
		public Func<Pawn, Texture2D[]> GetCameraMarkers = null;

		static MethodInfo GetMethod(Pawn pawn, string name)
		{
			return pawn.GetType().Assembly
				.GetType("CameraPlusSupport.Methods", false)?
				.GetMethod(name, AccessTools.all);
		}

		public CameraDelegates(Pawn pawn)
		{
			var m_GetCameraColors = GetMethod(pawn, "GetCameraPlusColors");
			if (m_GetCameraColors != null)
			{
				var funcType = Expression.GetFuncType(new[] { typeof(Pawn), typeof(Color[]) });
				GetCameraColors = (Func<Pawn, Color[]>)Delegate.CreateDelegate(funcType, m_GetCameraColors);
			}

			var m_GetCameraTextures = GetMethod(pawn, "GetCameraPlusMarkers");
			if (m_GetCameraTextures != null)
			{
				var funcType = Expression.GetFuncType(new[] { typeof(Pawn), typeof(Texture2D[]) });
				GetCameraMarkers = (Func<Pawn, Texture2D[]>)Delegate.CreateDelegate(funcType, m_GetCameraTextures);
			}
		}
	}

	static class Extensions
	{
		public static void Slider(this Listing_Standard list, ref int value, int min, int max, Func<string> label)
		{
			float f = value;
			var h = HorizontalSlider(list.GetRect(22f), ref f, min, max, label == null ? null : label(), 1f);
			value = (int)f;
			list.Gap(h);
		}

		public static void Slider(this Listing_Standard list, ref float value, float min, float max, Func<string> label, float roundTo = -1f)
		{
			var rect = list.GetRect(22f);
			var h = HorizontalSlider(rect, ref value, min, max, label == null ? null : label(), roundTo);
			list.Gap(h);
		}

		public static float HorizontalSlider(Rect rect, ref float value, float leftValue, float rightValue, string label = null, float roundTo = -1f)
		{
			if (label != null)
			{
				var anchor = Text.Anchor;
				var font = Text.Font;
				Text.Font = GameFont.Tiny;
				Text.Anchor = TextAnchor.UpperLeft;
				Widgets.Label(rect, label);
				Text.Anchor = anchor;
				Text.Font = font;
				rect.y += 18f;
			}
			value = GUI.HorizontalSlider(rect, value, leftValue, rightValue);
			if (roundTo > 0f)
				value = Mathf.RoundToInt(value / roundTo) * roundTo;
			return 4f + label != null ? 18f : 0f;
		}
	}

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
				var material = graphic.MatEast;
				if (material == null) material = graphic.MatSingle;
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

		// shameless copy of vanilla
		public static bool PawnHasNoLabel(Pawn pawn)
		{
			if (!pawn.Spawned || pawn.Map.fogGrid.IsFogged(pawn.Position))
				return true;
			if (!pawn.RaceProps.Humanlike)
			{
				if (CameraPlusMain.Settings.includeNotTamedAnimals)
					return false;

				var animalNameMode = Prefs.AnimalNameMode;
				if (animalNameMode == AnimalNameDisplayMode.None)
					return true;
				if (animalNameMode != AnimalNameDisplayMode.TameAll)
				{
					if (animalNameMode == AnimalNameDisplayMode.TameNamed)
					{
						if (pawn.Name == null || pawn.Name.Numerical)
							return true;
					}
				}
				else if (pawn.Name == null)
					return true;
			}

			return false;
		}

		public static float MouseDistanceSquared(Vector3 pos, bool mapCoordinates)
		{
			var mouse = UI.MouseMapPosition();
			if (mapCoordinates)
			{
				var dx1 = (mouse.x - pos.x);
				var dz = (mouse.z - pos.z);
				return dx1 * dx1 + dz * dz;
			}

			mouse = UI.MapToUIPosition(mouse);
			var len = UI.CurUICellSize();
			mouse.y += len / 2;
			var dx2 = (mouse.x - pos.x);
			var dy = (mouse.y - pos.y);
			var delta = dx2 * dx2 + dy * dy;
			return delta / len / len;
		}

		public static bool ShouldShowBody(Pawn pawn)
		{
			if (CameraPlusMain.Settings.hideNamesWhenZoomedOut == false || MouseDistanceSquared(pawn.DrawPos, true) <= 2.25f)
				return true;

			return (UI.CurUICellSize() > CameraPlusMain.Settings.dotSize);
		}

		public static void ShouldShowLabel(Vector3 location, bool isPawn, out bool showLabel, out bool showDot)
		{
			showLabel = true;
			showDot = false;

			if (CameraPlusMain.Settings.hideNamesWhenZoomedOut == false)
				return;

			if (MouseDistanceSquared(location, isPawn) <= 2.25f && CameraPlusMain.Settings.mouseOverShowsLabels)
				return;

			var len = UI.CurUICellSize();

			if (isPawn && len <= CameraPlusMain.Settings.dotSize)
			{
				showLabel = false;
				showDot = true;
				return;
			}

			var lower = isPawn ? CameraPlusMain.Settings.hidePawnLabelBelow : CameraPlusMain.Settings.hideThingLabelBelow;
			if (len <= lower)
			{
				showLabel = false;
				showDot = false;
				return;
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

			var isAnimal = pawn.RaceProps.Animal;
			var showAnimals = CameraPlusMain.Settings.customNameStyle != LabelStyle.HideAnimals;
			if (isAnimal && showAnimals == false)
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
			if (inFrom == inTo) return (outFrom + outTo) / 2;
			return GenMath.LerpDouble(inFrom, inTo, outFrom, outTo, x);
		}

		public static float LerpRootSize(float x)
		{
			var n = CameraPlusMain.Settings.exponentiality;
			if (n == 0)
				return LerpDoubleSafe(minRootInput, maxRootInput, minRootResult, maxRootResult, x);

			if (minRootResult == maxRootResult)
				return minRootResult;
			var factor = (maxRootResult - minRootResult) / Math.Pow(maxRootInput - minRootInput, 2 * n);
			var y = minRootResult + Math.Pow(x - minRootInput, 2 * n) * factor;
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
			// TODO: 0.15f comes from the old zoomedInDollyFrictionPercent/zoomedOutDollyFrictionPercent
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

		public static void KeySettingsButton(Rect rect, bool allKeys, KeyCode setting, Action<KeyCode> action)
		{
			static List<KeyCode> AllWithNoneFirst()
			{
				return Enum.GetValues(typeof(KeyCode))
					.Cast<KeyCode>()
					.Where(code => code != KeyCode.None && code < KeyCode.JoystickButton0)
					.ToList();
			}

			if (Widgets.ButtonText(rect, setting == KeyCode.None ? "" : ToLabel(setting)))
			{
				var keys = allKeys ? AllWithNoneFirst() : new List<KeyCode>()
				{
					KeyCode.LeftShift, KeyCode.LeftAlt, KeyCode.LeftControl, KeyCode.LeftCommand, KeyCode.LeftWindows,
					KeyCode.RightShift, KeyCode.RightAlt, KeyCode.RightControl, KeyCode.RightCommand, KeyCode.RightWindows,
				};
				keys.Insert(0, KeyCode.None);
				var choices = keys
					.Select(code => new FloatMenuOption(ToLabel(code), delegate () { action(code); }, MenuOptionPriority.Default, null, null, 0f, null, null))
					.ToList();
				Find.WindowStack.Add(new FloatMenu(choices));
			}
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
							var dialog = new Dialog_ModSettings();
							var me = LoadedModManager.GetMod<CameraPlusMain>();
							dialog.selMod = me;
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

			var map = Find.CurrentMap;
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
							Find.CameraDriver.SetRootPosAndSize(view.rootPos, view.rootSize);
						Event.current.Use();
					}

			m1 = settings.cameraSettingsSave[0];
			m2 = settings.cameraSettingsSave[1];
			if (m1 != KeyCode.None || m2 != KeyCode.None)
				if (m1 == KeyCode.None || Input.GetKey(m1))
					if (m2 == KeyCode.None || Input.GetKey(m2))
					{
						var cameraDriver = Find.CameraDriver;
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
