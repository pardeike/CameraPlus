using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public abstract class ConditionTag : IExposable
	{
		public const float padding = 5;

		public static readonly ConditionTag[] AllTypeTags =
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
			new WildManTag(),
		];
		public static readonly ConditionTag[] AllAttributeTags =
		[
			new AdultTag(),
			new AttackingTag(),
			new AwakeTag(),
			new AwokenCorpseTag(),
			new ChildTag(),
			new CanCastTag(),
			new ColonistTag(),
			new ControllableTag(),
			new CrawlingTag(),
			new DeadTag(),
			new DeathrestingTag(),
			new DownedTag(),
			new DraftedTag(),
			new ExitingMapTag(),
			new FemaleTag(),
			new FreeTag(),
			new GuestTag(),
			new HostileTag(),
			new IdleTag(),
			new InjuredTag(),
			new InspiredTag(),
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
			new TeenagerTag(),
		];

		public static readonly ConditionTag[] AllTextTags =
		[
			new KindDefTag(),
			new FactionNameTag(),
			new PawnNameTag(),
			new HediffTag(),
		];

		public virtual ConditionTag Clone()
		{
			var copy = (ConditionTag)Activator.CreateInstance(GetType());
			Traverse.IterateFields(this, copy, (t1, t2) => t2.SetValue(t1.GetValue()));
			return copy;
		}

		public virtual bool HasDelete => true;
		public string BaseLabel => GetType().Name.TranslateSimple();
		public string MinimalLabel => Negated ? $"Not{GetType().Name}".TranslateSimple() : BaseLabel;
		public virtual string Label => MinimalLabel;
		internal float labelWidth = 0;

		public virtual float CustomWidth => 0;

		internal bool _negated = false;
		public virtual bool Negated
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

		public virtual bool Matches(Pawn pawn) => false;

		public static GenUI.StackElementWidthGetter<ConditionTag> WidthGetter = delegate (ConditionTag tag)
		{
			var width = tag.CustomWidth;
			if (width > 0)
				return width + 2 * padding;
			if (tag.labelWidth == 0)
				tag.labelWidth = Text.CalcSize(tag.Label).x;
			var extraWidth = tag.HasDelete ? padding + Assets.deleteTagButton.width : 0;
			return tag.labelWidth + 2 * padding + extraWidth;
		};

		public virtual void Draw(Rect rect, Action selectAction, Action deleteAction)
		{
			GUI.color = CharacterCardUtility.StackElementBackground;
			GUI.DrawTexture(rect, BaseContent.WhiteTex);
			GUI.color = Mouse.IsOver(rect) ? GenUI.MouseoverColor : Color.white;
			var extraWidth = HasDelete ? padding + Assets.deleteTagButton.width : 0;
			Widgets.Label(new Rect(rect.x + padding, rect.y, rect.width - padding - extraWidth, rect.height), Label);
			GUI.color = Color.white;
			if (HasDelete)
			{
				var deleteRect = rect.RightPartPixels(padding + Assets.deleteTagButton.width).LeftPartPixels(Assets.deleteTagButton.width).ContractedBy(0, (rect.height - Assets.deleteTagButton.height) / 2);
				if (Widgets.ButtonImage(deleteRect, Assets.deleteTagButton))
					deleteAction();
			}
			if (Widgets.ButtonInvisible(rect))
				selectAction();
		}

		public virtual void ExposeData()
		{
			Scribe_Values.Look(ref _negated, "negated", false);
		}
	}

	public class NumberIndexButton(int n) : ConditionTag
	{
		private readonly int n = n;
		public override string Label => $"{n}.";
		public override float CustomWidth => Text.CalcSize(Label).x;
		public override void Draw(Rect rect, Action selectAction, Action deleteAction) => Widgets.Label(rect, Label);
	}

	public class ChooseTag(ConditionTag tag) : ConditionTag
	{
		private readonly ConditionTag tag = tag;

		public override string Label => tag.Label;
		public override bool HasDelete => false;
		public ConditionTag ClonedTag => tag.Clone();
		public override bool Negated
		{
			get => tag.Negated;
			set
			{
				tag.Negated = value;
				labelWidth = 0;
			}
		}
	}

	public class TagAddButton : ConditionTag
	{
		public override float CustomWidth => 24;
		public override void Draw(Rect rect, Action selectAction, Action deleteAction)
		{
			if (Widgets.ButtonImageFitted(rect, TexButton.Plus))
				selectAction();
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

		public override ConditionTag Clone()
		{
			var copy = (TextTag)base.Clone();
			copy._text = _text;
			return copy;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref _text, "text", "");
		}
	}
}