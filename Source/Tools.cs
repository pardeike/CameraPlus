using Harmony;
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

		public static bool ReplacePawnWithDot(Pawn pawn)
		{
			if (CameraPlusMain.Settings.hideNamesWhenZoomedOut == false)
				return false;

			if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
				return false;

			var pos = pawn.DrawPos;
			var v1 = UI.MouseCell().ToVector3().MapToUIPosition();
			var v2 = pos.MapToUIPosition();
			return Vector2.Distance(v1, v2) > 28f;
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
				innerColor = GetMainColor(pawn) ?? innerColor;
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

		public static float LerpRootSize(float x)
		{
			var n = CameraPlusMain.Settings.exponentiality;
			if (n == 0)
				return GenMath.LerpDouble(minRootInput, maxRootInput, minRootResult, maxRootResult, x);

			var factor = (maxRootResult - minRootResult) / Math.Pow(maxRootInput - minRootInput, 2 * n);
			var y = minRootResult + Math.Pow(x - minRootInput, 2 * n) * factor;
			return (float)y;
		}

		public static float GetDollyRateKeys(float orthSize)
		{
			var zoomedIn = orthSize * CameraPlusMain.Settings.zoomedInDollyPercent * 4;
			var zoomedOut = orthSize * CameraPlusMain.Settings.zoomedOutDollyPercent;
			return GenMath.LerpDouble(minRootResult, maxRootResult, zoomedIn, zoomedOut, orthSize);
		}

		public static float GetDollyRateMouse(float orthSize)
		{
			var zoomedIn = 1f * CameraPlusMain.Settings.zoomedInDollyPercent;
			var zoomedOut = 10f * CameraPlusMain.Settings.zoomedOutDollyPercent;
			return GenMath.LerpDouble(minRootResult, maxRootResult, zoomedIn, zoomedOut, orthSize);
		}

		public static float GetDollySpeedDecay(float orthSize)
		{
			var minVal = 1f - CameraPlusMain.Settings.zoomedInDollyFrictionPercent;
			var maxVal = 1f - CameraPlusMain.Settings.zoomedOutDollyFrictionPercent;
			return GenMath.LerpDouble(minRootResult, maxRootResult, minVal, maxVal, orthSize);
		}

		public static string ToLabel(KeyCode code)
		{
			switch (code)
			{
				// cannot be more optimized because the enum has multiple equal values
				//
				case KeyCode.LeftShift:
					return "KeyLeftShift".Translate();
				case KeyCode.LeftAlt:
					return "KeyLeftAlt".Translate();
				case KeyCode.LeftControl:
					return "KeyLeftControl".Translate();
				case KeyCode.LeftCommand:
					return "KeyLeftCommand".Translate();
				case KeyCode.LeftWindows:
					return "KeyLeftWindows".Translate();
				case KeyCode.RightShift:
					return "KeyRightShift".Translate();
				case KeyCode.RightAlt:
					return "KeyRightAlt".Translate();
				case KeyCode.RightControl:
					return "KeyRightControl".Translate();
				case KeyCode.RightCommand:
					return "KeyRightCommand".Translate();
				case KeyCode.RightWindows:
					return "KeyRightWindows".Translate();
				default:
					return code.ToStringReadable();
			}
		}

		public static void KeySettingsButton(Rect rect, bool allKeys, KeyCode setting, Action<KeyCode> action)
		{
			List<KeyCode> AllWithNoneFirst()
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
			if (Event.current.type == EventType.repaint)
				return;

			var settings = CameraPlusMain.Settings;
			var m1 = settings.cameraSettingsMod1;
			var m2 = settings.cameraSettingsMod2;
			if (m1 == KeyCode.None && m2 == KeyCode.None)
				return;

			if (m1 == KeyCode.None || Input.GetKey(m1))
				if (m2 == KeyCode.None || Input.GetKey(m2))
				{
					if (Input.GetKey(settings.cameraSettingsKey))
					{
						var stack = Find.WindowStack;
						if (stack.IsOpen<Dialog_ModSettings>() == false)
						{
							var dialog = new Dialog_ModSettings();
							var me = LoadedModManager.GetMod<CameraPlusMain>();
							Refs.selMod(dialog) = me;
							stack.Add(dialog);
						}
						Event.current.Use();
						return;
					}

					var isSave = Input.GetKey(settings.cameraSettingsOption);
					for (var i = 1; i <= 9; i++)
						if (Input.GetKey("" + i))
						{
							var map = Find.CurrentMap;
							var cameraDriver = Find.CameraDriver;
							var savedViews = map.GetComponent<SavedViews>();
							if (isSave)
							{
								savedViews.views[i - 1] = new RememberedCameraPos(map)
								{
									rootPos = Refs.rootPos(cameraDriver),
									rootSize = Refs.rootSize(cameraDriver)
								};
							}
							else
							{
								var view = savedViews.views[i - 1];
								if (view != null)
									Find.CameraDriver.SetRootPosAndSize(view.rootPos, view.rootSize);
							}

							Event.current.Use();
							return;
						}
				}
		}
	}
}