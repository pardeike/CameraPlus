using RimWorld;
using System;
using System.Collections.Generic;
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
		const float bottomMarkerBarOffset = 36f;

		public static void DrawDots(Map map)
		{
			using var measure = PerfMetrics.Measure("DotDrawer.DrawDots");
			if (map == null || map.Disposed || map.dynamicDrawManager == null)
				return;

			var drawThings = map.dynamicDrawManager.DrawThings;
			if (drawThings == null)
				return;

			PerfMetrics.Count("dotdrawer.draw_calls");
			PerfMetrics.Sample("dotdrawer.registered_drawables", drawThings.Count);

			var borderMarkerSize = new Vector2(16f * Prefs.UIScale, 16f * Prefs.UIScale);
			var viewRect = RealViewRect(borderMarkerSize.x * Settings.clippedBorderDistanceFactor);
			var clippedMarkerMapScale = ClippedMarkerMapScale(borderMarkerSize);
			var observeEdgeUI = EdgeUIInsets.Enabled;

			ClearEdgeBuckets();
			if ((Time.frameCount & 255) == 0)
				MarkerCache.PruneInvalid();

			var visiblePawns = 0;
			var markerDraws = 0;
			var edgeDraws = 0;
			var markerCandidates = 0;
			var flyingPawns = 0;
			for (var thingIndex = 0; thingIndex < drawThings.Count; thingIndex++)
			{
				var markerTarget = drawThings[thingIndex];
				var pawn = MarkerPawnFor(markerTarget);
				if (pawn == null || markerTarget == null || markerTarget.Spawned == false || markerTarget.Map != map)
					continue;

				var decision = MarkerDecisionCache.Get(pawn);
				if (object.ReferenceEquals(decision.markerTarget, markerTarget) == false)
					continue;

				markerCandidates++;
				if (markerTarget is PawnFlyer)
					flyingPawns++;
				DrawPawnMarker(pawn, decision, viewRect, observeEdgeUI, ref visiblePawns, ref markerDraws, ref edgeDraws);
			}

			DrawEdges(clippedMarkerMapScale, viewRect, borderMarkerSize, observeEdgeUI);

			PerfMetrics.Sample("dotdrawer.marker_candidates", markerCandidates);
			PerfMetrics.Sample("dotdrawer.flying_pawns", flyingPawns);
			PerfMetrics.Sample("dotdrawer.visible_pawns", visiblePawns);
			PerfMetrics.Sample("dotdrawer.marker_draws", markerDraws);
			PerfMetrics.Sample("dotdrawer.edge_draws", edgeDraws);
			PerfMetrics.FlushIfNeeded();
		}

		internal static Pawn MarkerPawnFor(Thing markerTarget)
		{
			if (markerTarget is Pawn pawn)
				return pawn;
			if (markerTarget is PawnFlyer flyer)
				return flyer.FlyingPawn;
			return null;
		}

		internal static bool IsRegisteredDrawable(Map map, Thing thing)
		{
			if (map == null || map.Disposed || thing == null || map.dynamicDrawManager?.DrawThings == null)
				return false;

			var drawThings = map.dynamicDrawManager.DrawThings;
			for (var i = 0; i < drawThings.Count; i++)
				if (object.ReferenceEquals(drawThings[i], thing))
					return true;
			return false;
		}

		internal static bool IsMarkerCandidate(Map map, Pawn pawn, Thing markerTarget)
		{
			if (pawn == null || markerTarget == null || markerTarget.Spawned == false || markerTarget.Map != map)
				return false;
			return object.ReferenceEquals(MarkerPawnFor(markerTarget), pawn)
				&& IsRegisteredDrawable(map, markerTarget);
		}

		static void DrawPawnMarker(Pawn pawn, MarkerDecision decision, Rect viewRect, bool observeEdgeUI, ref int visiblePawns, ref int markerDraws, ref int edgeDraws)
		{
			if (pawn == null || decision.hidden || decision.markerTarget == null)
				return;

			visiblePawns++;

			var dotConfig = decision.dotConfig;
			var drawEdge = false;
			var edgeVector = default(Vector2);
			if (decision.edgeEnabled)
			{
				var (vec, clipped) = ConfinedPoint(new Vector2(decision.drawPos.x, decision.drawPos.z), viewRect);
				if (clipped)
				{
					drawEdge = true;
					edgeVector = vec;
				}
			}

			if (drawEdge == false && decision.canDrawInsideMarker == false)
				return;

			if (decision.hasMarkerColors == false)
			{
				PerfMetrics.Count("dotdrawer.skipped_colorless");
				return;
			}

			var useMarkers = DotTools.GetMarkerColors(pawn, dotConfig, out var innerColor, out var outerColor);
			if (useMarkers == false)
				return;

			var materials = MarkerCache.MaterialFor(pawn, dotConfig, decision.canDrawInsideMarker, drawEdge, drawEdge && observeEdgeUI);
			if (materials == null)
				return;

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
				return;

			materials.ApplyColors(innerColor, outerColor);

			Material materialMarker;
			switch (decision.mode)
			{
				case DotStyle.ClassicDots:
					materialMarker = materials.dot;
					if (materialMarker != null)
					{
						markerDraws++;
						DrawMarker(pawn, decision.drawPos, dotConfig, materialMarker);
					}
					break;
				case DotStyle.BetterSilhouettes:
					materialMarker = materials.silhouette;
					if (materialMarker != null)
					{
						markerDraws++;
						DrawMarker(pawn, decision.drawPos, dotConfig, materialMarker);
					}
					break;
				case DotStyle.Custom:
					materialMarker = materials.custom;
					if (materialMarker != null)
					{
						markerDraws++;
						DrawMarker(pawn, decision.drawPos, dotConfig, materialMarker);
					}
					break;
			}
		}

		static void DrawEdges(Vector3 clippedMarkerMapScale, Rect viewRect, Vector2 borderMarkerSize, bool observeEdgeUI)
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
					if (observeEdgeUI)
						ObserveEdgeBounds(command, viewRect, borderMarkerSize);
					altitute += edgeAltitudeStep;
				}
			}

			ClearEdgeBuckets();
		}

		static EdgeScreenEdges ScreenEdges(Vector2 screenCenter, Vector2 visibleSize, float contract)
		{
			var leftDistance = Mathf.Abs(screenCenter.x - contract);
			var rightDistance = Mathf.Abs(screenCenter.x - (UI.screenWidth - contract));
			var horizontalDistance = Mathf.Min(leftDistance, rightDistance);
			var topDistance = Mathf.Abs(screenCenter.y - contract);
			var bottomDistance = Mathf.Abs(screenCenter.y - (UI.screenHeight - contract - bottomMarkerBarOffset));
			var verticalDistance = Mathf.Min(topDistance, bottomDistance);

			var edges = EdgeScreenEdges.None;
			if (horizontalDistance <= verticalDistance + visibleSize.x / 2f)
				edges |= leftDistance <= rightDistance ? EdgeScreenEdges.Left : EdgeScreenEdges.Right;
			if (verticalDistance <= horizontalDistance + visibleSize.y / 2f)
				edges |= topDistance <= bottomDistance ? EdgeScreenEdges.Top : EdgeScreenEdges.Bottom;
			return edges;
		}

		static void ObserveEdgeBounds(EdgeDrawCommand command, Rect viewRect, Vector2 borderMarkerSize)
		{
			var contract = borderMarkerSize.x * Settings.clippedBorderDistanceFactor;
			var screenCenter = new Vector2(
				Mathf.Lerp(contract, UI.screenWidth - contract, Mathf.InverseLerp(viewRect.xMin, viewRect.xMax, command.edgeVector.x)),
				Mathf.Lerp(UI.screenHeight - contract - bottomMarkerBarOffset, contract, Mathf.InverseLerp(viewRect.yMin, viewRect.yMax, command.edgeVector.y)));
			var relativeSize = Mathf.Abs(Settings.clippedRelativeSize * (command.dotConfig?.relativeSize ?? 1f));
			var quadSize = borderMarkerSize * (meshClipped.bounds.size.x * clippedScale * relativeSize);
			var visible = command.materials.edgeVisualBounds.For(command.outlineColor);
			var screenBounds = Rect.MinMaxRect(
				screenCenter.x + (visible.xMin - 0.5f) * quadSize.x,
				screenCenter.y + (visible.yMin - 0.5f) * quadSize.y,
				screenCenter.x + (visible.xMax - 0.5f) * quadSize.x,
				screenCenter.y + (visible.yMax - 0.5f) * quadSize.y);
			var edges = ScreenEdges(screenCenter, screenBounds.size, contract);
			var normalScreenDepth = new Vector2(
				(edges & EdgeScreenEdges.Left) != 0
					? contract + (visible.xMax - 0.5f) * quadSize.x
					: contract - (visible.xMin - 0.5f) * quadSize.x,
				(edges & EdgeScreenEdges.Top) != 0
					? contract + (visible.yMax - 0.5f) * quadSize.y
					: contract + bottomMarkerBarOffset - (visible.yMin - 0.5f) * quadSize.y);
			EdgeUIInsets.ObserveMarker(edges, screenBounds, normalScreenDepth);
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

			if (pawn.RaceProps?.Animal == true && playerFaction)
				return 1;

			if (IsPlayerControlled(pawn))
				return 1;

			if (pawn.HostileTo(Faction.OfPlayer))
				return 2;

			if (pawn.RaceProps?.Animal == true)
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
			var p1 = UI.UIToMapPosition(contract, contract + bottomMarkerBarOffset);
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
			if (materialClipped == null)
				return;

			var pos = vec.ToVector3();
			pos.y = altitute;
			var matrixClipped = Matrix4x4.TRS(pos, Quaternion.identity, scale * clippedScale * Settings.clippedRelativeSize * (dotConfig?.relativeSize ?? 1f));
			Graphics.DrawMesh(meshClipped, matrixClipped, materialClipped, 0);
		}

		private static void DrawMarker(Pawn pawn, Vector3 drawPos, DotConfig dotConfig, Material materialMarker)
		{
			using var measure = PerfMetrics.Measure("DotDrawer.DrawMarker");
			if (pawn == null || materialMarker == null)
				return;

			var q = pawn.health?.Downed == true ? downedRotation : Quaternion.identity;
			var renderer = pawn.Drawer?.renderer;
			var posMarker = drawPos;
			if (renderer != null)
			{
				try
				{
					posMarker = renderer.GetBodyPos(drawPos, pawn.GetPosture(), out _);
				}
				catch (Exception exception)
				{
					WarnRendererFallback(pawn, "position", exception, 219675569);
				}
			}

			var isAnimal = pawn.RaceProps?.Animal == true && pawn.Name != null;
			var miscPlayer = isAnimal == false && pawn.Faction == Faction.OfPlayer && IsColonistPlayerControlled(pawn) == false;
			var drawSize = renderer?.BodyGraphic?.drawSize ?? (miscPlayer ? Vector2.one : SafeDrawSize(pawn));
			if (IsUsableSize(drawSize) == false)
				drawSize = Vector2.one;
			var finalDrawSize = miscPlayer ? 1.5f * drawSize : drawSize;
			var relativeSize = Settings.dotRelativeSize * (dotConfig?.relativeSize ?? 1f);
			if (float.IsNaN(relativeSize) || float.IsInfinity(relativeSize))
				return;
			var matrixMarker = Matrix4x4.TRS(posMarker, q, Vector3.one * Mathf.Pow((finalDrawSize.x + finalDrawSize.y) / 2, 1 / markerSizeScaler) * markerScale * relativeSize);
			var mesh = pawn.Rotation == Rot4.West ? meshWest : meshEast;
			Graphics.DrawMesh(mesh, matrixMarker, materialMarker, 0);
		}

		static Vector2 SafeDrawSize(Pawn pawn)
		{
			try
			{
				return pawn.DrawSize;
			}
			catch (Exception exception)
			{
				WarnRendererFallback(pawn, "draw size", exception, 223257659);
				return Vector2.one;
			}
		}

		static bool IsColonistPlayerControlled(Pawn pawn)
			=> pawn?.health != null
			&& pawn.mindState != null
			&& pawn.IsColonistPlayerControlled;

		static bool IsPlayerControlled(Pawn pawn)
			=> pawn?.health != null
			&& pawn.mindState != null
			&& pawn.IsPlayerControlled;

		static bool IsUsableSize(Vector2 size)
			=> float.IsNaN(size.x) == false
			&& float.IsInfinity(size.x) == false
			&& float.IsNaN(size.y) == false
			&& float.IsInfinity(size.y) == false
			&& size.x > 0f
			&& size.y > 0f;

		static void WarnRendererFallback(Pawn pawn, string operation, Exception exception, int salt)
		{
			var typeName = pawn?.GetType().FullName ?? "unknown pawn type";
			Log.WarningOnce($"CameraPlus could not read the marker {operation} for {typeName}; using a safe fallback: {exception}",
				Gen.HashCombineInt(typeName.GetHashCode(), salt));
		}
	}
}
