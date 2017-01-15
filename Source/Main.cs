using Verse;
using Harmony;
using UnityEngine;

namespace CameraPlus
{
	[StaticConstructorOnStartup]
	class Main
	{
		static Main()
		{
			var harmony = HarmonyInstance.Create("net.pardeike.rimworld.mod.camera+");
			harmony.PatchAll(typeof(Main).Module);
		}
	}

	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch("ApplyPositionToGameObject")]
	class CameraDriverPatch
	{
		static void Postfix(CameraDriver instance)
		{
			var trv = Traverse.Create(instance);
			var p_MyCamera = trv.Property("MyCamera").GetValue<Camera>();
			var pos = p_MyCamera.transform.position;
			var y = p_MyCamera.transform.position.y;
			var o = p_MyCamera.orthographicSize;

			var y2 = GenMath.LerpDouble(65, 15, 65, 32, y);
			var o2 = GenMath.LerpDouble(60, 11, 60, 0.5f, o);
			var dolly = GenMath.LerpDouble(65, 15, 1, 3, y);
			dolly = 85 - dolly * dolly * dolly * dolly;

			p_MyCamera.transform.position = new Vector3(pos.x, y2, pos.z);
			p_MyCamera.orthographicSize = o2;
			instance.config.dollyRateKeys = dolly;
		}
	}
}