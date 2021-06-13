using System.Reflection;
using Verse;
using static HarmonyLib.AccessTools;

namespace CameraPlus
{
	[StaticConstructorOnStartup]
	static class Refs
	{
		// Common
		public static readonly FieldInfo f_rootSize = Field(typeof(CameraDriver), "rootSize");

		// DrawThingLabel
		public static readonly MethodInfo p_CameraDriver = Property(typeof(Find), "CameraDriver")?.GetGetMethod(true);

		// ApplyPositionToGameObject
		public static readonly MethodInfo p_MyCamera = Property(typeof(CameraDriver), "MyCamera")?.GetGetMethod(true);

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
