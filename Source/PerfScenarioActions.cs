#if CAMERAPLUS_PERF
using LudeonTK;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	static class PerfScenarioActions
	{
		static readonly string[] edgeAnimalKindNames =
		[
			"Alpaca",
			"Cow",
			"Horse",
			"Elephant",
			"Bear_Grizzly",
		];

		[DebugAction("CameraPlusPerf", "Add edge stress pawns", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
		static void AddEdgeStressPawns()
		{
			var map = Find.CurrentMap;
			if (map == null)
			{
				Log.Warning("CameraPlusPerf: cannot add edge stress pawns without a current map.");
				return;
			}

			var tamedExisting = NormalizeAnimalsForCameraPlus(map);
			var spawnedColonists = SpawnColonists(map, 80);
			var spawnedAnimals = SpawnPlayerAnimals(map, 160);

			Current.cameraDriverInt.SetRootPosAndSize(new Vector3(map.Size.x / 2f, 0f, map.Size.z / 2f), 30f);
			Log.Message($"CameraPlusPerf: edge scenario added {spawnedColonists} colonists, {spawnedAnimals} named player animals, and normalized {tamedExisting} existing animals for Camera+ edge-dot stress.");
		}

		static int NormalizeAnimalsForCameraPlus(Map map)
		{
			var count = 0;
			foreach (var pawn in map.mapPawns.AllPawnsSpawned.ToList())
			{
				if (pawn.RaceProps.Animal == false)
					continue;

				MakeCameraPlusTameAnimal(pawn, $"CP Tame {count + 1}");
				count++;
			}

			return count;
		}

		static int SpawnColonists(Map map, int count)
		{
			var pawnKind = PawnKindDefOf.Colonist;
			var spawned = 0;
			for (var i = 0; i < count; i++)
			{
				var cell = FindEdgeSpawnCell(map, i);
				var request = new PawnGenerationRequest(
					pawnKind,
					Faction.OfPlayer,
					PawnGenerationContext.NonPlayer,
					map.Tile,
					forceGenerateNewPawn: true,
					canGeneratePawnRelations: false,
					mustBeCapableOfViolence: false,
					forceNoGear: true
				);
				var pawn = PawnGenerator.GeneratePawn(request);
				pawn.Name ??= new NameSingle($"CP Edge Colonist {i + 1}");
				GenSpawn.Spawn(pawn, cell, map);
				spawned++;
			}

			return spawned;
		}

		static int SpawnPlayerAnimals(Map map, int count)
		{
			var animalKinds = edgeAnimalKindNames
				.Select(name => DefDatabase<PawnKindDef>.GetNamedSilentFail(name))
				.Where(kind => kind != null)
				.ToArray();
			if (animalKinds.Length == 0)
			{
				Log.Warning("CameraPlusPerf: no configured animal PawnKindDefs were found for edge scenario generation.");
				return 0;
			}

			var spawned = 0;
			for (var i = 0; i < count; i++)
			{
				var kind = animalKinds[i % animalKinds.Length];
				var cell = FindEdgeSpawnCell(map, i + 10000);
				var request = new PawnGenerationRequest(
					kind,
					Faction.OfPlayer,
					PawnGenerationContext.NonPlayer,
					map.Tile,
					forceGenerateNewPawn: true,
					canGeneratePawnRelations: false,
					mustBeCapableOfViolence: false,
					forceNoGear: true
				);
				var pawn = PawnGenerator.GeneratePawn(request);
				MakeCameraPlusTameAnimal(pawn, $"CP Edge {kind.defName} {i + 1}");
				GenSpawn.Spawn(pawn, cell, map);
				spawned++;
			}

			return spawned;
		}

		static void MakeCameraPlusTameAnimal(Pawn pawn, string fallbackName)
		{
			if (pawn.Faction != Faction.OfPlayer)
				pawn.SetFaction(Faction.OfPlayer);
			pawn.Name ??= new NameSingle(fallbackName);
		}

		static IntVec3 FindEdgeSpawnCell(Map map, int index)
		{
			var margin = 5;
			var side = index % 4;
			var slot = index / 4;
			var maxX = map.Size.x - 1 - margin;
			var maxZ = map.Size.z - 1 - margin;
			var spanX = Mathf.Max(1, maxX - margin);
			var spanZ = Mathf.Max(1, maxZ - margin);

			var root = side switch
			{
				0 => new IntVec3(margin + slot * 7 % spanX, 0, margin),
				1 => new IntVec3(maxX, 0, margin + slot * 7 % spanZ),
				2 => new IntVec3(margin + slot * 7 % spanX, 0, maxZ),
				_ => new IntVec3(margin, 0, margin + slot * 7 % spanZ),
			};

			foreach (var cell in GenRadial.RadialCellsAround(root, 14f, true))
			{
				if (cell.InBounds(map) && cell.Standable(map) && cell.GetFirstPawn(map) == null)
					return cell;
			}

			foreach (var cell in map.AllCells.InRandomOrder())
			{
				if (cell.Standable(map) && cell.GetFirstPawn(map) == null)
					return cell;
			}

			return CellFinder.RandomCell(map);
		}
	}
}
#endif
