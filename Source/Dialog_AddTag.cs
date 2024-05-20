using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class Dialog_AddTag : Window
	{
		private readonly (string label, List<ChooseTag> tags, int rowCount)[] categories =
			new (string label, ConditionTag[] tags, int rowCount)[]
			{
				("TagCategoryType", ConditionTag.AllTypeTags, 5),
				("TagCategoryAttribute", ConditionTag.AllAttributeTags, 6),
				("TagCategoryText", ConditionTag.AllTextTags, 2)
			}
			.Select(item => (item.label.TranslateSimple(), item.tags.Select(t => new ChooseTag(t)).ToList(), item.rowCount)).ToArray();

		private readonly Action<ConditionTag> callback;
		private bool negated;

		public override Vector2 InitialSize => new(640f, 440f);

		public Dialog_AddTag(Action<ConditionTag> callback)
		{
			this.callback = callback;
			doCloseButton = false;
			doCloseX = true;
			draggable = true;
			UpdateNegation();
		}

		void UpdateNegation()
		{
			foreach (var (_, tags, _) in categories)
				tags.Do(tag => tag.Negated = negated);
		}

		private void ChooseTag(ChooseTag tag)
		{
			var newTag = tag.ClonedTag;
			newTag.Negated = tag.Negated;
			callback(newTag);
			Close();
		}

		private void DrawTags(Listing_Standard list, List<ChooseTag> tags, float catHeight)
		{
			var rect = list.GetRect(catHeight);
			var font = Text.Font;
			Text.Font = GameFont.Tiny;
			GenUI.DrawElementStack(rect, Text.LineHeightOf(GameFont.Tiny), tags, (r, t) => t.Draw(r, () => ChooseTag(t), null), t => ConditionTag.WidthGetter(t), 4, 5, false);
			Text.Font = font;
		}

		public override void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard();
			list.Begin(inRect);

			foreach (var (label, tags, rowCount) in categories)
			{
				list.Label(label);
				DrawTags(list, tags, 21 * rowCount);
				list.Gap(20);
			}

			list.End();

			var opposite = "Opposite".Translate();
			var rect = inRect.BottomPartPixels(24).LeftPartPixels(Text.CalcSize(opposite).x + 34);
			var oldNegated = negated;
			Widgets.CheckboxLabeled(rect, opposite, ref negated);
			if (oldNegated != negated)
				UpdateNegation();
		}
	}
}