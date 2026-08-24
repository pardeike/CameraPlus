using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	internal enum EdgeScreenSide
	{
		None,
		Left,
		Right
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
		public readonly float uiMinY;
		public readonly float uiMaxY;
		public readonly float observed;
		public readonly float target;
		public readonly float current;

		public EdgeUIInsetChannelState(bool uiVisible, bool overlappingMarkers, float uiMinY, float uiMaxY,
			float observed, float target, float current)
		{
			this.uiVisible = uiVisible;
			this.overlappingMarkers = overlappingMarkers;
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
		public readonly EdgeUIInsetChannelState topLeft;
		public readonly EdgeUIInsetChannelState bottomLeft;
		public readonly EdgeUIInsetChannelState topRight;
		public readonly EdgeUIInsetChannelState bottomRight;

		public EdgeUIInsetState(bool enabled, int leftMarkerCount, int rightMarkerCount,
			EdgeUIInsetChannelState topLeft, EdgeUIInsetChannelState bottomLeft,
			EdgeUIInsetChannelState topRight, EdgeUIInsetChannelState bottomRight)
		{
			this.enabled = enabled;
			this.leftMarkerCount = leftMarkerCount;
			this.rightMarkerCount = rightMarkerCount;
			this.topLeft = topLeft;
			this.bottomLeft = bottomLeft;
			this.topRight = topRight;
			this.bottomRight = bottomRight;
		}
	}

	internal static class EdgeUIInsets
	{
		const float noMarkerHoldSeconds = 0.15f;
		const float expandSmoothTime = 0.10f / 1.5f;
		const float retractSmoothTime = 0.20f / 1.5f;
		const float settledDistance = 0.05f;
		const float mouseoverLineHeight = 19f;
		const float mouseoverBottom = 65f;

		static readonly System.Collections.Generic.List<EdgeMarkerSpan> leftMarkers = new System.Collections.Generic.List<EdgeMarkerSpan>(16);
		static readonly System.Collections.Generic.List<EdgeMarkerSpan> rightMarkers = new System.Collections.Generic.List<EdgeMarkerSpan>(16);
		static readonly UIObservation[] uiObservations = new UIObservation[4];
		static readonly AnimatedInset[] channels =
		{
			new AnimatedInset(),
			new AnimatedInset(),
			new AnimatedInset(),
			new AnimatedInset()
		};

		static int markerObservationFrame = -1;
		static int animationFrame = -1;
		static float lastAnimationTime = -1f;

		internal static void ObserveMarker(EdgeScreenSide side, Rect screenBounds)
		{
			if (side == EdgeScreenSide.None || Settings?.indentVanillaUIForEdgeMarkers != true)
				return;

			var frame = Time.frameCount;
			if (markerObservationFrame != frame)
			{
				markerObservationFrame = frame;
				leftMarkers.Clear();
				rightMarkers.Clear();
			}

			var inset = side == EdgeScreenSide.Left
				? screenBounds.xMax
				: UI.screenWidth - screenBounds.xMin;
			inset = Mathf.Clamp(inset, 0f, Mathf.Max(0f, UI.screenWidth - 80f));
			var yMin = Mathf.Clamp(screenBounds.yMin, 0f, UI.screenHeight);
			var yMax = Mathf.Clamp(screenBounds.yMax, 0f, UI.screenHeight);
			if (inset <= 0f || yMax <= yMin)
				return;

			var span = new EdgeMarkerSpan(yMin, yMax, inset);
			(side == EdgeScreenSide.Left ? leftMarkers : rightMarkers).Add(span);
		}

		internal static void ObserveUI(EdgeUIArea area, float yMin, float yMax)
		{
			yMin = Mathf.Clamp(yMin, 0f, UI.screenHeight);
			yMax = Mathf.Clamp(yMax, 0f, UI.screenHeight);
			if (yMax <= yMin)
				return;

			ref var observation = ref uiObservations[(int)area];
			var frame = Time.frameCount;
			if (observation.frame != frame)
				observation = new UIObservation(frame, yMin, yMax);
			else
			{
				observation.yMin = Mathf.Min(observation.yMin, yMin);
				observation.yMax = Mathf.Max(observation.yMax, yMax);
			}
		}

		internal static Matrix4x4 Push(EdgeUIArea area)
		{
			UpdateAnimation();
			var previous = GUI.matrix;
			var inset = channels[(int)area].current;
			if (Mathf.Abs(inset) > settledDistance)
			{
				var direction = area == EdgeUIArea.TopLeft || area == EdgeUIArea.BottomLeft ? 1f : -1f;
				GUI.matrix = previous * Matrix4x4.Translate(new Vector3(direction * inset, 0f, 0f));
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
				markerObservationFrame >= Time.frameCount - 1 ? leftMarkers.Count : 0,
				markerObservationFrame >= Time.frameCount - 1 ? rightMarkers.Count : 0,
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
			return new EdgeUIInsetChannelState(
				uiVisible,
				channel.observed > 0f,
				uiVisible ? observation.yMin : 0f,
				uiVisible ? observation.yMax : 0f,
				channel.observed,
				channel.target,
				channel.current);
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
				channel.observed = OverlappingInset((EdgeUIArea)i);
				if (channel.observed > 0f)
				{
					channel.recent = channel.observed;
					channel.lastSeen = now;
				}
				channel.target = enabled
					? channel.observed > 0f
						? channel.observed
						: now - channel.lastSeen <= noMarkerHoldSeconds ? channel.recent : 0f
					: 0f;
				channel.current = Smooth(channel.current, channel.target, ref channel.velocity, deltaTime);
			}
		}

		static float OverlappingInset(EdgeUIArea area)
		{
			var ui = uiObservations[(int)area];
			if (ui.frame < Time.frameCount - 1 || markerObservationFrame < Time.frameCount - 1)
				return 0f;

			var markers = area == EdgeUIArea.TopLeft || area == EdgeUIArea.BottomLeft ? leftMarkers : rightMarkers;
			var inset = 0f;
			for (var i = 0; i < markers.Count; i++)
			{
				var marker = markers[i];
				if (marker.yMax > ui.yMin && marker.yMin < ui.yMax)
					inset = Mathf.Max(inset, marker.inset);
			}
			return inset;
		}

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

		readonly struct EdgeMarkerSpan
		{
			public readonly float yMin;
			public readonly float yMax;
			public readonly float inset;

			public EdgeMarkerSpan(float yMin, float yMax, float inset)
			{
				this.yMin = yMin;
				this.yMax = yMax;
				this.inset = inset;
			}
		}

		struct UIObservation
		{
			public int frame;
			public float yMin;
			public float yMax;

			public UIObservation(int frame, float yMin, float yMax)
			{
				this.frame = frame;
				this.yMin = yMin;
				this.yMax = yMax;
			}
		}

		sealed class AnimatedInset
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
			EdgeUIInsets.ObserveUI(EdgeUIArea.TopLeft, 7f, 7f + Mathf.Min(___lastDrawnHeight, UI.screenHeight - 207f));
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
				EdgeUIInsets.ObserveUI(EdgeUIArea.BottomRight, Find.LetterStack.LastTopY, UI.screenHeight);
		}

		[HarmonyPriority(Priority.Last)]
		static Exception Finalizer(Exception __exception, Matrix4x4 __state) => EdgeUIInsets.Restore(__exception, __state);
	}

	[HarmonyPatch(typeof(AlertsReadout), nameof(AlertsReadout.AlertsReadoutOnGUI))]
	static class AlertsReadoutEdgeInsetPatch
	{
		[HarmonyPriority(Priority.First)]
		static void Prefix(out Matrix4x4 __state) => __state = EdgeUIInsets.Push(EdgeUIArea.TopRight);

		[HarmonyPriority(Priority.Last)]
		static void Postfix(AlertsReadout __instance, float ___lastFinalY)
		{
			if (Event.current.type == EventType.Layout || Event.current.type == EventType.MouseDrag)
				return;
			var height = __instance.AlertsHeight;
			if (height > 0f)
				EdgeUIInsets.ObserveUI(EdgeUIArea.TopRight, Find.LetterStack.LastTopY - height, ___lastFinalY);
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
				EdgeUIInsets.ObserveUI(EdgeUIArea.BottomRight, UI.screenHeight - height, UI.screenHeight);
		}

		[HarmonyPriority(Priority.Last)]
		static Exception Finalizer(Exception __exception, Matrix4x4 __state) => EdgeUIInsets.Restore(__exception, __state);
	}
}
