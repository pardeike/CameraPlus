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
			var borderMarkerSize = new Vector2(16f * Prefs.UIScale, 16f * Prefs.UIScale);
			var viewRect = RealViewRect(borderMarkerSize.x * Settings.clippedBorderDistanceFactor);

			var cellSize = FastUI.CurUICellSize;
			var altitute = AltitudeLayer.Silhouettes.AltitudeFor();
			var pawnsToRender = map.mapPawns.AllPawnsSpawned.Where(pawn => Tools.IsHiddenFromPlayer(pawn) == false);
			foreach (var pawn in pawnsToRender)
			{
				altitute -= 0.0001f;

				var useMarkers = DotTools.GetMarkerColors(pawn, out var innerColor, out var outerColor);
				if (useMarkers == false)
					continue;

				var materials = MarkerCache.MaterialFor(pawn);
				if (materials == null)
					continue;

				var dotConfig = pawn.GetDotConfig();

				var defaultShow = true;
				if (pawn.RaceProps.Animal)
				{
					defaultShow = Settings.customNameStyle != LabelStyle.HideAnimals;
					if (Settings.includeNotTamedAnimals == false && pawn.Name == null)
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
							materialClipped.SetColor("_FillColor", innerColor);
							materialClipped.SetColor("_OutlineColor", outerColor);
							DrawClipped(borderMarkerSize, dotConfig, altitute, vec, materialClipped);
						}
					}
				}

				if (Settings.dotStyle == DotStyle.VanillaDefault)
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
						materialMarker.SetColor("_FillColor", innerColor);
						materialMarker.SetColor("_OutlineColor", outerColor);
						DrawMarker(pawn, dotConfig, materialMarker);
						break;
					case DotStyle.BetterSilhouettes:
						materialMarker = materials.silhouette;
						materialMarker.SetColor("_FillColor", innerColor);
						materialMarker.SetColor("_OutlineColor", outerColor);
						DrawMarker(pawn, dotConfig, materialMarker);
						break;
				}
			}
		}

		private static Rect RealViewRect(float contract)
		{
			var p1 = UI.UIToMapPosition(contract, contract + 36); // 36 is bottom bar
			var wh = UI.UIToMapPosition(UI.screenWidth - contract, UI.screenHeight - contract) - p1;
			return new Rect(p1.x, p1.z, wh.x, wh.z);
		}

		private static (Vector2 vector, bool clipped) ConfinedPoint(Vector2 p, Rect r)
		{
			var center = new Vector2(r.x + r.width / 2, r.y + r.height / 2);

			if (r.Contains(p))
				return (p, false);

			var direction = (p - center).normalized;
			var dx = direction.x;
			var dy = direction.y;

			var t = float.MaxValue;

			if (dx != 0)
			{
				if (dx > 0)
				{
					var t1 = (r.xMax - center.x) / dx;
					if (t1 > 0 && center.y + t1 * dy >= r.yMin && center.y + t1 * dy <= r.yMax)
						t = Mathf.Min(t, t1);
				}
				else
				{
					var t1 = (r.xMin - center.x) / dx;
					if (t1 > 0 && center.y + t1 * dy >= r.yMin && center.y + t1 * dy <= r.yMax)
						t = Mathf.Min(t, t1);
				}
			}

			if (dy != 0)
			{
				if (dy > 0)
				{
					var t1 = (r.yMax - center.y) / dy;
					if (t1 > 0 && center.x + t1 * dx >= r.xMin && center.x + t1 * dx <= r.xMax)
						t = Mathf.Min(t, t1);
				}
				else
				{
					var t1 = (r.yMin - center.y) / dy;
					if (t1 > 0 && center.x + t1 * dx >= r.xMin && center.x + t1 * dx <= r.xMax)
						t = Mathf.Min(t, t1);
				}
			}

			return (center + t * direction, true);
		}

		private static void DrawClipped(Vector2 size, DotConfig dotConfig, float altitute, Vector2 vec, Material materialClipped)
		{
			var v2 = UI.MapToUIPosition(vec);
			var rect = new Rect(v2.x - size.x / 2, v2.y - size.y / 2, size.x, size.y);
			var p1 = UI.UIToMapPosition(new Vector2(rect.xMin, rect.yMin));
			var p2 = UI.UIToMapPosition(new Vector2(rect.xMax, rect.yMax));
			var scale = p2 - p1;
			var pos = vec.ToVector3();
			pos.y = altitute;
			var matrixClipped = Matrix4x4.TRS(pos, Quaternion.identity, scale * clippedScale * Settings.clippedRelativeSize * (dotConfig?.relativeSize ?? 1f));
			Graphics.DrawMesh(meshClipped, matrixClipped, materialClipped, 0);
		}

		private static void DrawMarker(Pawn pawn, DotConfig dotConfig, Material materialMarker)
		{
			var q = pawn.Downed ? downedRotation : Quaternion.identity;
			var posMarker = pawn.Drawer.renderer.GetBodyPos(pawn.DrawPos, pawn.GetPosture(), out _);
			_ = pawn.Drawer.renderer.renderTree.nodesByTag.TryGetValue(PawnRenderNodeTagDefOf.Body, out var bodyNode);
			var isAnimal = pawn.RaceProps.Animal && pawn.Name != null;
			var miscPlayer = isAnimal == false && pawn.Faction == Faction.OfPlayer && pawn.IsColonistPlayerControlled == false;
			var size = miscPlayer ? 1.5f * Vector2.one : (bodyNode?.Graphic ?? pawn.Graphic)?.drawSize ?? pawn.DrawSize;
			var relativeSize = Settings.dotRelativeSize * (dotConfig?.relativeSize ?? 1f);
			var matrixMarker = Matrix4x4.TRS(posMarker, q, Vector3.one * Mathf.Pow((size.x + size.y) / 2, 1 / markerSizeScaler) * markerScale * relativeSize);
			var mesh = pawn.Rotation == Rot4.West ? meshWest : meshEast;
			Graphics.DrawMesh(mesh, matrixMarker, materialMarker, 0);
		}
	}
}