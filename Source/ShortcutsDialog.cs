using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class ShortcutsDialog : Window
	{
		const float buttonWidth = 80f;
		const float buttonSpace = 4f;

		public ShortcutsDialog()
		{
			doCloseButton = true;
		}

		public override Vector2 InitialSize => new(480, 320);

		public override void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard();
			list.Begin(inRect);

			_ = list.Label("HotKeys".Translate());
			list.Gap(6f);

			var rect = list.GetRect(28f);
			GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
			Widgets.Label(rect, "SettingsKey".Translate());
			GenUI.ResetLabelAlign();
			rect.xMin = rect.xMax - buttonWidth;
			Tools.KeySettingsButton(rect, true, CameraPlusMain.Settings.cameraSettingsKey, KeyCode.Tab, code => CameraPlusMain.Settings.cameraSettingsKey = code);
			rect.xMin -= buttonWidth + buttonSpace;
			rect.xMax = rect.xMin + buttonWidth;
			Tools.KeySettingsButton(rect, false, CameraPlusMain.Settings.cameraSettingsMod[1], KeyCode.None, code => CameraPlusMain.Settings.cameraSettingsMod[1] = code);
			rect.xMin -= buttonWidth + buttonSpace;
			rect.xMax = rect.xMin + buttonWidth;
			Tools.KeySettingsButton(rect, false, CameraPlusMain.Settings.cameraSettingsMod[0], KeyCode.None, code => CameraPlusMain.Settings.cameraSettingsMod[0] = code);
			list.Gap(6f);

			rect = list.GetRect(28f);
			GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
			Widgets.Label(rect, "LoadModifier".Translate());
			GenUI.ResetLabelAlign();
			rect.xMin = rect.xMax - buttonWidth;
			GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
			Widgets.Label(rect, "1 - 9");
			GenUI.ResetLabelAlign();
			rect.xMin -= buttonWidth + buttonSpace;
			rect.xMax = rect.xMin + buttonWidth;
			Tools.KeySettingsButton(rect, false, CameraPlusMain.Settings.cameraSettingsLoad[1], KeyCode.None, code => CameraPlusMain.Settings.cameraSettingsLoad[1] = code);
			rect.xMin -= buttonWidth + buttonSpace;
			rect.xMax = rect.xMin + buttonWidth;
			Tools.KeySettingsButton(rect, false, CameraPlusMain.Settings.cameraSettingsLoad[0], KeyCode.LeftShift, code => CameraPlusMain.Settings.cameraSettingsLoad[0] = code);
			list.Gap(6f);

			rect = list.GetRect(28f);
			GenUI.SetLabelAlign(TextAnchor.MiddleLeft);
			Widgets.Label(rect, "SaveModifier".Translate());
			GenUI.ResetLabelAlign();
			rect.xMin = rect.xMax - buttonWidth;
			GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
			Widgets.Label(rect, "1 - 9");
			GenUI.ResetLabelAlign();
			rect.xMin -= buttonWidth + buttonSpace;
			rect.xMax = rect.xMin + buttonWidth;
			Tools.KeySettingsButton(rect, false, CameraPlusMain.Settings.cameraSettingsSave[1], KeyCode.None, code => CameraPlusMain.Settings.cameraSettingsSave[1] = code);
			rect.xMin -= buttonWidth + buttonSpace;
			rect.xMax = rect.xMin + buttonWidth;
			Tools.KeySettingsButton(rect, false, CameraPlusMain.Settings.cameraSettingsSave[0], KeyCode.LeftAlt, code => CameraPlusMain.Settings.cameraSettingsSave[0] = code);

			list.End();
		}
	}
}