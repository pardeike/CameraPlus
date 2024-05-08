using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class OptionalColor(Color? color) : IExposable
	{
		public Color? color = color;

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
	}
}