using UnityEngine;

namespace CameraPlus
{
	public class Materials
	{
		public Material dot;
		public Material silhouette;
		public Material custom;
		public int refreshTick;

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

		static void ApplyColors(Material material, Color fill, Color outline)
		{
			if (material == null)
				return;

			material.SetColor("_FillColor", fill);
			material.SetColor("_OutlineColor", outline);
		}
	}
}
