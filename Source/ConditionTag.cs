using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public abstract class ConditionTag
	{
		public const float padding = 5;

		public static ConditionTag[] AllTags =
		[
			new AncientTag(),
			new AnimalTag(),
			new CreepJoinerTag(),
			new EmpireTag(),
			new EntityTag(),
			new GhoulTag(),
			new HoraxCultTag(),
			new HumanTag(),
			new InsectTag(),
			new MechanoidTag(),
			new Mechanitor(),
			new MutantTag(),
			new PirateTag(),
			new PrisonerTag(),
			new ShamblerTag(),
			new SlaveTag(),
			new VehicleTag(),
			//
			new AdultTag(),
			new AttackingTag(),
			new AwakeTag(),
			new AwokenCorpseTag(),
			new CanCastTag(),
			new ColonistTag(),
			new ControllableTag(),
			new CrawlingTag(),
			new DeadTag(),
			new DownedTag(),
			new ExitingMapTag(),
			new FemaleTag(),
			new FreeTag(),
			new HostileTag(),
			new IdleTag(),
			new InjuredTag(),
			new MaleTag(),
			new ManhunterTag(),
			new MeleeTag(),
			new MentalTag(),
			new ModdedTag(),
			new OverseenTag(),
			new PlayerFactionTag(),
			new PredatorHuntTag(),
			new SelfShutdownTag(),
			new TameTag(),
			//
			new KindDefTag(),
			new FactionNameTag(),
			new PawnNameTag(),
			new HediffTag(),
		];

		public ConditionTag Clone()
		{
			var copy = (ConditionTag)Activator.CreateInstance(GetType());
			Traverse.IterateFields(this, copy, (t1, t2) => t2.SetValue(t1.GetValue()));
			return copy;
		}

		public string BaseLabel => GetType().Name.TranslateSimple();
		public string MinimalLabel => Negated ? $"Not{GetType().Name}".TranslateSimple() : BaseLabel;
		public virtual string Label => MinimalLabel;
		internal float labelWidth = 0;

		public virtual float CustomWidth => 0;

		private bool _negated = false;
		public bool Negated
		{
			get => _negated;
			set
			{
				if (_negated != value)
				{
					_negated = value;
					labelWidth = 0;
				}
			}
		}

		public abstract bool Matches(Pawn pawn);

		public static GenUI.StackElementWidthGetter<ConditionTag> WidthGetter = delegate (ConditionTag tag)
		{
			var width = tag.CustomWidth;
			if (width > 0)
				return width + 2 * padding;
			if (tag.labelWidth == 0)
				tag.labelWidth = Text.CalcSize(tag.Label).x;
			return tag.labelWidth + 3 * padding + Assets.deleteTagButton.width;
		};

		public virtual void Draw(Rect rect, Action action)
		{
			GUI.color = CharacterCardUtility.StackElementBackground;
			GUI.DrawTexture(rect, BaseContent.WhiteTex);
			GUI.color = Mouse.IsOver(rect) ? GenUI.MouseoverColor : Color.white;
			Widgets.Label(new Rect(rect.x + padding, rect.y, rect.width - 2 * padding - Assets.deleteTagButton.width, rect.height), Label);
			GUI.color = Color.white;
			var deleteRect = rect.RightPartPixels(padding + Assets.deleteTagButton.width).LeftPartPixels(Assets.deleteTagButton.width).ContractedBy(0, (rect.height - Assets.deleteTagButton.height) / 2);
			if (Widgets.ButtonImage(deleteRect, Assets.deleteTagButton))
			{
				action();
				Event.current.Use();
			}
			if (Widgets.ButtonInvisible(rect))
				Find.WindowStack.Add(new Dialog_TagEdit(this));
		}
	}

	public class TagAddButton : ConditionTag
	{
		public override string Label => $"...";
		public override float CustomWidth => Text.CalcSize(Label).x;
		public override bool Matches(Pawn pawn) => false;

		public override void Draw(Rect rect, Action action)
		{
			GUI.color = CharacterCardUtility.StackElementBackground;
			GUI.DrawTexture(rect, BaseContent.WhiteTex);
			GUI.color = Mouse.IsOver(rect) ? GenUI.MouseoverColor : Color.white;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect, Label);
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
			if (Widgets.ButtonInvisible(rect))
				action();
		}
	}

	public abstract class BoolTag : ConditionTag
	{
	}

	public abstract class TextTag : ConditionTag
	{
		private string _text = "";
		public string Text
		{
			get => _text;
			set
			{
				if (_text != value)
				{
					_text = value;
					labelWidth = 0;
				}
			}
		}

		public override string Label => $"{MinimalLabel}: {Text}";
	}
}