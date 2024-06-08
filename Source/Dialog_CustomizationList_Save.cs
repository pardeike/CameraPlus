using System.IO;
using RimWorld;
using Verse;

namespace CameraPlus
{
	public class Dialog_CustomizationList_Save : Dialog_CustomizationList
	{
		const string rootElementName = "CameraPlusCustomization";

		public override bool ShouldDoTypeInField => true;

		public Dialog_CustomizationList_Save()
		{
			interactButLabel = "OverwriteButton".Translate();
			typingName = null;
		}

		private void Save()
		{
			var dotConfigs = Find.World.GetComponent<CameraSettings>().dotConfigs;
			Scribe_Collections.Look(ref dotConfigs, "dotConfigs", LookMode.Deep);
		}

		public override void DoFileInteraction(string fileName)
		{
			fileName = GenFile.SanitizedFileName(fileName);
			var absPath = Path.Combine(Assets.CameraPlusFolderPath, fileName + ".xml");
			LongEventHandler.QueueLongEvent(() => SafeSaver.Save(absPath, rootElementName, Save, false), "SavingLongEvent", false, null, true, null);
			Messages.Message("SavedAs".Translate(fileName), MessageTypeDefOf.SilentInput, false);
			Close(true);
		}
	}
}