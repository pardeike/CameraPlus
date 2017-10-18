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

		public void DoWindowContents(Rect inRect)
		{
			float previous;
			var map = Current.Game?.VisibleMap;

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

			// cannot preview here because this value is not showing differences in either
			// min or max setting (that is when you have changed any of the above sliders)

			list.NewColumn();
			list.Gap(12f);

			list.Label("ZoomedInDollyPercent".Translate() + ": " + Math.Round(zoomedInDollyPercent * 100, 1) + "%", -1f);
			zoomedInDollyPercent = list.Slider(zoomedInDollyPercent, 0f, 2f);

			list.Gap(12f);

			list.Label("ZoomedOutDollyPercent".Translate() + ": " + Math.Round(zoomedOutDollyPercent * 100, 1) + "%", -1f);
			zoomedOutDollyPercent = list.Slider(zoomedOutDollyPercent, 0f, 2f);

			list.End();
		}
	}
}