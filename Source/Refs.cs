using HarmonyLib;
using System.Reflection;
using Verse;
using static HarmonyLib.AccessTools;

namespace CameraPlus
{
	[StaticConstructorOnStartup]
	static class Refs // used in transpiling
	{
		public static readonly MethodInfo p_RootSize = PropertyGetter(typeof(CameraDriver), nameof(CameraDriver.RootSize));
		public static readonly MethodInfo p_CameraDriver = PropertyGetter(typeof(Find), nameof(Find.CameraDriver));
		public static readonly MethodInfo p_MyCamera = PropertyGetter(typeof(CameraDriver), nameof(CameraDriver.MyCamera));

		static Refs()
		{
			if (p_RootSize == null)
				Log.Error("Cannot find field CameraDriver.RootSize");

			if (p_CameraDriver == null)
				Log.Error("Cannot find property Find.CameraDriver");

			if (p_MyCamera == null)
				Log.Error("Cannot find property CameraDriver.MyCamera");
		}
	}
}
