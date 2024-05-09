using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	public class Dialog_Colors : Window
	{
		public override Vector2 InitialSize => new(720, 440);

		static readonly Vector2 colorFieldSize = new(120, 30);
		static readonly Color borderColor = Color.gray.ToTransparent(0.5f);
		static Color[] emptyColors = [];

		public Dialog_Colors()
		{
			doCloseButton = true;
		}

		static void CenteredLabel(Rect labelRect, TaggedString label)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.LabelFit(labelRect, label);
			Text.Anchor = TextAnchor.UpperLeft;
		}

		static void PrepareRow(Listing_Standard list, TaggedString label, out Rect cRect1, out Rect cRect2, out Rect cRect3, out Rect cRect4)
		{
			var rect = list.GetRect(colorFieldSize.y + 5);

			var labelRect = rect.LeftPartPixels(rect.width - 4 * (Dialog_ColorPicker.spacing + colorFieldSize.x));
			cRect1 = new Rect(labelRect.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, colorFieldSize.y);
			cRect2 = new Rect(cRect1.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, colorFieldSize.y);
			cRect3 = new Rect(cRect2.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, colorFieldSize.y);
			cRect4 = new Rect(cRect3.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, colorFieldSize.y);
			Widgets.Label(labelRect, label); // CenteredLabel(labelRect, label);
		}

		static void ColorButton(Rect rect, string title, bool canDelete, Color? color, Action<Color?> newColorCallback)
		{
			GUI.DrawTexture(rect, Assets.colorBackgroundPattern, ScaleMode.StretchToFill);
			if (color.HasValue)
				Widgets.DrawBoxSolidWithOutline(rect, color.Value, borderColor);
			var deleteRect = rect.RightPartPixels(rect.height).ExpandedBy(-4);
			if (canDelete && color.HasValue)
			{
				GUI.DrawTexture(deleteRect, Assets.deleteColorButton);
				GUI.color = Color.white;
				if (Widgets.ButtonInvisible(deleteRect))
					newColorCallback(null);
			}
			if (Widgets.ButtonInvisible(rect))
				Find.WindowStack.Add(new Dialog_ColorPicker(title, color, newColorCallback));
		}

		void ColorEditorRow(Listing_Standard list, TaggedString label, string outerName, string innerName)
		{
			PrepareRow(list, label, out var cRect1, out var cRect2, out var cRect3, out var cRect4);
			var trv = Traverse.Create(Settings);
			if (outerName != "")
			{
				var outers = trv.Field(outerName).GetValue<Array>();
				if (outers is OptionalColor[] optionals)
				{
					ColorButton(cRect1, $"{label} line", true, optionals[0]?.color, c => outers.SetValue(new OptionalColor(c), 0));
					ColorButton(cRect2, $"{label} line selected", true, optionals[1]?.color, c => outers.SetValue(new OptionalColor(c), 1));
				}
				if (outers is Color[] colors)
				{
					ColorButton(cRect1, $"{label} line", false, colors[0], c => outers.SetValue(c ?? Color.clear, 0));
					ColorButton(cRect2, $"{label} line selected", false, colors[1], c => outers.SetValue(c ?? Color.clear, 1));
				}
			}
			if (innerName != "")
			{
				var inners = trv.Field(innerName).GetValue<Array>();
				if (inners is OptionalColor[] optionals)
				{
					ColorButton(cRect3, $"{label} fill", true, optionals[0]?.color, c => inners.SetValue(new OptionalColor(c), 0));
					ColorButton(cRect4, $"{label} fill selected", true, optionals[1]?.color, c => inners.SetValue(new OptionalColor(c), 1));
				}
				if (inners is Color[] colors)
				{
					ColorButton(cRect3, $"{label} fill", false, colors[0], c => inners.SetValue(c ?? Color.clear, 0));
					ColorButton(cRect4, $"{label} fill selected", false, colors[1], c => inners.SetValue(c ?? Color.clear, 1));
				}
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard();
			list.Begin(inRect);

			PrepareRow(list, "", out var cRect1, out var cRect2, out var cRect3, out var cRect4);
			CenteredLabel(cRect1.Union(cRect2), "Line /Line Selected");
			CenteredLabel(cRect3.Union(cRect4), "Fill / Fill Selected");

			ColorEditorRow(list, "Player normal", nameof(Settings.playerNormalOuterColors), nameof(Settings.playerInnerColors));
			ColorEditorRow(list, "Player drafted", "", nameof(Settings.playerDraftedOuterColors));
			ColorEditorRow(list, "Player downed", "", nameof(Settings.playerDownedOuterColors));
			ColorEditorRow(list, "Player mental", "", nameof(Settings.playerMentalInnerColors));

			list.Gap();
			ColorEditorRow(list, "Animals / Other", nameof(Settings.defaultOuterColors), nameof(Settings.defaultInnerColors));

			list.Gap();
			ColorEditorRow(list, "Custom / Mods", nameof(Settings.customOuterColors), nameof(Settings.customInnerColors));

			list.End();
		}
	}
}