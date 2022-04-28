using System;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class Dialog_AskForKey : Window
	{
		public Vector2 windowSize = new Vector2(400f, 200f);
		public Action<KeyCode> callback;

		public override Vector2 InitialSize => new Vector2(400f, 200f);
		public override float Margin => 0f;

		public Dialog_AskForKey(Action<KeyCode> callback)
		{
			this.callback = callback;
			closeOnAccept = false;
			closeOnCancel = false;
			forcePause = true;
			onlyOneOfTypeAllowed = true;
			absorbInputAroundWindow = true;
		}

		public override void DoWindowContents(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(inRect, "PressAnyKeyOrEsc".Translate());
			Text.Anchor = TextAnchor.UpperLeft;
			if (Event.current.isKey && Event.current.type == EventType.KeyDown && Event.current.keyCode != KeyCode.None)
			{
				if (Event.current.keyCode != KeyCode.Escape)
					callback(Event.current.keyCode);
				Close(true);
				Event.current.Use();
			}
		}
	}
}
