using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public enum DotMode
	{
		Off,
		VanillaDot,
		VanillaSilhouette,
		CameraPlusDot,
		CameraPlusSilhouette
	}

	public class DotConfig : IExposable
	{
		public List<ConditionTag> conditions = [];

		public DotMode mode = DotMode.CameraPlusSilhouette;
		public int showBelowPixels = -1;
		public bool useInside = true;
		public bool useEdge = true;
		public Color lineColor = Color.black;
		public Color fillColor = Color.white;
		public Color lineSelectedColor = Color.white;
		public Color fillSelectedColor = Color.white;
		public float relativeSize = 1;
		public float outlineFactor = 0.1f;
		public bool hideOnMouseover = true;

		public DotConfig()
		{
			conditions = [];
			mode = DotMode.CameraPlusSilhouette;
			showBelowPixels = -1;
			useInside = true;
			useEdge = true;
			lineColor = Color.black;
			fillColor = Color.white;
			lineSelectedColor = Color.white;
			fillSelectedColor = Color.white;
			relativeSize = 1;
			outlineFactor = 0.1f;
			hideOnMouseover = true;
		}

		public DotConfig(params object[] args) : base()
		{
			//if (args == null)
			//	Log.Warning($"### used wrorng constructor: null");
			//else
			//	Log.Warning($"### used wrorng constructor: {args.Length} [{args.Join(a => $"{a}")}]");
		}

		public void ExposeData()
		{
			Scribe_Collections.Look(ref conditions, "conditions", LookMode.Deep);
			conditions ??= [];

			Scribe_Values.Look(ref mode, "mode", DotMode.Off);
			Scribe_Values.Look(ref showBelowPixels, "showBelowPixels", -1);
			Scribe_Values.Look(ref useInside, "useInside", true);
			Scribe_Values.Look(ref useEdge, "useEdge", true);
			Scribe_Values.Look(ref lineColor, "lineColor", Color.clear);
			Scribe_Values.Look(ref fillColor, "fillColor", Color.clear);
			Scribe_Values.Look(ref lineSelectedColor, "lineSelectedColor", Color.clear);
			Scribe_Values.Look(ref fillSelectedColor, "fillSelectedColor", Color.clear);
			Scribe_Values.Look(ref relativeSize, "relativeSize", 1);
			Scribe_Values.Look(ref outlineFactor, "outlineFactor", 1);
			Scribe_Values.Look(ref hideOnMouseover, "hideOnMouseover", true);
		}
	}
}