using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class Dialog_Colors : Window
	{
		public override Vector2 InitialSize => new(950, 640);
		private Vector2 scrollPosition = Vector2.zero;

		const float rowHeight = 30f;
		const float rowSpacing = 10f;
		static readonly Vector2 previewSize = new(100, 30);
		static readonly Vector2 colorFieldSize = new(100, rowHeight);
		static readonly Color borderColor = Color.gray.ToTransparent(0.5f);

		public Dialog_Colors()
		{
			doCloseButton = true;
			draggable = true;
		}

		static void PrepareRow(Listing_Standard list, bool first, List<ConditionTag> tags, out Rect previewRect, out Rect cRect1, out Rect cRect2, out Rect cRect3, out Rect cRect4)
		{
			if (first == false)
				list.Gap(rowSpacing);

			var h = colorFieldSize.y;
			var rect = list.GetRect(h);

			var labelRect = rect.LeftPartPixels(rect.width - 4 * (Dialog_ColorPicker.spacing + colorFieldSize.x) - Dialog_ColorPicker.spacing - previewSize.x);
			previewRect = new Rect(labelRect.xMax + Dialog_ColorPicker.spacing, labelRect.y, previewSize.x, h).Rounded();
			cRect1 = new Rect(previewRect.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, h).Rounded();
			cRect2 = new Rect(cRect1.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, h).Rounded();
			cRect3 = new Rect(cRect2.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, h).Rounded();
			cRect4 = new Rect(cRect3.xMax + Dialog_ColorPicker.spacing, labelRect.y, colorFieldSize.x, h).Rounded();

			if (tags != null)
			{
				var font = Text.Font;
				Text.Font = GameFont.Tiny;
				var allTags = tags.Union([new TagAddButton()]).ToList();
				GenUI.DrawElementStack(labelRect, Text.LineHeightOf(GameFont.Tiny), allTags, (rect, tag) => tag.Draw(rect, delegate ()
				{
					if (tag is TagAddButton)
						Find.WindowStack.Add(new Dialog_AddTag(newTag => tags.Add(newTag.Clone())));
					else
						LongEventHandler.ExecuteWhenFinished(() => tags.RemoveWhere(t => t == tag));
				}),
				ConditionTag.WidthGetter, 4, 5, false);
				Text.Font = font;
			}
		}

		static void ColorButton(Rect rect, string title, Color color, Action<Color> newColorCallback)
		{
			GUI.DrawTexture(rect, Assets.colorBackgroundPattern, ScaleMode.StretchToFill);
			Widgets.DrawBoxSolidWithOutline(rect, color, borderColor, 2);
			var deleteRect = rect.RightPartPixels(rect.height).ExpandedBy(-4);
			GUI.color = Color.white;
			var current = Event.current;
			if (Widgets.ButtonInvisible(rect))
				Find.WindowStack.Add(new Dialog_ColorPicker(title, color, newColorCallback));
		}

		void DrawPreview(Rect previewRect, bool isSelected, Color outerColor, Color innerColor)
		{
			var rect = new Rect(previewRect.center.x + (isSelected ? 5 : -previewRect.height - 5), previewRect.y, previewRect.height, previewRect.height);
			GUI.color = outerColor;
			GUI.DrawTexture(rect.ContractedBy(4).Rounded(), Assets.outerColonistTexture);
			GUI.color = innerColor;
			GUI.DrawTexture(rect.ContractedBy(4).Rounded(), Assets.innerColonistTexture);
			GUI.color = Color.white;
		}

		void ColorEditorRow(Listing_Standard list, bool first, DotConfig dotConfig)
		{
			PrepareRow(list, first, dotConfig.conditions, out var previewRect, out var cRect1, out var cRect2, out var cRect3, out var cRect4);

			DrawPreview(previewRect, false, dotConfig.lineColor, dotConfig.fillColor);
			DrawPreview(previewRect, true, dotConfig.lineSelectedColor, dotConfig.fillSelectedColor);

			ColorButton(cRect1, $"{dotConfig.name} {"Line".Translate()}", dotConfig.lineColor, c => dotConfig.lineColor = c);
			ColorButton(cRect2, $"{dotConfig.name} {"Fill".Translate()}", dotConfig.fillColor, c => dotConfig.fillColor = c);

			var selected = "Selected".Translate();
			ColorButton(cRect3, $"{dotConfig.name} {"Line".Translate()} {selected}", dotConfig.lineSelectedColor, c => dotConfig.lineSelectedColor = c);
			ColorButton(cRect4, $"{dotConfig.name} {"Fill".Translate()} {selected}", dotConfig.fillSelectedColor, c => dotConfig.fillSelectedColor = c);
		}

		public override void DoWindowContents(Rect inRect)
		{
			inRect.yMax -= 40; // close button footer

			var list = new Listing_Standard();
			list.Begin(inRect);

			PrepareRow(list, true, null, out var previewRect, out var cRect1, out var cRect2, out var cRect3, out var cRect4);
			Widgets.DrawTextureFitted(previewRect, Assets.columnHeaderPreview, 1);
			Widgets.DrawTextureFitted(cRect1.Union(cRect2), Assets.columnHeader, 1);
			Widgets.DrawTextureFitted(cRect3.Union(cRect4), Assets.columnHeaderSelected, 1);
			list.Gap(rowSpacing);
			var outRect = list.GetRect(inRect.height - list.curY - rowSpacing - 30);

			var dotConfigs = Find.World.GetComponent<CameraSettings>().dotConfigs;
			var configCount = dotConfigs.Count;
			var viewRect = new Rect(0, 0, inRect.width - 16, rowHeight * configCount + rowSpacing * (configCount - 1));
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
			var innerList = new Listing_Standard();
			innerList.Begin(viewRect);
			for (var row = 0; row < configCount; ++row)
				ColorEditorRow(innerList, row == 0, dotConfigs[row]);
			innerList.End();
			Widgets.EndScrollView();

			list.Gap(rowSpacing);
			if (list.ButtonText("New".TranslateSimple(), null, 0.2f))
				dotConfigs.Add(new DotConfig());

			list.End();
		}
	}
}