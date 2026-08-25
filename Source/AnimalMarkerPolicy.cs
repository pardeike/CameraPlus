using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	readonly struct AnimalMarkerPolicy
	{
		public readonly bool isAnimal;
		public readonly bool isNamed;
		public readonly bool isPlayerFaction;
		public readonly bool included;
		public readonly bool rulesAllowed;
		public readonly bool useAnimalMarkerTextures;
		public readonly bool useAnimalEdgeColor;

		AnimalMarkerPolicy(
			bool isAnimal,
			bool isNamed,
			bool isPlayerFaction,
			bool included,
			bool rulesAllowed,
			bool useAnimalMarkerTextures,
			bool useAnimalEdgeColor)
		{
			this.isAnimal = isAnimal;
			this.isNamed = isNamed;
			this.isPlayerFaction = isPlayerFaction;
			this.included = included;
			this.rulesAllowed = rulesAllowed;
			this.useAnimalMarkerTextures = useAnimalMarkerTextures;
			this.useAnimalEdgeColor = useAnimalEdgeColor;
		}

		public static AnimalMarkerPolicy For(Pawn pawn)
		{
			if (pawn?.RaceProps?.Animal != true)
				return new AnimalMarkerPolicy(false, false, false, true, true, false, false);

			var isNamed = pawn.Name != null;
			var isPlayerFaction = pawn.Faction?.IsPlayer ?? false;
			var settings = Settings;
			if (settings == null)
				return new AnimalMarkerPolicy(true, isNamed, isPlayerFaction, true, true, false, false);

			var included = settings.customNameStyle != LabelStyle.HideAnimals
				&& (settings.includeNotTamedAnimals || isNamed || isPlayerFaction);

			return new AnimalMarkerPolicy(
				true,
				isNamed,
				isPlayerFaction,
				included,
				included,
				included && settings.customNameStyle == LabelStyle.AnimalsDifferent,
				included && settings.pawnColoredEdgeIndicators);
		}
	}
}
