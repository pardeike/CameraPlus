using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace CameraPlus
{
	// base types

	public class AncientTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.Faction == Faction.OfAncients;
	}

	public class AnimalTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.RaceProps.Animal;
	}

	public class CreepJoinerTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.IsCreepJoiner;
	}

	public class EmpireTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.Faction == Faction.OfEmpire;
	}

	public class EntityTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.Faction == Faction.OfEntities;
	}

	public class GhoulTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.IsGhoul;
	}

	public class HoraxCultTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.Faction == Faction.OfHoraxCult;
	}

	public class HumanTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.RaceProps.Humanlike;
	}

	public class InsectTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.Faction == Faction.OfInsects;
	}

	public class MechanoidTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.RaceProps.IsMechanoid;
	}

	public class Mechanitor : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ MechanitorUtility.IsMechanitor(pawn);
	}

	public class MutantTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.IsMutant;
	}

	public class PirateTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.Faction == Faction.OfPirates;
	}

	public class PrisonerTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.IsPrisoner;
	}

	public class ShamblerTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.IsShambler;
	}

	public class SlaveTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.IsSlave;
	}

	public class VehicleTag : BoolTag
	{
		static readonly Type vehicleType = AccessTools.TypeByName("Vehicles.VehiclePawn");
		public override bool Matches(Pawn pawn) => Negated ^ pawn.GetType() == vehicleType;
	}

	// conditions

	public class AdultTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.DevelopmentalStage == DevelopmentalStage.Adult;
	}

	public class AttackingTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.IsAttacking();
	}

	public class AwakeTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.Awake();
	}

	public class AwokenCorpseTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.IsAwokenCorpse;
	}

	public class CanCastTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.jobs.curDriver.job.ability.CanCast;
	}

	public class ColonistTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.IsColonist;
	}

	public class ControllableTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ (
			pawn.MentalStateDef == null
			|| pawn.OverseerSubject != null && pawn.OverseerSubject.State == OverseerSubjectState.Overseen
			|| (pawn.mutant?.Def.canBeDrafted ?? false)
		);
	}

	public class CrawlingTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.Crawling;
	}

	public class DeadTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.Dead;
	}

	public class DownedTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.Downed;
	}

	public class ExitingMapTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ PawnUtility.IsExitingMap(pawn);
	}

	public class FemaleTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.gender == Gender.Female;
	}

	public class FreeTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.HostFaction == null;
	}

	public class HostileTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.HostileTo(Faction.OfPlayer);
	}

	public class IdleTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.mindState.IsIdle;
	}

	public class InjuredTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>().Any(hediff_Injury => hediff_Injury.IsPermanent() == false && hediff_Injury.CanHealNaturally());
	}

	public class MaleTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.gender == Gender.Male;
	}

	public class ManhunterTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ (pawn.MentalStateDef == MentalStateDefOf.Manhunter || pawn.MentalStateDef == MentalStateDefOf.ManhunterPermanent);
	}

	public class MeleeTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ (pawn.equipment.PrimaryEq?.PrimaryVerb.IsMeleeAttack ?? false);
	}

	public class MentalTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.MentalStateDef != null;
	}

	public class ModdedTag : BoolTag
	{
		static readonly Assembly executingAssembly = Assembly.GetExecutingAssembly();
		public override bool Matches(Pawn pawn) => Negated ^ pawn.GetType().Assembly != executingAssembly;
	}

	public class OverseenTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.OverseerSubject?.State == OverseerSubjectState.Overseen;
	}

	public class PlayerFactionTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.Faction == Faction.OfPlayer;
	}

	public class PredatorHuntTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.CurJob?.def == JobDefOf.PredatorHunt;
	}

	public class SelfShutdownTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.needs.energy.IsSelfShutdown;
	}

	public class TameTag : BoolTag
	{
		public override bool Matches(Pawn pawn) => Negated ^ pawn.Faction == Faction.OfPlayer && pawn.Name != null;
	}
}