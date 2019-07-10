using Harmony;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;
using static Harmony.AccessTools;

namespace CameraPlus
{
	[StaticConstructorOnStartup]
	static class Refs
	{
		// Common
		public static readonly FieldInfo f_rootSize = Field(typeof(CameraDriver), "rootSize");

		// Update
		public static readonly FieldRef<CameraDriver, Vector3> rootPos = FieldRefAccess<CameraDriver, Vector3>("rootPos");
		public static readonly FieldRef<CameraDriver, float> rootSize = FieldRefAccess<CameraDriver, float>("rootSize");
		public static readonly FieldRef<CameraDriver, Vector2> mouseDragVect = FieldRefAccess<CameraDriver, Vector2>("mouseDragVect");
		public static readonly FieldRef<CameraDriver, Vector3> velocity = FieldRefAccess<CameraDriver, Vector3>("velocity");
		public static readonly MethodInfo m_ApplyPositionToGameObject = Method(typeof(CameraDriver), "ApplyPositionToGameObject");
		public static readonly FastInvokeHandler applyPositionToGameObjectInvoker = MethodInvoker.GetHandler(m_ApplyPositionToGameObject);

		// OnGUI
		public static readonly FieldInfo f_mouseDragVect = Field(typeof(CameraDriver), "mouseDragVect");
		public static readonly FieldInfo f_desiredDolly = Field(typeof(CameraDriver), "desiredDolly");
		public static readonly MethodInfo p_get_zero = Property(typeof(Vector2), nameof(Vector2.zero)).GetGetMethod();
		public static readonly MethodInfo m_op_Inequality = Method(typeof(Vector2), "op_Inequality");

		// HandleLowPriorityShortcuts
		public static readonly FieldRef<Dialog_ModSettings, Mod> selMod = FieldRefAccess<Dialog_ModSettings, Mod>("selMod");

		// DrawThingLabel
		public static readonly MethodInfo p_CameraDriver = Property(typeof(Find), "CameraDriver")?.GetGetMethod(true);

		// ApplyPositionToGameObject
		public static readonly MethodInfo p_MyCamera = Property(typeof(CameraDriver), "MyCamera")?.GetGetMethod(true);

		static Refs()
		{
			if (f_rootSize == null)
				Log.Error("Cannot find field CameraDriver.rootSize");

			if (m_ApplyPositionToGameObject == null)
				Log.Error("Cannot find method CameraDriver.ApplyPositionToGameObject");

			if (f_mouseDragVect == null)
				Log.Error("Cannot find field CameraDriver.mouseDragVect");
			if (f_desiredDolly == null)
				Log.Error("Cannot find field CameraDriver.desiredDolly");
			if (m_op_Inequality == null)
				Log.Error("Cannot find method Vector2.op_Inequality");

			if (p_CameraDriver == null)
				Log.Error("Cannot find property Find.CameraDriver");

			if (p_MyCamera == null)
				Log.Error("Cannot find property CameraDriver.MyCamera");
		}
	}
}