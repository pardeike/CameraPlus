using System.Collections.Generic;
using Verse;

namespace CameraPlus
{
	public class SavedViews(Map map) : MapComponent(map)
	{
		public RememberedCameraPos[] views = new RememberedCameraPos[9];
		public List<DotConfig> dotConfig = [];

		public override void ExposeData()
		{
			for (var i = 0; i < 9; i++)
				Scribe_Deep.Look(ref views[i], "view" + (i + 1), [map]);
			Scribe_Collections.Look(ref dotConfig, "dotConfig", LookMode.Deep);
			dotConfig ??= [];
		}
	}
}