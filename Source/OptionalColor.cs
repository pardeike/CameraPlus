using System;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class OptionalColor : IExposable, IEquatable<OptionalColor>
	{
		public Color? color;

		public OptionalColor()
		{
		}

		public OptionalColor(Color? color)
		{
			this.color = color;
		}

		public void ExposeData()
		{
			if (Scribe.mode == LoadSaveMode.Saving)
				Scribe.saver.WriteElement("value", color.HasValue ? color.Value.ToString() : "");
			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				var subNode = Scribe.loader.curXmlParent["value"];
				color = subNode.InnerText.NullOrEmpty() ? null : ParseHelper.FromString<Color>(subNode.InnerText);
			}
		}

		public bool Equals(OptionalColor other) => color == other.color;

		public override bool Equals(object obj)
		{
			if (obj is not OptionalColor optionalColor) return false;
			return Equals(optionalColor);
		}

		public override int GetHashCode() => color.GetHashCode();
		public static bool operator ==(OptionalColor left, OptionalColor right) => Equals(left, right);
		public static bool operator !=(OptionalColor left, OptionalColor right) => !Equals(left, right);
	}
}