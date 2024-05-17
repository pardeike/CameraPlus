using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class Dialog_Colors : Window
	{
		public override Vector2 InitialSize => new(950, 420);

		static readonly Vector2 previewSize = new(100, 30);
		static readonly Vector2 colorFieldSize = new(100, 30);
		static readonly Color borderColor = Color.gray.ToTransparent(0.5f);

		public Dialog_Colors()
		{
			doCloseButton = true;
			draggable = true;
		}

		static void PrepareRow(Listing_Standard list, List<ConditionTag> tags, out Rect previewRect, out Rect cRect1, out Rect cRect2, out Rect cRect3, out Rect cRect4)
		{
			var h = colorFieldSize.y;
			var rect = list.GetRect(h + 10);

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
			var material = Assets.previewMaterial;
			material.SetColor("_OutlineColor", outerColor);
			material.SetColor("_FillColor", innerColor);
			material.SetFloat("_OutlineFactor", 0.35f);
			GenUI.DrawTextureWithMaterial(rect.ContractedBy(4).Rounded(), Assets.dummyTexture, material);
		}

		void ColorEditorRow(Listing_Standard list, DotConfig dotConfig)
		{
			PrepareRow(list, dotConfig.conditions, out var previewRect, out var cRect1, out var cRect2, out var cRect3, out var cRect4);

			DrawPreview(previewRect, false, dotConfig.lineColor, dotConfig.fillColor);
			DrawPreview(previewRect, true, dotConfig.lineSelectedColor, dotConfig.fillSelectedColor);

			ColorButton(cRect1, $"{"Line".Translate()}", dotConfig.lineColor, c => dotConfig.lineColor = c);
			ColorButton(cRect2, $"{"Fill".Translate()}", dotConfig.fillColor, c => dotConfig.fillColor = c);

			var selected = "Selected".Translate();
			ColorButton(cRect3, $"{"Line".Translate()} {selected}", dotConfig.lineSelectedColor, c => dotConfig.lineSelectedColor = c);
			ColorButton(cRect4, $"{"Fill".Translate()} {selected}", dotConfig.fillSelectedColor, c => dotConfig.fillSelectedColor = c);
		}

		public override void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard();
			list.Begin(inRect);

			PrepareRow(list, null, out var previewRect, out var cRect1, out var cRect2, out var cRect3, out var cRect4);

			Widgets.DrawTextureFitted(previewRect, Assets.columnHeaderPreview, 1);
			Widgets.DrawTextureFitted(cRect1.Union(cRect2), Assets.columnHeader, 1);
			Widgets.DrawTextureFitted(cRect3.Union(cRect4), Assets.columnHeaderSelected, 1);

			var worldComponent = Find.World.GetComponent<CameraSettings>();
			var dotConfigs = worldComponent.dotConfigs;
			if (dotConfigs == null)
			{
				dotConfigs = [];
				worldComponent.dotConfigs = dotConfigs;
			}
			foreach (var dotConfig in dotConfigs)
			{
				ColorEditorRow(list, dotConfig);
				list.Gap(20);
			}
			if (list.ButtonText("New".TranslateSimple(), null, 0.2f))
				dotConfigs.Add(new DotConfig());

			list.End();
		}
	}
}