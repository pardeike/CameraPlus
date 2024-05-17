using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace CameraPlus
{
	public class CameraSettings(World world) : WorldComponent(world)
	{
		public List<DotConfig> dotConfigs = [];

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref dotConfigs, "dotConfigs", LookMode.Deep);
			dotConfigs ??= [];
		}
	}
}
