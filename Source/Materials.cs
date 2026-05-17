using UnityEngine;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	public class Materials
	{
		public Material dot;
		public Material edgeDot;
		public Material silhouette;
		public Material custom;
		public DotStyle mode;
		public string customDotStyle;
		public float outlineFactor;

		bool colorsApplied;
		Color fillColor;
		Color outlineColor;

		public void ApplyColors(Color fill, Color outline)
		{
			if (colorsApplied && fill == fillColor && outline == outlineColor)
				return;

			colorsApplied = true;
			fillColor = fill;
			outlineColor = outline;

			ApplyColors(dot, fill, outline);
			ApplyColors(silhouette, fill, outline);
			ApplyColors(custom, fill, outline);
		}

		public void ApplyEdgeColors(Color fill, Color outline)
		{
			ApplyColors(edgeDot, fill, outline);
		}

		public bool Matches(DotConfig dotConfig)
		{
			var expectedMode = dotConfig?.mode ?? Settings.dotStyle;
			var expectedCustomDotStyle = dotConfig?.customDotStyle;
			var expectedOutlineFactor = dotConfig?.outlineFactor ?? Settings.outlineFactor;
			return mode == expectedMode
				&& customDotStyle == expectedCustomDotStyle
				&& Mathf.Approximately(outlineFactor, expectedOutlineFactor);
		}

		static void ApplyColors(Material material, Color fill, Color outline)
		{
			if (material == null)
				return;

			material.SetColor("_FillColor", fill);
			material.SetColor("_OutlineColor", outline);
		}
	}
}
