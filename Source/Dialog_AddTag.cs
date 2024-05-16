using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class Dialog_AddTag : Window
	{
		private readonly List<TagChooseButton>[] categories =
			new ConditionTag[][] { ConditionTag.AllTypeTags, ConditionTag.AllAttributeTags, ConditionTag.AllTextTags }
			.Select(tags => tags.Select(t => new TagChooseButton(t)).ToList()).ToArray();

		private readonly Action<ConditionTag> callback;
		private readonly int total;

		public override Vector2 InitialSize => new(600f, 280f);

		public Dialog_AddTag(Action<ConditionTag> callback)
		{
			this.callback = callback;
			doCloseButton = false;
			doCloseX = true;
			draggable = true;
			total = categories.Sum(c => c.Count);
		}

		private void ChooseTag(TagChooseButton tag)
		{
			callback(tag.ClonedTag);
			Close();
		}

		public override void DoWindowContents(Rect inRect)
		{
			var list = new Listing_Standard();
			list.Begin(inRect);
			var font = Text.Font;
			Text.Font = GameFont.Tiny;

			var addGap = false;
			const float gap = 10f;
			foreach (var categoryTags in categories)
			{
				if (addGap)
					list.Gap(gap);
				var catHeight = Mathf.Floor((inRect.height - gap * (categories.Length - 1)) * categoryTags.Count / total);
				var rect = list.GetRect(catHeight);
				GenUI.DrawElementStack(rect, Text.LineHeightOf(GameFont.Tiny), categoryTags, (r, t) => t.Draw(r, () => ChooseTag(t)), t => ConditionTag.WidthGetter(t), 4, 5, false);
				addGap = true;
			}

			Text.Font = font;
			list.End();
		}
	}
}