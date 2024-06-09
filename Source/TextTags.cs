using System.Linq;
using Verse;

namespace CameraPlus
{


	public class KindDefTag : TextTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ Tools.ContainsCaseInsensitive(pawn.kindDef, Text);
	}

	public class FactionNameTag : TextTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ Tools.ContainsCaseInsensitive(pawn.Faction?.Name, Text);
	}

	public class PawnNameTag : TextTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ Tools.ContainsCaseInsensitive(pawn.Label, Text);
	}

	public class HediffTag : TextTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.health.hediffSet.hediffs.Any(h => Tools.ContainsCaseInsensitive(h.def, Text));
	}

	public class ApparelTag : TextTag
	{
		static bool Contains(Pawn pawn, string text)
		{
			var apparel = pawn.apparel;
			if (apparel == null)
				return false;
			return (apparel.lockedApparel ?? []).Union(apparel.GetDirectlyHeldThings()).Any(t => Tools.ContainsCaseInsensitive(t.Label, text));
		}

		public override bool Matches(Pawn pawn) => Negated ^ Contains(pawn, Text);
	}

	public class InventoryTag : TextTag
	{
		static bool Contains(Pawn pawn, string text)
		{
			var inventory = pawn.inventory;
			if (inventory == null)
				return false;
			return inventory.GetDirectlyHeldThings().Any(t => Tools.ContainsCaseInsensitive(t.Label, text));
		}

		public override bool Matches(Pawn pawn) => Negated ^ Contains(pawn, Text);
	}

	public class EquipmentTag : TextTag
	{
		static bool Contains(Pawn pawn, string text)
		{
			var equipment = pawn.equipment;
			if (equipment == null)
				return false;
			return equipment.GetDirectlyHeldThings().Any(t => Tools.ContainsCaseInsensitive(t.Label, text));
		}

		public override bool Matches(Pawn pawn) => Negated ^ Contains(pawn, Text);
	}

	public class WeaponTag : TextTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ Tools.ContainsCaseInsensitive(pawn.equipment?.Primary?.def?.label, Text);
	}
}