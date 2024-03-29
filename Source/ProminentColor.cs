using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class ColorAmount(Color32 color, int amount)
{
	public Color32 color = color;
	public int amount = amount;
}

public static class ProminentColor
{
	static readonly List<Color32> colorList = [];
	static List<ColorAmount> pixelColorAmount = [];

	public static List<Color32> GetColors32FromImage(Texture2D texture, int resizedTo, int maxColorAmount, float colorLimiterPercentage, int toleranceUniteColors, float minimiumColorPercentage)
	{
		colorList.Clear();
		pixelColorAmount.Clear();

		var pixels = texture.GetPixels32();
		pixels = ResizeCanvas(pixels, texture.width, texture.height, resizedTo, resizedTo);

		for (var i = 0; i < pixels.Length; i += 1)
		{
			var px = pixels[i];
			if (px.a < 255)
				continue;
			var c = pixelColorAmount.Find(x => x.color.Equals(px));
			if (c == null)
				pixelColorAmount.Add(new ColorAmount(px, 1));
			else
				c.amount++;
		}

		if (pixelColorAmount.Count <= 0)
			return null;

		pixelColorAmount = UniteSimilarColors(pixelColorAmount, toleranceUniteColors);

		pixelColorAmount = [.. pixelColorAmount.OrderByDescending(x => x.amount)];

		var totalAmount = pixelColorAmount.Sum(x => x.amount);

		var lastAmount = pixelColorAmount[0].amount;
		colorList.Add(pixelColorAmount[0].color);
		pixelColorAmount.RemoveAt(0);

		for (var i = 0; i < pixelColorAmount.Count; i++)
		{
			if (pixelColorAmount.Count <= i || colorList.Count >= maxColorAmount || pixelColorAmount[i].amount < (float)totalAmount / minimiumColorPercentage)
				continue;

			if (((float)pixelColorAmount[i].amount / lastAmount) * 100f > (i == 0 ? 5f : colorLimiterPercentage))
			{
				colorList.Add(pixelColorAmount[i].color);
				lastAmount = pixelColorAmount[i].amount;
			}
		}

		return colorList;
	}

	static Color32[] ResizeCanvas(IList<Color32> pixels, int oldWidth, int oldHeight, int width, int height)
	{
		var newPixels = new Color32[(width * height)];
		var wBorder = (width - oldWidth) / 2;
		var hBorder = (height - oldHeight) / 2;

		for (var r = 0; r < height; r++)
		{
			var oldR = r - hBorder;
			if (oldR < 0)
			{ continue; }
			if (oldR >= oldHeight)
			{ break; }

			for (var c = 0; c < width; c++)
			{
				var oldC = c - wBorder;
				if (oldC < 0)
				{ continue; }
				if (oldC >= oldWidth)
				{ break; }

				var oldI = oldR * oldWidth + oldC;
				var i = r * width + c;
				newPixels[i] = pixels[oldI];
			}
		}

		return newPixels;
	}

	static List<ColorAmount> UniteSimilarColors(List<ColorAmount> colorAmounts, int tolerance = 30, bool replaceSimilarColors = false)
	{
		var toReturn = new List<ColorAmount>();

		for (var i = 0; i < colorAmounts.Count; i++)
		{
			var found = false;
			for (var j = 0; j < toReturn.Count; j++)
			{
				if (ColorTest(ref colorAmounts[i].color, ref toReturn[j].color, tolerance))
				{
					if (replaceSimilarColors)
					{
						if (GetColorSaturation(ref toReturn[j].color) < GetColorSaturation(ref colorAmounts[i].color))
							toReturn[j].color = colorAmounts[i].color;
					}

					toReturn[j].amount += colorAmounts[i].amount;
					found = true;
				}
			}

			if (!found)
				toReturn.Add(colorAmounts[i]);
		}

		return toReturn;
	}

	static bool ColorTest(ref Color32 c1, ref Color32 c2, float tol)
	{
		float diffRed = Mathf.Abs(c1.r - c2.r);
		float diffGreen = Mathf.Abs(c1.g - c2.g);
		float diffBlue = Mathf.Abs(c1.b - c2.b);

		var diffPercentage = ((diffRed / 255f) + (diffGreen / 255f) + (diffBlue / 255f)) / 3 * 100;
		return diffPercentage < tol;
	}

	static double GetColorSaturation(ref Color32 color)
	{
		var max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
		var min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
		return (max == 0) ? 0 : 1d - (1d * min / max);
	}
}