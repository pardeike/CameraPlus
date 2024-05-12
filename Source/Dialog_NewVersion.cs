using RimWorld;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class Dialog_NewVersion : Window
	{
		readonly string message = "CameraPlus3".Translate();

		public override Vector2 InitialSize => new(320f, 240f);

		public Dialog_NewVersion() : base()
		{
		}

		public override void DoWindowContents(Rect inRect)
		{
			Widgets.Label(new Rect(0f, inRect.y, inRect.width, Text.CalcHeight(message, inRect.width)), message);

			var num6 = inRect.width / 2f;
			var num7 = num6 - 10f;

			GUI.color = Color.white;
			if (Widgets.ButtonText(new Rect(0f, inRect.height - 35f, num7, 35f), "OK".Translate()))
				Close(true);

			if (Widgets.ButtonText(new Rect(num6, inRect.height - 35f, num7, 35f), "ModSettings".Translate()))
			{
				Close(true);
				LongEventHandler.ExecuteWhenFinished(() =>
				{
					var me = LoadedModManager.GetMod<CameraPlusMain>();
					var dialog = new Dialog_ModSettings(me);
					Find.WindowStack.Add(dialog);
				});
			}
		}

		public override void OnCancelKeyPressed()
		{
			Close(true);
			base.OnCancelKeyPressed();
		}

		public override void OnAcceptKeyPressed()
		{
			Close(true);
			Event.current.Use();
			base.OnAcceptKeyPressed();
		}
	}
}