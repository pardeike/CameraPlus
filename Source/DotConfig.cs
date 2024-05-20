using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public enum DotMode
	{
		Default,
		VanillaDot,
		VanillaSilhouette,
		CameraPlusDot,
		CameraPlusSilhouette
	}

	public class DotConfig : IExposable
	{
		public List<ConditionTag> conditions = [];

		public DotMode mode = DotMode.Default;
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
			mode = DotMode.Default;
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

		public DotConfig Clone() => new()
		{
			conditions = conditions.Select(condition => condition.Clone()).ToList(),
			mode = mode,
			showBelowPixels = showBelowPixels,
			useInside = useInside,
			useEdge = useEdge,
			lineColor = lineColor,
			fillColor = fillColor,
			lineSelectedColor = lineSelectedColor,
			fillSelectedColor = fillSelectedColor,
			relativeSize = relativeSize,
			outlineFactor = outlineFactor,
			hideOnMouseover = hideOnMouseover
		};

		// keep
		public DotConfig(params object[] args) : base()
		{
		}

		public void ExposeData()
		{
			Scribe_Collections.Look(ref conditions, "conditions", LookMode.Deep);
			conditions ??= [];

			Scribe_Values.Look(ref mode, "mode", DotMode.Default);
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

		public static DotConfig ToDotConfig(string xml)
		{
			if (xml.StartsWith("<?xml") == false)
				return null;
			return Tools.ScribeFromString<DotConfig>(xml);
		}

		public override string ToString() => Tools.ScribeToString(this);
	}
}