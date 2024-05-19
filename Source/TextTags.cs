using Verse;

namespace CameraPlus
{
	public class KindDefTag : TextTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.kindDef.defName.Contains(Text);
	}

	public class FactionNameTag : TextTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.Faction.def.defName.Contains(Text);
	}

	public class PawnNameTag : TextTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.Label.Contains(Text);
	}

	public class HediffTag : TextTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.health.hediffSet.hediffs.Any(h => h.def.defName.Contains(Text));
	}
}