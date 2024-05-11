using System;
using System.Linq;
using UnityEngine;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	public class Dialog_Colors : Window
	{
		public override Vector2 InitialSize => new(730, 420);

		static readonly Vector2 previewSize = new(100, 30);
		static readonly Vector2 colorFieldSize = new(100, 30);
		static readonly Color borderColor = Color.gray.ToTransparent(0.5f);

		public Dialog_Colors()
		{
			doCloseButton = true;
			draggable = true;
		}

		static void ColumnLabel(Rect labelRect, TaggedString label, bool center)
		{
			Text.Anchor = center ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
			Widgets.LabelFit(labelRect, label);
			Text.Anchor = TextAnchor.UpperLeft;
		}

		static void PrepareRow(Listing_Standard list, TaggedString label, out Rect previewRect, out Rect cRect1, out Rect cRect2, out Rect cRect3, out Rect cRect4)
		{
			var h = colorFieldSize.y;
			var rect = list.GetRect(h + 10);

			var labelRect = rect.LeftPartPixels(rect.width - 4 * (Dialog_ColorPicker.spacing + colorFieldSize.x) - Dialog_ColorPicker.spacing - previewSize.x);
			previewRect = new Rect(labelRect.xMax + Dialog_ColorPicker.spacing, labelRect.y, previewSize.x, h).Rounded();
			cRect1 = new Rect(previewRect.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, h).Rounded();
			cRect2 = new Rect(cRect1.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, h).Rounded();
			cRect3 = new Rect(cRect2.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, h).Rounded();
			cRect4 = new Rect(cRect3.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, h).Rounded();

			ColumnLabel(labelRect.TopPartPixels(h), label, false);
		}

		static void ColorButton(Rect rect, string title, OptionalColor color, Action<OptionalColor> newColorCallback)
		{
			GUI.DrawTexture(rect, Assets.colorBackgroundPattern, ScaleMode.StretchToFill);
			if (color.HasValue)
				Widgets.DrawBoxSolidWithOutline(rect, color.Value, borderColor, 2);
			var deleteRect = rect.RightPartPixels(rect.height).ExpandedBy(-4);
			if (color.HasValue)
			{
				GUI.color = Color.white;
				GUI.DrawTexture(deleteRect, Assets.deleteColorButton);
				if (Widgets.ButtonInvisible(deleteRect))
					newColorCallback(new OptionalColor());
			}
			if (Widgets.ButtonInvisible(rect))
				Find.WindowStack.Add(new Dialog_ColorPicker(title, color, newColorCallback));
		}

		void DrawPreview(Rect previewRect, bool isSelected, OptionalColor outerColor, OptionalColor innerColor)
		{
			if (outerColor.HasValue == false && innerColor.HasValue == false)
			{
				ColumnLabel(previewRect, "Default".Translate(), true);
				return;
			}

			var rect = new Rect(previewRect.center.x + (isSelected ? 5 : -previewRect.height - 5), previewRect.y, previewRect.height, previewRect.height);
			var material = Assets.previewMaterial;
			material.SetColor("_OutlineColor", outerColor.color ?? Color.clear);
			material.SetColor("_FillColor", innerColor.color ?? Color.clear);
			GenUI.DrawTextureWithMaterial(rect.ContractedBy(4).Rounded(), Assets.dummyTexture, material);
		}

		void ColorEditorRow(Listing_Standard list, TaggedString label, OptionalColor[] outers, OptionalColor[] inners)
		{
			PrepareRow(list, label, out var previewRect, out var cRect1, out var cRect2, out var cRect3, out var cRect4);

			DrawPreview(previewRect, false, outers[0], inners[0]);
			ColorButton(cRect1, $"{label} {"Line".Translate()}", outers[0], c => outers[0] = c);
			ColorButton(cRect2, $"{label} {"Fill".Translate()}", inners[0], c => inners[0] = c);

			DrawPreview(previewRect, true, outers[1], inners[1]);
			var selected = "StartingPawnsSelected".Translate();
			ColorButton(cRect3, $"{label} {"Line".Translate()} {selected}", outers[1], c => outers[1] = c);
			ColorButton(cRect4, $"{label} {"Fill".Translate()} {selected}", inners[1], c => inners[1] = c);
		}

		public override void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard();
			list.Begin(inRect);

			PrepareRow(list, "", out var previewRect, out var cRect1, out var cRect2, out var cRect3, out var cRect4);

			Widgets.DrawTextureFitted(previewRect, Assets.columnHeaderPreview, 1);
			Widgets.DrawTextureFitted(cRect1.Union(cRect2), Assets.columnHeader, 1);
			Widgets.DrawTextureFitted(cRect3.Union(cRect4), Assets.columnHeaderSelected, 1);

			var colony = "Player".Translate();
			ColorEditorRow(list, $"{colony} {"PlanetTemperature_Normal".Translate()}", Settings.playerNormalOuterColors, Settings.playerNormalInnerColors);
			ColorEditorRow(list, $"{colony} {"CommandDraftLabel".Translate()}", Settings.playerDraftedOuterColors, Settings.playerDraftedInnerColors);
			ColorEditorRow(list, $"{colony} {"DownedLower".Translate().CapitalizeFirst()}", Settings.playerDownedOuterColors, Settings.playerDownedInnerColors);
			ColorEditorRow(list, $"{colony} {"BrokenDown".Translate()}", Settings.playerMentalOuterColors, Settings.playerMentalInnerColors);

			list.Gap(20);
			ColorEditorRow(list, $"{"AnimalsSection".Translate()} / {"AutoSlaugtherHeaderColOther".Translate()}", Settings.defaultOuterColors, Settings.defaultInnerColors);

			list.Gap(20);
			ColorEditorRow(list, $"{"ScenariosCustom".Translate()} / {"Mods".Translate()}", Settings.customOuterColors, Settings.customInnerColors);

			list.End();
		}
	}
}