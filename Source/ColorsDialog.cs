using UnityEngine;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	public class ColorsDialog : Window
	{
		const float colorFieldHeight = 40f;
		static readonly Color borderColor = Color.white.ToTransparent(0.5f);

		public ColorsDialog()
		{
			doCloseButton = true;
		}

		public override Vector2 InitialSize => new(640, 360);

		static void ColorField(Listing_Standard list, string title, ColorHolder colorHolder)
		{
			list.Gap(16f);
			list.Label(title);
			var rect = list.GetRect(colorFieldHeight);
			Widgets.DrawBoxSolidWithOutline(rect, colorHolder.color, borderColor);
			if (Mouse.IsOver(rect))
			{
				Widgets.DrawBoxSolidWithOutline(rect, Color.white, Color.white);
				if (Input.GetMouseButton(0))
				{
					var picker = new ColorPicker(title, colorHolder.color, colorHolder.update);
					Find.WindowStack.Add(picker);
				}
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard { ColumnWidth = (inRect.width - 34f) / 2f };
			list.Begin(inRect);

			ColorField(list, "Player fill", ColorHolder.With(Settings.playerColor, c => Settings.playerColor = c));
			ColorField(list, "Selected fill", ColorHolder.With(Settings.selectedColor, c => Settings.selectedColor = c));
			ColorField(list, "Uncontrollable", ColorHolder.With(Settings.uncontrollableColor, c => Settings.uncontrollableColor = c));

			list.NewColumn();
			list.curX += 17;

			list.TwoColumns(
				() => ColorField(list, "Colonist normal", ColorHolder.With(Settings.colonistColor[0], c => Settings.colonistColor[0] = c)),
				() => ColorField(list, "Colonist selected", ColorHolder.With(Settings.colonistColor[1], c => Settings.colonistColor[1] = c))
			);

			list.TwoColumns(
				() => ColorField(list, "Downed normal", ColorHolder.With(Settings.downedColor[0], c => Settings.downedColor[0] = c)),
				() => ColorField(list, "Downed selected", ColorHolder.With(Settings.downedColor[1], c => Settings.downedColor[1] = c))
			);

			list.TwoColumns(
				() => ColorField(list, "Drafted normal", ColorHolder.With(Settings.draftedColor[0], c => Settings.draftedColor[0] = c)),
				() => ColorField(list, "Drafted selected", ColorHolder.With(Settings.draftedColor[1], c => Settings.draftedColor[1] = c))
			);

			list.End();
		}
	}
}