using UnityEngine;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	public class Dialog_Colors : Window
	{
		const float colorFieldHeight = 40f;
		static readonly Color borderColor = Color.white.ToTransparent(0.5f);

		public Dialog_Colors()
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
					var picker = new Dialog_ColorPicker(title, colorHolder.color, colorHolder.update);
					Find.WindowStack.Add(picker);
				}
			}
		}

		/* three main groups: player, other, custom
		 * in two states: normal and selected (player: +drafted +downed +mental)
		 * in two versions: inner and outer
		 * 
		 * Player
		 * - normal    [ outer ] / [ inner ]
		 * - drafted   [ outer ] / [ inner ]
		 * - downed    [ outer ] / [ inner ]
		 * - mental    [ outer ] / [ inner ]
		 * - selected  [ outer ] / [ inner ]
		 * 
		 * Other
		 * - normal    [outer+x] / [inner+x]
		 * - selected  [outer+x] / [inner+x]
		 * 
		 * Custom
		 * - normal    [outer+x] / [inner+x]
		 * - selected  [outer+x] / [inner+x]
		 */

		public override void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard { ColumnWidth = (inRect.width - 34f) / 2f };
			list.Begin(inRect);

			ColorField(list, "Player fill", ColorHolder.With(Settings.playerInnerColors, c => Settings.playerInnerColors = c));
			ColorField(list, "Selected fill", ColorHolder.With(Settings.defaultSelectedOuterColor, c => Settings.defaultSelectedOuterColor = c));
			ColorField(list, "Uncontrollable", ColorHolder.With(Settings.playerMentalInnerColors, c => Settings.playerMentalInnerColors = c));

			list.NewColumn();
			list.curX += 17;

			list.TwoColumns(
				() => ColorField(list, "Colonist normal", ColorHolder.With(Settings.playerNormalOuterColors[0], c => Settings.playerNormalOuterColors[0] = c)),
				() => ColorField(list, "Colonist selected", ColorHolder.With(Settings.playerNormalOuterColors[1], c => Settings.playerNormalOuterColors[1] = c))
			);

			list.TwoColumns(
				() => ColorField(list, "Downed normal", ColorHolder.With(Settings.playerDownedOuterColors[0], c => Settings.playerDownedOuterColors[0] = c)),
				() => ColorField(list, "Downed selected", ColorHolder.With(Settings.playerDownedOuterColors[1], c => Settings.playerDownedOuterColors[1] = c))
			);

			list.TwoColumns(
				() => ColorField(list, "Drafted normal", ColorHolder.With(Settings.playerDraftedOuterColors[0], c => Settings.playerDraftedOuterColors[0] = c)),
				() => ColorField(list, "Drafted selected", ColorHolder.With(Settings.playerDraftedOuterColors[1], c => Settings.playerDraftedOuterColors[1] = c))
			);

			list.End();
		}
	}
}