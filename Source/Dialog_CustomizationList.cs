using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using Verse;

namespace CameraPlus
{
	public abstract class Dialog_CustomizationList : Dialog_FileList
	{
		internal string FolderPath => GenFilePaths.FolderUnderSaveData("CameraPlus");
		internal string AbsPathForCustomization(string scenarioName) => Path.Combine(FolderPath, scenarioName + ".xml");

		public IEnumerable<FileInfo> AllCustomizations()
		{
			var directoryInfo = new DirectoryInfo(FolderPath);
			if (!directoryInfo.Exists)
				directoryInfo.Create();
			return directoryInfo.GetFiles().Where(f => f.Extension == ".xml").OrderByDescending(f => f.LastWriteTime);
		}

		public override void ReloadFiles()
		{
			files.Clear();
			foreach (var fileInfo in AllCustomizations())
			{
				try
				{
					var saveFileInfo = new SaveFileInfo(fileInfo);
					saveFileInfo.LoadData();
					files.Add(saveFileInfo);
				}
				catch (Exception ex)
				{
					Log.Error("Exception loading " + fileInfo.Name + ": " + ex.ToString());
				}
			}
		}
	}
}