using RimWorld;
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

		const float clippedScale = 3f;
		const float markerScale = 2f;
		const float markerSizeScaler = 2f;

		public static void DrawDots(Map map)
		{
			using var measure = PerfMetrics.Measure("DotDrawer.DrawDots");
			PerfMetrics.Count("dotdrawer.draw_calls");
			PerfMetrics.Sample("dotdrawer.all_pawns_spawned", map.mapPawns.AllPawnsSpawned.Count);

			var borderMarkerSize = new Vector2(16f * Prefs.UIScale, 16f * Prefs.UIScale);
			var viewRect = RealViewRect(borderMarkerSize.x * Settings.clippedBorderDistanceFactor);
			var clippedMarkerMapScale = ClippedMarkerMapScale(borderMarkerSize);

			var cellSize = FastUI.CurUICellSize;
			var altitute = AltitudeLayer.Silhouettes.AltitudeFor();
			var visiblePawns = 0;
			var markerDraws = 0;
			var edgeDraws = 0;
			foreach (var pawn in map.mapPawns.AllPawnsSpawned)
			{
				if (Tools.IsHiddenFromPlayer(pawn))
					continue;

				visiblePawns++;
				altitute -= 0.0001f;

				var dotConfig = Caches.dotConfigCache.Get(pawn);
				var useMarkers = DotTools.GetMarkerColors(pawn, dotConfig, out var innerColor, out var outerColor);
				if (useMarkers == false)
					continue;

				var materials = MarkerCache.MaterialFor(pawn, dotConfig);
				if (materials == null)
					continue;
				materials.ApplyColors(innerColor, outerColor);

				var defaultShow = true;
				if (pawn.RaceProps.Animal)
				{
					defaultShow = Settings.customNameStyle != LabelStyle.HideAnimals;
					if (Settings.includeNotTamedAnimals == false && pawn.Name == null && dotConfig == null)
						defaultShow = false;
				}

				if (dotConfig?.useEdge ?? (Settings.edgeIndicators && defaultShow))
				{
					var (vec, clipped) = ConfinedPoint(new Vector2(pawn.DrawPos.x, pawn.DrawPos.z), viewRect);
					if (clipped)
					{
						var materialClipped = materials.dot;
						if (materialClipped != null)
						{
							edgeDraws++;
							DrawClipped(clippedMarkerMapScale, dotConfig, altitute, vec, materialClipped);
						}
					}
				}

				if ((dotConfig?.mode ?? Settings.dotStyle) <= DotStyle.VanillaDefault)
					continue;

				if (dotConfig != null && dotConfig.useInside == false)
					continue;

				if (defaultShow == false)
					continue;

				var dotSize = dotConfig?.showBelowPixels ?? Settings.dotSize;
				if (dotSize == -1)
					dotSize = Settings.dotSize;
				if (cellSize > dotSize)
					continue;

				var mouseReveals = dotConfig?.mouseReveals ?? Settings.mouseOverShowsLabels;
				if (mouseReveals && Tools.MouseDistanceSquared(pawn.DrawPos, true) <= 2.25f) // TODO
					continue;

				Material materialMarker;
				switch (dotConfig?.mode ?? Settings.dotStyle)
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

			PerfMetrics.Sample("dotdrawer.visible_pawns", visiblePawns);
			PerfMetrics.Sample("dotdrawer.marker_draws", markerDraws);
			PerfMetrics.Sample("dotdrawer.edge_draws", edgeDraws);
			PerfMetrics.FlushIfNeeded();
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
