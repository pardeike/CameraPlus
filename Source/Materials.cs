using UnityEngine;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	public class Materials
	{
		public Material dot;
		public Material edgeDot;
		public MarkerVisualBounds edgeVisualBounds = MarkerVisualBounds.Full;
		public bool edgeVisualBoundsReady;
		public Material silhouette;
		public Material custom;
		public MaterialSignature signature;
		public bool dynamicMarkerTextures;

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

		public bool Matches(MaterialSignature expectedSignature)
			=> signature.Matches(expectedSignature);

		public bool MatchesConfiguration(DotStyle mode, string customDotStyle, float outlineFactor, bool westFacing)
			=> signature.MatchesConfiguration(mode, customDotStyle, outlineFactor, westFacing);

		public bool NeedsPreparation(DotStyle mode, bool needInside, bool needEdge, bool needEdgeBounds)
		{
			if (needInside)
			{
				if (mode == DotStyle.ClassicDots && dot == null)
					return true;
				if (mode == DotStyle.BetterSilhouettes && silhouette == null)
					return true;
				if (mode == DotStyle.Custom && custom == null)
					return true;
			}

			return needEdge && edgeDot == null
				|| needEdgeBounds && edgeVisualBoundsReady == false;
		}

		static void ApplyColors(Material material, Color fill, Color outline)
		{
			if (material == null)
				return;

			material.SetColor("_FillColor", fill);
			material.SetColor("_OutlineColor", outline);
		}
	}

	public readonly struct MarkerVisualBounds
	{
		public static readonly MarkerVisualBounds Full = new MarkerVisualBounds(
			new Rect(0f, 0f, 1f, 1f),
			new Rect(0f, 0f, 1f, 1f));

		public readonly Rect fill;
		public readonly Rect outlined;

		public MarkerVisualBounds(Rect fill, Rect outlined)
		{
			this.fill = fill;
			this.outlined = outlined;
		}

		public Rect For(Color outlineColor)
			=> outlineColor.a > 0.001f ? outlined : fill;
	}

	public readonly struct MaterialSignature
	{
		public readonly DotStyle mode;
		public readonly string customDotStyle;
		public readonly float outlineFactor;
		public readonly int dotTextureId;
		public readonly int silhouetteTextureId;
		public readonly int customTextureId;
		public readonly bool westFacing;

		public MaterialSignature(DotStyle mode, string customDotStyle, float outlineFactor, int dotTextureId, int silhouetteTextureId, int customTextureId, bool westFacing)
		{
			this.mode = mode;
			this.customDotStyle = customDotStyle;
			this.outlineFactor = outlineFactor;
			this.dotTextureId = dotTextureId;
			this.silhouetteTextureId = silhouetteTextureId;
			this.customTextureId = customTextureId;
			this.westFacing = westFacing;
		}

		public bool MatchesConfiguration(DotStyle expectedMode, string expectedCustomDotStyle, float expectedOutlineFactor, bool expectedWestFacing)
			=> mode == expectedMode
			&& customDotStyle == expectedCustomDotStyle
			&& Mathf.Approximately(outlineFactor, expectedOutlineFactor)
			&& westFacing == expectedWestFacing;

		public bool Matches(MaterialSignature other)
			=> MatchesConfiguration(other.mode, other.customDotStyle, other.outlineFactor, other.westFacing)
			&& dotTextureId == other.dotTextureId
			&& silhouetteTextureId == other.silhouetteTextureId
			&& customTextureId == other.customTextureId;
	}
}
