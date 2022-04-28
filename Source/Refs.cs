using System.Reflection;
using Verse;
using static HarmonyLib.AccessTools;

namespace CameraPlus
{
	[StaticConstructorOnStartup]
	static class Refs // used in transpiling
	{
		public static readonly FieldInfo f_rootSize = Field(typeof(CameraDriver), nameof(CameraDriver.rootSize));
		public static readonly MethodInfo p_CameraDriver = PropertyGetter(typeof(Find), nameof(Find.CameraDriver));
		public static readonly MethodInfo p_MyCamera = PropertyGetter(typeof(CameraDriver), nameof(CameraDriver.MyCamera));

		static Refs()
		{
			if (f_rootSize == null)
				Log.Error("Cannot find field CameraDriver.rootSize");

			if (p_CameraDriver == null)
				Log.Error("Cannot find property Find.CameraDriver");

			if (p_MyCamera == null)
				Log.Error("Cannot find property CameraDriver.MyCamera");
		}
	}
}
