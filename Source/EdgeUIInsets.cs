using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	[Flags]
	internal enum EdgeScreenEdges
	{
		None = 0,
		Left = 1,
		Right = 2,
		Top = 4,
		Bottom = 8
	}

	internal enum EdgeUIArea
	{
		TopLeft,
		BottomLeft,
		TopRight,
		BottomRight
	}

	internal readonly struct EdgeUIInsetChannelState
	{
		public readonly bool uiVisible;
		public readonly bool overlappingMarkers;
		public readonly float uiMinX;
		public readonly float uiMaxX;
		public readonly float uiMinY;
		public readonly float uiMaxY;
		public readonly Vector2 observed;
		public readonly Vector2 target;
		public readonly Vector2 current;

		public EdgeUIInsetChannelState(bool uiVisible, bool overlappingMarkers,
			float uiMinX, float uiMaxX, float uiMinY, float uiMaxY,
			Vector2 observed, Vector2 target, Vector2 current)
		{
			this.uiVisible = uiVisible;
			this.overlappingMarkers = overlappingMarkers;
			this.uiMinX = uiMinX;
			this.uiMaxX = uiMaxX;
			this.uiMinY = uiMinY;
			this.uiMaxY = uiMaxY;
			this.observed = observed;
			this.target = target;
			this.current = current;
		}
	}

	internal readonly struct EdgeUIInsetState
	{
		public readonly bool enabled;
		public readonly int leftMarkerCount;
		public readonly int rightMarkerCount;
		public readonly int topMarkerCount;
		public readonly int bottomMarkerCount;
		public readonly EdgeUIInsetChannelState topLeft;
		public readonly EdgeUIInsetChannelState bottomLeft;
		public readonly EdgeUIInsetChannelState topRight;
		public readonly EdgeUIInsetChannelState bottomRight;

		public EdgeUIInsetState(bool enabled, int leftMarkerCount, int rightMarkerCount,
			int topMarkerCount, int bottomMarkerCount,
			EdgeUIInsetChannelState topLeft, EdgeUIInsetChannelState bottomLeft,
			EdgeUIInsetChannelState topRight, EdgeUIInsetChannelState bottomRight)
		{
			this.enabled = enabled;
			this.leftMarkerCount = leftMarkerCount;
			this.rightMarkerCount = rightMarkerCount;
			this.topMarkerCount = topMarkerCount;
			this.bottomMarkerCount = bottomMarkerCount;
			this.topLeft = topLeft;
			this.bottomLeft = bottomLeft;
			this.topRight = topRight;
			this.bottomRight = bottomRight;
		}
	}

	internal static class EdgeUIInsets
	{
		const float partiallyOffscreenClearance = 6f;
		const float noMarkerHoldSeconds = 0.15f;
		const float expandSmoothTime = 0.10f / 1.5f;
		const float retractSmoothTime = 0.20f / 1.5f;
		const float settledDistance = 0.05f;
		const float mouseoverLineHeight = 19f;
		const float mouseoverBottom = 65f;
		const float mouseoverRight = 256f;
		internal const float MainButtonsHeight = 35f;
		internal const float GizmoBottomSpacing = 14f;
		const float minimumRemainingUI = 80f;

		static readonly System.Collections.Generic.List<EdgeMarkerBounds> markers = new System.Collections.Generic.List<EdgeMarkerBounds>(32);
		static readonly UIObservation[] uiObservations = new UIObservation[4];
		static readonly AnimatedInset[] channels =
		{
			new AnimatedInset(),
			new AnimatedInset(),
			new AnimatedInset(),
			new AnimatedInset()
		};

		static int markerObservationFrame = -1;
		static int leftMarkerCount;
		static int rightMarkerCount;
		static int topMarkerCount;
		static int bottomMarkerCount;
		static int animationFrame = -1;
		static float lastAnimationTime = -1f;

		internal static void ObserveMarker(EdgeScreenEdges edges, Rect screenBounds, Vector2 normalScreenDepth)
		{
			if (edges == EdgeScreenEdges.None || Settings?.indentVanillaUIForEdgeMarkers != true)
				return;

			var frame = Time.frameCount;
			if (markerObservationFrame != frame)
			{
				markerObservationFrame = frame;
				markers.Clear();
				leftMarkerCount = 0;
				rightMarkerCount = 0;
				topMarkerCount = 0;
				bottomMarkerCount = 0;
			}

			var xMin = Mathf.Clamp(screenBounds.xMin, 0f, UI.screenWidth);
			var xMax = Mathf.Clamp(screenBounds.xMax, 0f, UI.screenWidth);
			var yMin = Mathf.Clamp(screenBounds.yMin, 0f, UI.screenHeight);
			var yMax = Mathf.Clamp(screenBounds.yMax, 0f, UI.screenHeight);
			if (xMax <= xMin || yMax <= yMin)
				return;

			var outsideEdges = EdgeScreenEdges.None;
			if (screenBounds.xMin < 0f)
				outsideEdges |= EdgeScreenEdges.Left;
			if (screenBounds.xMax > UI.screenWidth)
				outsideEdges |= EdgeScreenEdges.Right;
			if (screenBounds.yMin < 0f)
				outsideEdges |= EdgeScreenEdges.Top;
			if (screenBounds.yMax > UI.screenHeight)
				outsideEdges |= EdgeScreenEdges.Bottom;

			markers.Add(new EdgeMarkerBounds(
				edges,
				outsideEdges,
				Rect.MinMaxRect(xMin, yMin, xMax, yMax),
				normalScreenDepth));
			if ((edges & EdgeScreenEdges.Left) != 0)
				leftMarkerCount++;
			if ((edges & EdgeScreenEdges.Right) != 0)
				rightMarkerCount++;
			if ((edges & EdgeScreenEdges.Top) != 0)
				topMarkerCount++;
			if ((edges & EdgeScreenEdges.Bottom) != 0)
				bottomMarkerCount++;
		}

		internal static void ObserveUI(EdgeUIArea area, float xMin, float xMax, float yMin, float yMax)
		{
			xMin = Mathf.Clamp(xMin, 0f, UI.screenWidth);
			xMax = Mathf.Clamp(xMax, 0f, UI.screenWidth);
			yMin = Mathf.Clamp(yMin, 0f, UI.screenHeight);
			yMax = Mathf.Clamp(yMax, 0f, UI.screenHeight);
			if (xMax <= xMin || yMax <= yMin)
				return;

			ref var observation = ref uiObservations[(int)area];
			var frame = Time.frameCount;
			if (observation.frame != frame)
				observation = new UIObservation(frame, xMin, xMax, yMin, yMax);
			else
			{
				observation.xMin = Mathf.Min(observation.xMin, xMin);
				observation.xMax = Mathf.Max(observation.xMax, xMax);
				observation.yMin = Mathf.Min(observation.yMin, yMin);
				observation.yMax = Mathf.Max(observation.yMax, yMax);
			}
		}

		internal static Matrix4x4 Push(EdgeUIArea area)
		{
			UpdateAnimation();
			var previous = GUI.matrix;
			var channel = channels[(int)area];
			if (Mathf.Abs(channel.horizontal.current) > settledDistance || Mathf.Abs(channel.vertical.current) > settledDistance)
			{
				var horizontalDirection = IsLeft(area) ? 1f : -1f;
				var verticalDirection = IsTop(area) ? 1f : -1f;
				GUI.matrix = previous * Matrix4x4.Translate(new Vector3(
					horizontalDirection * channel.horizontal.current,
					verticalDirection * channel.vertical.current,
					0f));
			}
			return previous;
		}

		internal static Exception Restore(Exception exception, Matrix4x4 previous)
		{
			GUI.matrix = previous;
			return exception;
		}

		internal static EdgeUIInsetState State()
		{
			UpdateAnimation();
			return new EdgeUIInsetState(
				Settings?.indentVanillaUIForEdgeMarkers == true,
				markerObservationFrame >= Time.frameCount - 1 ? leftMarkerCount : 0,
				markerObservationFrame >= Time.frameCount - 1 ? rightMarkerCount : 0,
				markerObservationFrame >= Time.frameCount - 1 ? topMarkerCount : 0,
				markerObservationFrame >= Time.frameCount - 1 ? bottomMarkerCount : 0,
				ChannelState(EdgeUIArea.TopLeft),
				ChannelState(EdgeUIArea.BottomLeft),
				ChannelState(EdgeUIArea.TopRight),
				ChannelState(EdgeUIArea.BottomRight));
		}

		internal static void ObserveMouseoverReadout()
		{
			if (Event.current.type != EventType.Repaint || Find.MainTabsRoot.OpenTab != null)
				return;

			var rows = MouseoverRows();
			if (rows <= 0)
				return;

			var firstLineY = UI.screenHeight - mouseoverBottom;
			ObserveUI(
				EdgeUIArea.BottomLeft,
				15f,
				Mathf.Min(mouseoverRight, UI.screenWidth),
				firstLineY - (rows - 1) * mouseoverLineHeight,
				firstLineY + mouseoverLineHeight);
		}

		static int MouseoverRows()
		{
			var map = Find.CurrentMap;
			if (map == null)
				return 0;

			var cell = UI.MouseCell();
			if (cell.InBounds(map) == false)
				return 0;
			if (cell.Fogged(map))
				return 1;

			var rows = 2;
			if (map.Biome.inVacuum)
				rows++;
			if (cell.GetZone(map) != null)
				rows++;
			if (map.snowGrid.GetDepth(cell) > 0.03f)
				rows++;
			if (ModsConfig.OdysseyActive && map.sandGrid.GetDepth(cell) > 0.03f)
				rows++;

			var things = cell.GetThingList(map);
			for (var i = 0; i < things.Count; i++)
			{
				var thing = things[i];
				var proxy = thing.TryGetComp<CompSelectProxy>();
				if (proxy?.thingToSelect != null)
					thing = proxy.thingToSelect;
				if (thing.def.category != ThingCategory.Mote && (!(thing is Pawn pawn) || pawn.IsHiddenFromPlayer() == false))
					rows++;
			}

			if (cell.GetRoof(map) != null)
				rows++;
			if (map.gasGrid.AnyGasAt(cell))
			{
				if (map.gasGrid.DensityAt(cell, GasType.BlindSmoke) > 0)
					rows++;
				if (map.gasGrid.DensityAt(cell, GasType.ToxGas) > 0)
					rows++;
				if (map.gasGrid.DensityAt(cell, GasType.RotStink) > 0)
					rows++;
				if (map.gasGrid.DensityAt(cell, GasType.DeadlifeDust) > 0)
					rows++;
			}
			if (ModsConfig.OdysseyActive && map.waterBodyTracker.TryGetWaterBodyAt(cell, out var body) && body.HasFish)
				rows++;
			return rows;
		}

		static EdgeUIInsetChannelState ChannelState(EdgeUIArea area)
		{
			var channel = channels[(int)area];
			var observation = uiObservations[(int)area];
			var uiVisible = observation.frame >= Time.frameCount - 1;
			var observed = new Vector2(channel.horizontal.observed, channel.vertical.observed);
			var target = new Vector2(channel.horizontal.target, channel.vertical.target);
			var current = new Vector2(channel.horizontal.current, channel.vertical.current);
			return new EdgeUIInsetChannelState(
				uiVisible,
				observed.x > 0f || observed.y > 0f,
				uiVisible ? observation.xMin : 0f,
				uiVisible ? observation.xMax : 0f,
				uiVisible ? observation.yMin : 0f,
				uiVisible ? observation.yMax : 0f,
				observed,
				target,
				current);
		}

		static void UpdateAnimation()
		{
			var frame = Time.frameCount;
			if (animationFrame == frame)
				return;

			animationFrame = frame;
			var now = Time.realtimeSinceStartup;
			var deltaTime = lastAnimationTime < 0f ? 0f : Mathf.Clamp(now - lastAnimationTime, 0f, 0.1f);
			lastAnimationTime = now;
			var enabled = Settings?.indentVanillaUIForEdgeMarkers == true && skipCustomRendering == false;

			for (var i = 0; i < channels.Length; i++)
			{
				var channel = channels[i];
				var observed = OverlappingInset((EdgeUIArea)i);
				UpdateAxis(channel.horizontal, observed.x, enabled, now, deltaTime);
				UpdateAxis(channel.vertical, observed.y, enabled, now, deltaTime);
			}
		}

		static void UpdateAxis(AnimatedAxis axis, float observed, bool enabled, float now, float deltaTime)
		{
			axis.observed = observed;
			if (observed > 0f)
			{
				axis.recent = observed;
				axis.lastSeen = now;
			}
			axis.target = enabled
				? observed > 0f
					? observed
					: now - axis.lastSeen <= noMarkerHoldSeconds ? axis.recent : 0f
				: 0f;
			axis.current = Smooth(axis.current, axis.target, ref axis.velocity, deltaTime);
		}

		static Vector2 OverlappingInset(EdgeUIArea area)
		{
			var ui = uiObservations[(int)area];
			if (ui.frame < Time.frameCount - 1 || markerObservationFrame < Time.frameCount - 1)
				return Vector2.zero;

			var horizontalEdge = IsLeft(area)
				? EdgeScreenEdges.Left
				: EdgeScreenEdges.Right;
			var verticalEdge = IsTop(area) ? EdgeScreenEdges.Top : EdgeScreenEdges.Bottom;
			var inset = Vector2.zero;
			for (var i = 0; i < markers.Count; i++)
			{
				var marker = markers[i];
				var horizontalClearance = (marker.outsideEdges & horizontalEdge) != 0 ? partiallyOffscreenClearance : 0f;
				var horizontalWouldOverlap = horizontalEdge == EdgeScreenEdges.Left
					? marker.bounds.xMax + horizontalClearance > ui.xMin
					: marker.bounds.xMin - horizontalClearance < ui.xMax;
				if ((marker.edges & horizontalEdge) != 0
					&& marker.bounds.yMax > ui.yMin
					&& marker.bounds.yMin < ui.yMax
					&& horizontalWouldOverlap)
				{
					var horizontal = horizontalEdge == EdgeScreenEdges.Left
						? marker.bounds.xMax
						: UI.screenWidth - marker.bounds.xMin;
					horizontal = Mathf.Min(horizontal, marker.normalScreenDepth.x) + horizontalClearance;
					inset.x = Mathf.Max(inset.x, horizontal);
				}

				var verticalClearance = (marker.outsideEdges & verticalEdge) != 0 ? partiallyOffscreenClearance : 0f;
				var verticalWouldOverlap = verticalEdge == EdgeScreenEdges.Top
					? marker.bounds.yMax + verticalClearance > ui.yMin
					: marker.bounds.yMin - verticalClearance < ui.yMax;
				if ((marker.edges & verticalEdge) != 0
					&& marker.bounds.xMax > ui.xMin
					&& marker.bounds.xMin < ui.xMax
					&& verticalWouldOverlap)
				{
					var verticalScreenDepth = verticalEdge == EdgeScreenEdges.Top
						? marker.bounds.yMax
						: UI.screenHeight - marker.bounds.yMin;
					verticalScreenDepth = Mathf.Min(verticalScreenDepth, marker.normalScreenDepth.y);
					var vertical = verticalEdge == EdgeScreenEdges.Top
						? verticalScreenDepth
						: verticalScreenDepth - MainButtonsHeight;
					vertical += verticalClearance;
					inset.y = Mathf.Max(inset.y, vertical);
				}
			}
			inset.x = Mathf.Clamp(inset.x, 0f, Mathf.Max(0f, UI.screenWidth - minimumRemainingUI));
			inset.y = Mathf.Clamp(inset.y, 0f, Mathf.Max(0f, UI.screenHeight - minimumRemainingUI));
			return inset;
		}

		static bool IsLeft(EdgeUIArea area) => area == EdgeUIArea.TopLeft || area == EdgeUIArea.BottomLeft;

		static bool IsTop(EdgeUIArea area) => area == EdgeUIArea.TopLeft || area == EdgeUIArea.TopRight;

		static float Smooth(float current, float target, ref float velocity, float deltaTime)
		{
			if (deltaTime <= 0f)
				return current;
			var smoothTime = target > current ? expandSmoothTime : retractSmoothTime;
			var result = Mathf.SmoothDamp(current, target, ref velocity, smoothTime, Mathf.Infinity, deltaTime);
			if (target == 0f && result < settledDistance)
			{
				velocity = 0f;
				return 0f;
			}
			return result;
		}

		readonly struct EdgeMarkerBounds
		{
			public readonly EdgeScreenEdges edges;
			public readonly EdgeScreenEdges outsideEdges;
			public readonly Rect bounds;
			public readonly Vector2 normalScreenDepth;

			public EdgeMarkerBounds(EdgeScreenEdges edges, EdgeScreenEdges outsideEdges, Rect bounds, Vector2 normalScreenDepth)
			{
				this.edges = edges;
				this.outsideEdges = outsideEdges;
				this.bounds = bounds;
				this.normalScreenDepth = normalScreenDepth;
			}
		}

		struct UIObservation
		{
			public int frame;
			public float xMin;
			public float xMax;
			public float yMin;
			public float yMax;

			public UIObservation(int frame, float xMin, float xMax, float yMin, float yMax)
			{
				this.frame = frame;
				this.xMin = xMin;
				this.xMax = xMax;
				this.yMin = yMin;
				this.yMax = yMax;
			}
		}

		sealed class AnimatedInset
		{
			public readonly AnimatedAxis horizontal = new AnimatedAxis();
			public readonly AnimatedAxis vertical = new AnimatedAxis();
		}

		sealed class AnimatedAxis
		{
			public float observed;
			public float recent;
			public float lastSeen = -100f;
			public float target;
			public float current;
			public float velocity;
		}
	}

	[HarmonyPatch(typeof(ResourceReadout), nameof(ResourceReadout.ResourceReadoutOnGUI))]
	static class ResourceReadoutEdgeInsetPatch
	{
		[HarmonyPriority(Priority.First)]
		static void Prefix(out Matrix4x4 __state) => __state = EdgeUIInsets.Push(EdgeUIArea.TopLeft);

		[HarmonyPriority(Priority.Last)]
		static void Postfix(float ___lastDrawnHeight)
		{
			if (Event.current.type == EventType.Layout || Current.ProgramState != ProgramState.Playing || Find.MainTabsRoot.OpenTab == MainButtonDefOf.Menu)
				return;
			var x = Prefs.ResourceReadoutCategorized ? 2f : 7f;
			var width = Prefs.ResourceReadoutCategorized ? 124f : 110f;
			EdgeUIInsets.ObserveUI(
				EdgeUIArea.TopLeft,
				x,
				x + width,
				7f,
				7f + Mathf.Min(___lastDrawnHeight, UI.screenHeight - 207f));
		}

		[HarmonyPriority(Priority.Last)]
		static Exception Finalizer(Exception __exception, Matrix4x4 __state) => EdgeUIInsets.Restore(__exception, __state);
	}

	[HarmonyPatch(typeof(MouseoverReadout), nameof(MouseoverReadout.MouseoverReadoutOnGUI))]
	static class MouseoverReadoutEdgeInsetPatch
	{
		[HarmonyPriority(Priority.First)]
		static void Prefix(out Matrix4x4 __state) => __state = EdgeUIInsets.Push(EdgeUIArea.BottomLeft);

		[HarmonyPriority(Priority.Last)]
		static void Postfix() => EdgeUIInsets.ObserveMouseoverReadout();

		[HarmonyPriority(Priority.Last)]
		static Exception Finalizer(Exception __exception, Matrix4x4 __state) => EdgeUIInsets.Restore(__exception, __state);
	}

	[HarmonyPatch(typeof(GlobalControls), nameof(GlobalControls.GlobalControlsOnGUI))]
	static class GlobalControlsEdgeInsetPatch
	{
		[HarmonyPriority(Priority.First)]
		static void Prefix(out Matrix4x4 __state) => __state = EdgeUIInsets.Push(EdgeUIArea.BottomRight);

		[HarmonyPriority(Priority.Last)]
		static void Postfix()
		{
			if (Event.current.type != EventType.Layout)
				EdgeUIInsets.ObserveUI(
					EdgeUIArea.BottomRight,
					Mathf.Max(0f, UI.screenWidth - 300f),
					UI.screenWidth,
					Find.LetterStack.LastTopY,
					UI.screenHeight - EdgeUIInsets.MainButtonsHeight);
		}

		[HarmonyPriority(Priority.Last)]
		static Exception Finalizer(Exception __exception, Matrix4x4 __state) => EdgeUIInsets.Restore(__exception, __state);
	}

	[HarmonyPatch(typeof(AlertsReadout), nameof(AlertsReadout.AlertsReadoutOnGUI))]
	static class AlertsReadoutEdgeInsetPatch
	{
		[HarmonyPriority(Priority.First)]
		static void Prefix(out Matrix4x4 __state) => __state = EdgeUIInsets.Push(EdgeUIArea.BottomRight);

		[HarmonyPriority(Priority.Last)]
		static void Postfix(AlertsReadout __instance, float ___lastFinalY)
		{
			if (Event.current.type == EventType.Layout || Event.current.type == EventType.MouseDrag)
				return;
			var height = __instance.AlertsHeight;
			if (height > 0f)
				EdgeUIInsets.ObserveUI(
					EdgeUIArea.BottomRight,
					Mathf.Max(0f, UI.screenWidth - 154f),
					UI.screenWidth,
					Find.LetterStack.LastTopY - height,
					___lastFinalY);
		}

		[HarmonyPriority(Priority.Last)]
		static Exception Finalizer(Exception __exception, Matrix4x4 __state) => EdgeUIInsets.Restore(__exception, __state);
	}

	[HarmonyPatch(typeof(MapGizmoUtility), nameof(MapGizmoUtility.MapUIOnGUI))]
	static class MapGizmoEdgeInsetPatch
	{
		[HarmonyPriority(Priority.First)]
		static void Prefix(out Matrix4x4 __state) => __state = EdgeUIInsets.Push(EdgeUIArea.BottomRight);

		[HarmonyPriority(Priority.Last)]
		static void Postfix()
		{
			var height = GizmoGridDrawer.HeightDrawnRecently;
			if (height > 0f)
			{
				var x = 14f;
				var inspectPane = Find.WindowStack.WindowOfType<IInspectPane>();
				if (inspectPane != null)
					x += InspectPaneUtility.PaneWidthFor(inspectPane);
				EdgeUIInsets.ObserveUI(
					EdgeUIArea.BottomRight,
					x,
					Mathf.Max(x, UI.screenWidth - 147f),
					UI.screenHeight - height,
					UI.screenHeight - EdgeUIInsets.MainButtonsHeight - EdgeUIInsets.GizmoBottomSpacing);
			}
		}

		[HarmonyPriority(Priority.Last)]
		static Exception Finalizer(Exception __exception, Matrix4x4 __state) => EdgeUIInsets.Restore(__exception, __state);
	}
}
