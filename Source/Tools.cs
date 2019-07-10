using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class SavedViews : MapComponent
	{
		public RememberedCameraPos[] views = new RememberedCameraPos[9];

		public SavedViews(Map map) : base(map)
		{
		}

		public override void ExposeData()
		{
			for (var i = 0; i < 9; i++)
				Scribe_Deep.Look(ref views[i], "view" + (i + 1), new object[] { map });
		}
	}

	class Tools : CameraPlusSettings
	{
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