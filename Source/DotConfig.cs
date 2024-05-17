using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public enum DotMode
	{
		Off,
		Dot,
		Silhouette
	}

	public class DotConfig : IExposable
	{
		public List<ConditionTag> conditions = [];

		public DotMode mode = DotMode.Off;
		public int showBelowPixels = -1;
		public bool useInside = true;
		public bool useEdge = true;
		public Color lineColor = Color.black;
		public Color fillColor = Color.white;
		public Color lineSelectedColor = Color.white;
		public Color fillSelectedColor = Color.white;
		public float relativeSize = 1;
		public float lineWidth = 1;
		public bool hideOnMouseover = true;

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
			Scribe_Values.Look(ref lineWidth, "lineWidth", 1);
			Scribe_Values.Look(ref hideOnMouseover, "hideOnMouseover", true);
		}
	}
}