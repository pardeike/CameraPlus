using Harmony;
using System;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class CameraPlusSettings : ModSettings
	{
		public float zoomedOutPercent = 65;
		public float zoomedInPercent = 1;
		public int exponentiality = 1;
		public float zoomedOutDollyPercent = 1;
		public float zoomedInDollyPercent = 1;
		public float zoomedOutDollyFrictionPercent = 0.15f;
		public float zoomedInDollyFrictionPercent = 0.15f;
		public float soundNearness = 0;
		public bool scalePawnNames = true;
		public bool hideNamesWhenZoomedOut = true;
		public int[] zoomLevelPixelSizes = new int[] { 56, 31, 16, 10 };

		public static float minRootResult = 2;
		public static float maxRootResult = 130;

		public static readonly float minRootInput = 11;
		public static readonly float maxRootInput = 60;

		public static readonly float minRootOutput = 15;
		public static readonly float maxRootOutput = 65;

		public static readonly float nearestHeight = 32;
		public static readonly float farOutHeight = 256;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref zoomedOutPercent, "zoomedOutPercent", 65);
			Scribe_Values.Look(ref zoomedInPercent, "zoomedInPercent", 1);
			Scribe_Values.Look(ref exponentiality, "exponentiality", 1);
			Scribe_Values.Look(ref zoomedOutDollyPercent, "zoomedOutDollyPercent", 1);
			Scribe_Values.Look(ref zoomedInDollyPercent, "zoomedInDollyPercent", 1);
			Scribe_Values.Look(ref zoomedOutDollyFrictionPercent, "zoomedOutDollySpeedDecayPercent", 0.15f);
			Scribe_Values.Look(ref zoomedInDollyFrictionPercent, "zoomedInDollySpeedDecayPercent", 0.15f);
			Scribe_Values.Look(ref soundNearness, "soundNearness", 0);
			Scribe_Values.Look(ref scalePawnNames, "scalePawnNames", true);
			Scribe_Values.Look(ref hideNamesWhenZoomedOut, "hideNamesWhenZoomedOut", true);
			Scribe_Values.Look(ref zoomLevelPixelSizes[0], "zoomLevelPixelSizeClosest", 56);
			Scribe_Values.Look(ref zoomLevelPixelSizes[1], "zoomLevelPixelSizeClose", 31);
			Scribe_Values.Look(ref zoomLevelPixelSizes[2], "zoomLevelPixelSizeMiddle", 16);
			Scribe_Values.Look(ref zoomLevelPixelSizes[3], "zoomLevelPixelSizeFar", 10);

			if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
			{
				minRootResult = zoomedInPercent * 2;
				maxRootResult = zoomedOutPercent * 2;
			}
		}

		public static float LerpRootSize(float x)
		{
			var n = CameraPlusMain.Settings.exponentiality;
			if (n == 0)
				return GenMath.LerpDouble(minRootInput, maxRootInput, minRootResult, maxRootResult, x);

			var factor = (maxRootResult - minRootResult) / Math.Pow(maxRootInput - minRootInput, 2 * n);
			var y = minRootResult + Math.Pow(x - minRootInput, 2 * n) * factor;
			return (float)y;
		}

		public static float GetDollyRate(float orthSize)
		{
			var zoomedIn = orthSize * CameraPlusMain.Settings.zoomedInDollyPercent * 4;
			var zoomedOut = orthSize * CameraPlusMain.Settings.zoomedOutDollyPercent;
			return GenMath.LerpDouble(minRootResult, maxRootResult, zoomedIn, zoomedOut, orthSize);
		}

		public static float GetDollySpeedDecay(float orthSize)
		{
			var minVal = 1f - CameraPlusMain.Settings.zoomedInDollyFrictionPercent;
			var maxVal = 1f - CameraPlusMain.Settings.zoomedOutDollyFrictionPercent;
			return GenMath.LerpDouble(minRootResult, maxRootResult, minVal, maxVal, orthSize);
		}

		public void DoWindowContents(Rect inRect)
		{
			float previous;
			var map = Current.Game?.CurrentMap;

			var list = new Listing_Standard { ColumnWidth = (inRect.width - 34f) / 2f };
			list.Begin(inRect);

			list.Gap(12f);

			list.Label("ZoomedInPercent".Translate() + ": " + Math.Round(zoomedInPercent, 1) + "%", -1f);
			previous = zoomedInPercent;
			zoomedInPercent = list.Slider(zoomedInPercent, 0.1f, 20f);
			minRootResult = CameraPlusMain.Settings.zoomedInPercent * 2;
			if (previous != zoomedInPercent && map != null)
			{
				var val = Traverse.Create(Find.CameraDriver).Field("rootSize").GetValue<float>();
				if (val != minRootInput)
					Find.CameraDriver.SetRootPosAndSize(map.rememberedCameraPos.rootPos, minRootInput);
			}

			list.Gap(12f);

			list.Label("ZoomedOutPercent".Translate() + ": " + Math.Round(zoomedOutPercent, 1) + "%", -1f);
			previous = zoomedOutPercent;
			zoomedOutPercent = list.Slider(zoomedOutPercent, 50f, 100f);
			maxRootResult = CameraPlusMain.Settings.zoomedOutPercent * 2;
			if (previous != zoomedOutPercent && map != null)
			{
				var val = Traverse.Create(Find.CameraDriver).Field("rootSize").GetValue<float>();
				if (val != maxRootInput)
					Find.CameraDriver.SetRootPosAndSize(map.rememberedCameraPos.rootPos, maxRootInput);
			}

			list.Gap(12f);

			list.Label("Exponentiality".Translate(), -1f);
			if (list.RadioButton("Off", exponentiality == 0, 8f)) exponentiality = 0;
			for (var i = 1; i <= 3; i++)
				if (list.RadioButton(i + "x", exponentiality == i, 8f)) exponentiality = i;

			list.Gap(12f);

			list.Label("SoundNearness".Translate() + ": " + Math.Round(soundNearness * 100, 1) + "%", -1f);
			soundNearness = list.Slider(soundNearness, 0f, 1f);

			// cannot preview here because this value is not showing differences in either
			// min or max setting (that is when you have changed any of the above sliders)

			list.Gap(12f);

			list.Label("ZoomLevelPixelSizes".Translate(), -1f);
			var zoomLevelLabels = new string[] { "ClosestZoom", "CloseZoom", "MiddleZoom", "FarZoom" };
			var max = 64f;
			for (var i = 0; i < zoomLevelLabels.Length; i++)
			{
				list.Label(zoomLevelLabels[i].Translate() + ": " + zoomLevelPixelSizes[i] + " pixels", -1f);
				list.Gap(-2f);
				zoomLevelPixelSizes[i] = (int)Math.Min(max, list.Slider(zoomLevelPixelSizes[i], 1f, 64f));
				max = Math.Min(max, zoomLevelPixelSizes[i]);
				list.Gap(-4f);
			}

			list.Gap(12f);

			list.CheckboxLabeled("ScalePawnNames".Translate(), ref scalePawnNames);
			list.CheckboxLabeled("HideNamesWhenZoomedOut".Translate(), ref hideNamesWhenZoomedOut);

			list.NewColumn();
			list.Gap(12f);

			list.Label("DollyPercentLabel".Translate());
			list.Gap(4f);
			list.Label("ZoomedIn".Translate() + ": " + Math.Round(zoomedInDollyPercent * 100, 1) + "%", -1f);
			zoomedInDollyPercent = Mathf.Round(100f * list.Slider(zoomedInDollyPercent, 0f, 4f)) / 100f;
			list.Gap(4f);
			list.Label("ZoomedOut".Translate() + ": " + Math.Round(zoomedOutDollyPercent * 100, 1) + "%", -1f);
			zoomedOutDollyPercent = Mathf.Round(100f * list.Slider(zoomedOutDollyPercent, 0f, 4f)) / 100f;

			list.Gap(12f);

			list.Label("DollyFrictionLabel".Translate());
			list.Gap(4f);
			list.Label("ZoomedIn".Translate() + ": " + Math.Round(zoomedInDollyFrictionPercent * 100, 1) + "%", -1f);
			zoomedInDollyFrictionPercent = Mathf.Round(100f * list.Slider(zoomedInDollyFrictionPercent, 0f, 1f)) / 100f;
			list.Gap(4f);
			list.Label("ZoomedOut".Translate() + ": " + Math.Round(zoomedOutDollyFrictionPercent * 100, 1) + "%", -1f);
			zoomedOutDollyFrictionPercent = Mathf.Round(100f * list.Slider(zoomedOutDollyFrictionPercent, 0f, 1f)) / 100f;

			list.Gap(12f);

			if (list.ButtonText("RestoreToDefaultSettings".Translate()))
			{
				zoomedOutPercent = 65;
				zoomedInPercent = 1;
				exponentiality = 1;
				zoomedOutDollyPercent = 1;
				zoomedInDollyPercent = 1;
				zoomedOutDollyFrictionPercent = 0.15f;
				zoomedInDollyFrictionPercent = 0.15f;
				soundNearness = 0;
				scalePawnNames = true;
				hideNamesWhenZoomedOut = true;
				zoomLevelPixelSizes = new int[] { 56, 31, 16, 10 };
			}

			list.End();
		}
	}
}