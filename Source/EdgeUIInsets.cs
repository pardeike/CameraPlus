using System;
using System.Collections.Generic;
using System.Reflection;
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

	internal readonly struct EdgeUIInsetState
	{
		public readonly bool enabled;
		public readonly bool leftMarkers;
		public readonly bool rightMarkers;
		public readonly float leftObserved;
		public readonly float rightObserved;
		public readonly float leftTarget;
		public readonly float rightTarget;
		public readonly float leftCurrent;
		public readonly float rightCurrent;

		public EdgeUIInsetState(bool enabled, bool leftMarkers, bool rightMarkers, float leftObserved, float rightObserved,
			float leftTarget, float rightTarget, float leftCurrent, float rightCurrent)
		{
			this.enabled = enabled;
			this.leftMarkers = leftMarkers;
			this.rightMarkers = rightMarkers;
			this.leftObserved = leftObserved;
			this.rightObserved = rightObserved;
			this.leftTarget = leftTarget;
			this.rightTarget = rightTarget;
			this.leftCurrent = leftCurrent;
			this.rightCurrent = rightCurrent;
		}
	}

	internal static class EdgeUIInsets
	{
		const float noMarkerHoldSeconds = 0.15f;
		const float expandSmoothTime = 0.10f;
		const float retractSmoothTime = 0.20f;
		const float settledDistance = 0.05f;

		static int observationFrame = -1;
		static int animationFrame = -1;
		static float observationLeft;
		static float observationRight;
		static float recentLeft;
		static float recentRight;
		static float lastLeftSeen = -100f;
		static float lastRightSeen = -100f;
		static float lastAnimationTime = -1f;
		static float targetLeft;
		static float targetRight;
		static float currentLeft;
		static float currentRight;
		static float velocityLeft;
		static float velocityRight;

		internal static void Observe(EdgeScreenSide side, float screenMinX, float screenMaxX)
		{
			if (side == EdgeScreenSide.None || Settings?.indentVanillaUIForEdgeMarkers != true)
				return;

			var frame = Time.frameCount;
			if (observationFrame != frame)
			{
				observationFrame = frame;
				observationLeft = 0f;
				observationRight = 0f;
			}

			var maxInset = Mathf.Max(0f, UI.screenWidth - 80f);
			var now = Time.realtimeSinceStartup;
			if (side == EdgeScreenSide.Left)
			{
				observationLeft = Mathf.Max(observationLeft, Mathf.Clamp(screenMaxX, 0f, maxInset));
				recentLeft = observationLeft;
				lastLeftSeen = now;
			}
			else
			{
				observationRight = Mathf.Max(observationRight, Mathf.Clamp(UI.screenWidth - screenMinX, 0f, maxInset));
				recentRight = observationRight;
				lastRightSeen = now;
			}
		}

		internal static Matrix4x4 PushLeft()
		{
			UpdateAnimation();
			return Push(currentLeft);
		}

		internal static Matrix4x4 PushRight()
		{
			UpdateAnimation();
			return Push(-currentRight);
		}

		internal static EdgeUIInsetState State()
		{
			UpdateAnimation();
			var now = Time.realtimeSinceStartup;
			var enabled = Settings?.indentVanillaUIForEdgeMarkers == true;
			return new EdgeUIInsetState(
				enabled,
				enabled && now - lastLeftSeen <= noMarkerHoldSeconds,
				enabled && now - lastRightSeen <= noMarkerHoldSeconds,
				observationFrame >= Time.frameCount - 1 ? observationLeft : 0f,
				observationFrame >= Time.frameCount - 1 ? observationRight : 0f,
				targetLeft,
				targetRight,
				currentLeft,
				currentRight);
		}

		static Matrix4x4 Push(float x)
		{
			var previous = GUI.matrix;
			if (Mathf.Abs(x) > settledDistance)
				GUI.matrix = previous * Matrix4x4.Translate(new Vector3(x, 0f, 0f));
			return previous;
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
			targetLeft = DesiredInset(enabled, now, lastLeftSeen, observationLeft, recentLeft);
			targetRight = DesiredInset(enabled, now, lastRightSeen, observationRight, recentRight);
			currentLeft = Smooth(currentLeft, targetLeft, ref velocityLeft, deltaTime);
			currentRight = Smooth(currentRight, targetRight, ref velocityRight, deltaTime);
		}

		static float DesiredInset(bool enabled, float now, float lastSeen, float currentObservation, float recentObservation)
		{
			if (enabled == false)
				return 0f;
			if (observationFrame >= Time.frameCount - 1 && currentObservation > 0f)
				return currentObservation;
			return now - lastSeen <= noMarkerHoldSeconds ? recentObservation : 0f;
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
	}

	[HarmonyPatch]
	static class LeftEdgeUIInsetPatch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(ResourceReadout), nameof(ResourceReadout.ResourceReadoutOnGUI));
			yield return AccessTools.Method(typeof(MouseoverReadout), nameof(MouseoverReadout.MouseoverReadoutOnGUI));
		}

		[HarmonyPriority(Priority.First)]
		static void Prefix(out Matrix4x4 __state) => __state = EdgeUIInsets.PushLeft();

		[HarmonyPriority(Priority.Last)]
		static Exception Finalizer(Exception __exception, Matrix4x4 __state)
		{
			GUI.matrix = __state;
			return __exception;
		}
	}

	[HarmonyPatch]
	static class RightEdgeUIInsetPatch
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(GlobalControls), nameof(GlobalControls.GlobalControlsOnGUI));
			yield return AccessTools.Method(typeof(AlertsReadout), nameof(AlertsReadout.AlertsReadoutOnGUI));
			yield return AccessTools.Method(typeof(MapGizmoUtility), nameof(MapGizmoUtility.MapUIOnGUI));
		}

		[HarmonyPriority(Priority.First)]
		static void Prefix(out Matrix4x4 __state) => __state = EdgeUIInsets.PushRight();

		[HarmonyPriority(Priority.Last)]
		static Exception Finalizer(Exception __exception, Matrix4x4 __state)
		{
			GUI.matrix = __state;
			return __exception;
		}
	}
}
