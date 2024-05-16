using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	public class Dialog_Colors : Window
	{
		public override Vector2 InitialSize => new(950, 420);

		static readonly Vector2 previewSize = new(100, 30);
		static readonly Vector2 colorFieldSize = new(100, 30);
		static readonly Color borderColor = Color.gray.ToTransparent(0.5f);

		static readonly List<ConditionTag>[] tags = new List<ConditionTag>[6];

		public Dialog_Colors()
		{
			doCloseButton = true;
			draggable = true;

			for (var i = 0; i < tags.Length; i++)
				tags[i] = ConditionTag.AllTags.InRandomOrder().Take(Rand.RangeInclusive(1, 4)).Select(t => { t.Negated = Rand.Bool; return t.Clone(); }).ToList();
		}

		static void ColumnLabel(Rect labelRect, TaggedString label, bool center)
		{
			Text.Anchor = center ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
			Widgets.LabelFit(labelRect, label);
			Text.Anchor = TextAnchor.UpperLeft;
		}

		static void PrepareRow(Listing_Standard list, int idx, TaggedString label, out Rect previewRect, out Rect cRect1, out Rect cRect2, out Rect cRect3, out Rect cRect4)
		{
			var h = colorFieldSize.y;
			var rect = list.GetRect(h + 10);

			var labelRect = rect.LeftPartPixels(rect.width - 4 * (Dialog_ColorPicker.spacing + colorFieldSize.x) - Dialog_ColorPicker.spacing - previewSize.x);
			previewRect = new Rect(labelRect.xMax + Dialog_ColorPicker.spacing, labelRect.y, previewSize.x, h).Rounded();
			cRect1 = new Rect(previewRect.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, h).Rounded();
			cRect2 = new Rect(cRect1.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, h).Rounded();
			cRect3 = new Rect(cRect2.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, h).Rounded();
			cRect4 = new Rect(cRect3.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, h).Rounded();

			if (idx < 0)
				ColumnLabel(labelRect.TopPartPixels(h), label, false);
			else
			{
				var font = Text.Font;
				Text.Font = GameFont.Tiny;
				var currentTags = tags[idx].Union([new TagAddButton()]).ToList();
				GenUI.DrawElementStack(labelRect, Text.LineHeightOf(GameFont.Tiny), currentTags, (rect, tag) => tag.Draw(rect, delegate ()
				{
					if (tag is TagAddButton)
						tags[idx].Add(ConditionTag.AllTags.RandomElement().Clone());
					else
						LongEventHandler.ExecuteWhenFinished(() => tags[idx].RemoveWhere(t => t == tag));
				}),
				ConditionTag.WidthGetter, 4, 5, false);
				Text.Font = font;
			}
		}

		static void ColorButton(Rect rect, string title, OptionalColor color, Action<OptionalColor> newColorCallback, Action defaultCallback)
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
			var current = Event.current;
			if (current.type == EventType.MouseDown && Input.GetMouseButton(1) && Mouse.IsOver(rect))
			{
				defaultCallback();
				current.Use();
			}
			if (Widgets.ButtonInvisible(rect))
				Find.WindowStack.Add(new Dialog_ColorPicker(title, color, newColorCallback));
		}

		void DrawPreview(Rect previewRect, bool isAnimal, bool isSelected, OptionalColor outerColor, OptionalColor innerColor)
		{
			var rect = new Rect(previewRect.center.x + (isSelected ? 5 : -previewRect.height - 5), previewRect.y, previewRect.height, previewRect.height);
			var material = Assets.previewMaterials[isAnimal ? 1 : 0];
			material.SetColor("_OutlineColor", outerColor.color ?? Color.clear);
			material.SetColor("_FillColor", innerColor.color ?? Color.clear);
			material.SetFloat("_OutlineFactor", 0.35f);
			GenUI.DrawTextureWithMaterial(rect.ContractedBy(4).Rounded(), Assets.dummyTexture, material);
		}

		void ColorEditorRow(Listing_Standard list, int idx, TaggedString label, bool isAnimal, bool indicateDefault, OptionalColor[] outers, OptionalColor[] inners, OptionalColor[] outerDefaults, OptionalColor[] innerDefaults)
		{
			PrepareRow(list, idx, label, out var previewRect, out var cRect1, out var cRect2, out var cRect3, out var cRect4);

			var notDefault = indicateDefault == false || outers[0].HasValue || outers[1].HasValue || inners[0].HasValue || inners[1].HasValue;
			if (notDefault)
			{
				if (indicateDefault == false || outers[0].HasValue || inners[0].HasValue)
					DrawPreview(previewRect, isAnimal, false, outers[0], inners[0]);
				if (indicateDefault == false || outers[1].HasValue || inners[1].HasValue)
					DrawPreview(previewRect, isAnimal, true, outers[1], inners[1]);
			}
			else
				ColumnLabel(previewRect, "Default".Translate(), true);

			ColorButton(cRect1, $"{label} {"Line".Translate()}", outers[0], c => outers[0] = c, () => { outers[0] = outerDefaults[0]; });
			ColorButton(cRect2, $"{label} {"Fill".Translate()}", inners[0], c => inners[0] = c, () => { inners[0] = innerDefaults[0]; });

			var selected = "StartingPawnsSelected".Translate();
			ColorButton(cRect3, $"{label} {"Line".Translate()} {selected}", outers[1], c => outers[1] = c, () => { outers[1] = outerDefaults[1]; });
			ColorButton(cRect4, $"{label} {"Fill".Translate()} {selected}", inners[1], c => inners[1] = c, () => { inners[1] = innerDefaults[1]; });
		}

		public void ConditionTags()
		{

		}

		public override void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard();
			list.Begin(inRect);

			PrepareRow(list, -1, "", out var previewRect, out var cRect1, out var cRect2, out var cRect3, out var cRect4);

			Widgets.DrawTextureFitted(previewRect, Assets.columnHeaderPreview, 1);
			Widgets.DrawTextureFitted(cRect1.Union(cRect2), Assets.columnHeader, 1);
			Widgets.DrawTextureFitted(cRect3.Union(cRect4), Assets.columnHeaderSelected, 1);

			var defaults = new CameraPlusSettings();

			var colony = "Player".Translate();
			ColorEditorRow(list, 0, $"{colony} {"PlanetTemperature_Normal".Translate()}", false, false, Settings.playerNormalOuterColors, Settings.playerNormalInnerColors, defaults.playerNormalOuterColors, defaults.playerNormalInnerColors);
			ColorEditorRow(list, 1, $"{colony} {"CommandDraftLabel".Translate()}", false, false, Settings.playerDraftedOuterColors, Settings.playerDraftedInnerColors, defaults.playerDraftedOuterColors, defaults.playerDraftedInnerColors);
			ColorEditorRow(list, 2, $"{colony} {"DownedLower".Translate().CapitalizeFirst()}", false, false, Settings.playerDownedOuterColors, Settings.playerDownedInnerColors, defaults.playerDownedOuterColors, defaults.playerDownedInnerColors);
			ColorEditorRow(list, 3, $"{colony} {"BrokenDown".Translate()}", false, false, Settings.playerMentalOuterColors, Settings.playerMentalInnerColors, defaults.playerMentalOuterColors, defaults.playerMentalInnerColors);

			list.Gap(20);
			ColorEditorRow(list, 4, $"{"AnimalsSection".Translate()} / {"AutoSlaugtherHeaderColOther".Translate()}", true, true, Settings.defaultOuterColors, Settings.defaultInnerColors, defaults.defaultOuterColors, defaults.defaultInnerColors);

			list.Gap(20);
			ColorEditorRow(list, 5, $"{"ScenariosCustom".Translate()} / {"Mods".Translate()}", false, true, Settings.customOuterColors, Settings.customInnerColors, defaults.customOuterColors, defaults.customInnerColors);

			list.End();
		}
	}
}