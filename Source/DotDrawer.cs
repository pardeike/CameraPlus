using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	static class DotDrawer
	{
		static readonly Mesh meshWest = MeshPool.GridPlaneFlip(Vector2.one);
		static readonly Mesh meshEast = MeshPool.GridPlane(Vector2.one);
		static readonly Mesh meshClipped = MeshPool.GridPlane(Vector2.one / 2);
		static readonly Quaternion downedRotation = Quaternion.Euler(0, 90, 0);
		static readonly List<EdgeDrawCommand>[] edgeDrawBuckets = [
			[], // colonists
			[], // colony animals and player-controlled non-colonists
			[], // enemies
			[], // friendlies
			[], // non-colony animals
		];

		const float clippedScale = 3f;
		const float markerScale = 2f;
		const float markerSizeScaler = 2f;
		const float edgeAltitudeStep = 0.0001f;

		public static void DrawDots(Map map)
		{
			using var measure = PerfMetrics.Measure("DotDrawer.DrawDots");
			PerfMetrics.Count("dotdrawer.draw_calls");
			PerfMetrics.Sample("dotdrawer.all_pawns_spawned", map.mapPawns.AllPawnsSpawned.Count);

			var borderMarkerSize = new Vector2(16f * Prefs.UIScale, 16f * Prefs.UIScale);
			var viewRect = RealViewRect(borderMarkerSize.x * Settings.clippedBorderDistanceFactor);
			var clippedMarkerMapScale = ClippedMarkerMapScale(borderMarkerSize);

			ClearEdgeBuckets();
			if ((Time.frameCount & 255) == 0)
				MarkerCache.PruneInvalid();

			var visiblePawns = 0;
			var markerDraws = 0;
			var edgeDraws = 0;
			foreach (var pawn in map.mapPawns.AllPawnsSpawned)
			{
				if (Tools.IsHiddenFromPlayer(pawn))
					continue;

				visiblePawns++;

				var decision = MarkerDecisionCache.Get(pawn);
				var dotConfig = decision.dotConfig;
				var drawEdge = false;
				var edgeVector = default(Vector2);
				if (decision.edgeEnabled)
				{
					var (vec, clipped) = ConfinedPoint(new Vector2(pawn.DrawPos.x, pawn.DrawPos.z), viewRect);
					if (clipped)
					{
						drawEdge = true;
						edgeVector = vec;
					}
				}

				if (drawEdge == false && decision.canDrawInsideMarker == false)
					continue;

				if (decision.hasMarkerColors == false)
				{
					PerfMetrics.Count("dotdrawer.skipped_colorless");
					continue;
				}

				var useMarkers = DotTools.GetMarkerColors(pawn, dotConfig, out var innerColor, out var outerColor);
				if (useMarkers == false)
					continue;

				var materials = MarkerCache.MaterialFor(pawn, dotConfig, decision.canDrawInsideMarker, drawEdge);
				if (materials == null)
					continue;

				if (drawEdge)
				{
					var materialClipped = materials.edgeDot;
					if (materialClipped != null)
					{
						var edgeFillColor = DotTools.GetEdgeFillColor(pawn, innerColor);
						var command = new EdgeDrawCommand(pawn, dotConfig, materials, edgeVector, edgeFillColor, outerColor);
						edgeDrawBuckets[command.layer].Add(command);
						edgeDraws++;
					}
				}

				if (decision.canDrawInsideMarker == false)
					continue;

				materials.ApplyColors(innerColor, outerColor);

				Material materialMarker;
				switch (decision.mode)
				{
					case DotStyle.ClassicDots:
						materialMarker = materials.dot;
						markerDraws++;
						DrawMarker(pawn, dotConfig, materialMarker);
						break;
					case DotStyle.BetterSilhouettes:
						materialMarker = materials.silhouette;
						markerDraws++;
						DrawMarker(pawn, dotConfig, materialMarker);
						break;
					case DotStyle.Custom:
						materialMarker = materials.custom;
						if (materialMarker != null)
						{
							markerDraws++;
							DrawMarker(pawn, dotConfig, materialMarker);
						}
						break;
				}
			}

			DrawEdges(clippedMarkerMapScale);

			PerfMetrics.Sample("dotdrawer.visible_pawns", visiblePawns);
			PerfMetrics.Sample("dotdrawer.marker_draws", markerDraws);
			PerfMetrics.Sample("dotdrawer.edge_draws", edgeDraws);
			PerfMetrics.FlushIfNeeded();
		}

		static void DrawEdges(Vector3 clippedMarkerMapScale)
		{
			var edgeDrawCount = EdgeDrawCount();
			if (edgeDrawCount == 0)
				return;

			var altitute = AltitudeLayer.Silhouettes.AltitudeFor() - edgeDrawCount * edgeAltitudeStep;
			for (var layer = edgeDrawBuckets.Length - 1; layer >= 0; layer--)
			{
				var bucket = edgeDrawBuckets[layer];
				for (var i = 0; i < bucket.Count; i++)
				{
					var command = bucket[i];
					command.materials.ApplyEdgeColors(command.fillColor, command.outlineColor);
					DrawClipped(clippedMarkerMapScale, command.dotConfig, altitute, command.edgeVector, command.materials.edgeDot);
					altitute += edgeAltitudeStep;
				}
			}

			ClearEdgeBuckets();
		}

		static int EdgeDrawCount()
		{
			var count = 0;
			for (var i = 0; i < edgeDrawBuckets.Length; i++)
				count += edgeDrawBuckets[i].Count;
			return count;
		}

		static void ClearEdgeBuckets()
		{
			for (var i = 0; i < edgeDrawBuckets.Length; i++)
				edgeDrawBuckets[i].Clear();
		}

		static int EdgeLayerFor(Pawn pawn)
		{
			var playerFaction = pawn.Faction?.IsPlayer ?? false;
			if (pawn.IsColonist)
				return 0;

			if (pawn.RaceProps.Animal && playerFaction)
				return 1;

			if (pawn.IsColonyMechPlayerControlled || pawn.IsPlayerControlled)
				return 1;

			if (pawn.HostileTo(Faction.OfPlayer))
				return 2;

			if (pawn.RaceProps.Animal)
				return 4;

			return 3;
		}

		readonly struct EdgeDrawCommand(Pawn pawn, DotConfig dotConfig, Materials materials, Vector2 edgeVector, Color fillColor, Color outlineColor)
		{
			public readonly DotConfig dotConfig = dotConfig;
			public readonly Materials materials = materials;
			public readonly Vector2 edgeVector = edgeVector;
			public readonly Color fillColor = fillColor;
			public readonly Color outlineColor = outlineColor;
			public readonly int layer = EdgeLayerFor(pawn);
			public readonly int thingIdNumber = pawn.thingIDNumber;
		}

		private static Rect RealViewRect(float contract)
		{
			var p1 = UI.UIToMapPosition(contract, contract + 36); // 36 is bottom bar
			var wh = UI.UIToMapPosition(UI.screenWidth - contract, UI.screenHeight - contract) - p1;
			return new Rect(p1.x, p1.z, wh.x, wh.z);
		}

		private static Vector3 ClippedMarkerMapScale(Vector2 size)
		{
			var halfSize = size / 2f;
			var center = new Vector2(UI.screenWidth / 2f, UI.screenHeight / 2f);
			var p1 = UI.UIToMapPosition(center - halfSize);
			var p2 = UI.UIToMapPosition(center + halfSize);
			return p2 - p1;
		}

		private static (Vector2 vector, bool clipped) ConfinedPoint(Vector2 p, Rect r)
		{
			var center = new Vector2(r.x + r.width / 2, r.y + r.height / 2);
			var delta = p - center;
			var halfWidth = r.width / 2f;
			var halfHeight = r.height / 2f;

			if (Mathf.Abs(delta.x) <= halfWidth && Mathf.Abs(delta.y) <= halfHeight)
				return (p, false);

			var xScale = delta.x == 0f ? float.MaxValue : halfWidth / Mathf.Abs(delta.x);
			var yScale = delta.y == 0f ? float.MaxValue : halfHeight / Mathf.Abs(delta.y);
			var scale = Mathf.Min(xScale, yScale);
			return (center + delta * scale, true);
		}

		private static void DrawClipped(Vector3 scale, DotConfig dotConfig, float altitute, Vector2 vec, Material materialClipped)
		{
			using var measure = PerfMetrics.Measure("DotDrawer.DrawClipped");
			var pos = vec.ToVector3();
			pos.y = altitute;
			var matrixClipped = Matrix4x4.TRS(pos, Quaternion.identity, scale * clippedScale * Settings.clippedRelativeSize * (dotConfig?.relativeSize ?? 1f));
			Graphics.DrawMesh(meshClipped, matrixClipped, materialClipped, 0);
		}

		private static void DrawMarker(Pawn pawn, DotConfig dotConfig, Material materialMarker)
		{
			using var measure = PerfMetrics.Measure("DotDrawer.DrawMarker");
			var q = pawn.Downed ? downedRotation : Quaternion.identity;
			var posMarker = pawn.Drawer.renderer.GetBodyPos(pawn.DrawPos, pawn.GetPosture(), out _);
			var isAnimal = pawn.RaceProps.Animal && pawn.Name != null;
			var miscPlayer = isAnimal == false && pawn.Faction == Faction.OfPlayer && pawn.IsColonistPlayerControlled == false;
			var drawSize = pawn.Drawer.renderer?.BodyGraphic?.drawSize ?? pawn.DrawSize;
			var finalDrawSize = miscPlayer ? 1.5f * drawSize : drawSize;
			var relativeSize = Settings.dotRelativeSize * (dotConfig?.relativeSize ?? 1f);
			var matrixMarker = Matrix4x4.TRS(posMarker, q, Vector3.one * Mathf.Pow((finalDrawSize.x + finalDrawSize.y) / 2, 1 / markerSizeScaler) * markerScale * relativeSize);
			var mesh = pawn.Rotation == Rot4.West ? meshWest : meshEast;
			Graphics.DrawMesh(mesh, matrixMarker, materialMarker, 0);
		}
	}
}
