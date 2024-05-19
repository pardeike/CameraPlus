using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class Dialog_TagEdit : Window
	{
		private readonly ConditionTag tag;
		private bool focusedNameField;

		public override Vector2 InitialSize => new(240f, 142f);

		public Dialog_TagEdit(ConditionTag tag)
		{
			this.tag = tag;
			doCloseButton = false;
			doCloseX = true;
		}

		private bool IsReturn => Event.current.isKey && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;

		public override void DoWindowContents(Rect inRect)
		{
			if (IsReturn)
			{
				Close(true);
				Event.current.Use();
				return;
			}

			var list = new Listing_Standard();
			list.Begin(inRect);

			list.Label(tag.BaseLabel);
			if (tag is TextTag textTag)
			{
				GUI.SetNextControlName("NameField");
				textTag.Text = list.TextEntry(textTag.Text);
			}
			list.Gap();

			var negated = tag.Negated;
			list.CheckboxLabeled("Opposite".Translate(), ref negated);
			tag.Negated = negated;

			list.End();

			if (focusedNameField == false)
			{
				UI.FocusControl("NameField", this);
				focusedNameField = true;
			}
		}
	}
}