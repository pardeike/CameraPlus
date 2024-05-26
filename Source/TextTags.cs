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
}