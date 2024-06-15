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
			var dotConfigs = Tools.LoadDotConfigs(filePath);
			var cameraSettings = Find.World.GetComponent<CameraSettings>();
			cameraSettings.dotConfigs.Clear();
			cameraSettings.dotConfigs.AddRange(dotConfigs);
			Close(true);
		}
	}
}