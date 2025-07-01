using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class DotConfig : IExposable
	{
		public List<ConditionTag> conditions = [];
		public DotStyle mode = DotStyle.BetterSilhouettes;
		public string customDotStyle = null;
		public int showBelowPixels = -1;
		public bool useInside = true;
		public bool useEdge = true;
		public Color lineColor = Color.black;
		public Color fillColor = Color.white;
		public Color lineSelectedColor = Color.white;
		public Color fillSelectedColor = Color.white;
		public float relativeSize = 1;
		public float outlineFactor = 0.1f;
		public bool mouseReveals = true;

		public DotConfig()
		{
			conditions = [];
			mode = DotStyle.BetterSilhouettes;
			customDotStyle = null;
			showBelowPixels = -1;
			useInside = true;
			useEdge = true;
			lineColor = Color.black;
			fillColor = Color.white;
			lineSelectedColor = Color.white;
			fillSelectedColor = Color.white;
			relativeSize = 1;
			outlineFactor = 0.1f;
			mouseReveals = true;
		}

		public DotConfig Clone() => new()
		{
			conditions = conditions.Select(condition => condition.Clone()).ToArray().ToList(),
			mode = mode,
			customDotStyle = customDotStyle,
			showBelowPixels = showBelowPixels,
			useInside = useInside,
			useEdge = useEdge,
			lineColor = lineColor,
			fillColor = fillColor,
			lineSelectedColor = lineSelectedColor,
			fillSelectedColor = fillSelectedColor,
			relativeSize = relativeSize,
			outlineFactor = outlineFactor,
			mouseReveals = mouseReveals
		};

		// keep
		public DotConfig(params object[] _) : base()
		{
		}

		public void ExposeData()
		{
			Scribe_Collections.Look(ref conditions, "conditions", LookMode.Deep);
			conditions ??= [];

			Scribe_Values.Look(ref mode, "mode", DotStyle.BetterSilhouettes);
			Scribe_Values.Look(ref customDotStyle, "customDotStyle", null);
			Scribe_Values.Look(ref showBelowPixels, "showBelowPixels", -1);
			Scribe_Values.Look(ref useInside, "useInside", true);
			Scribe_Values.Look(ref useEdge, "useEdge", true);
			Scribe_Values.Look(ref lineColor, "lineColor", Color.clear);
			Scribe_Values.Look(ref fillColor, "fillColor", Color.clear);
			Scribe_Values.Look(ref lineSelectedColor, "lineSelectedColor", Color.clear);
			Scribe_Values.Look(ref fillSelectedColor, "fillSelectedColor", Color.clear);
			Scribe_Values.Look(ref relativeSize, "relativeSize", 1);
			Scribe_Values.Look(ref outlineFactor, "outlineFactor", 1);
			Scribe_Values.Look(ref mouseReveals, "mouseReveals", true);
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