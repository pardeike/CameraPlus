using System;
using System.Collections.Generic;
using Verse;

namespace CameraPlus
{
	public class Dialog_CustomizationList_Load : Dialog_CustomizationList
	{
		public Dialog_CustomizationList_Load()
		{
			interactButLabel = "Load".TranslateSimple();
		}

		public override void DoFileInteraction(string fileName)
		{
			var filePath = AbsPathForCustomization(fileName);
			try
			{
				Scribe.loader.InitLoading(filePath);
				var dotConfigs = new List<DotConfig>();
				Scribe_Collections.Look(ref dotConfigs, "dotConfigs", LookMode.Deep);
				Scribe.loader.FinalizeLoading();

				var cameraSettings = Find.World.GetComponent<CameraSettings>();
				cameraSettings.dotConfigs.Clear();
				cameraSettings.dotConfigs.AddRange(dotConfigs);
			}
			catch (Exception ex)
			{
				Scribe.ForceStop();
				Log.Error("Failed loading " + fileName + ": " + ex.ToString());
			}
			Close(true);
		}
	}
}