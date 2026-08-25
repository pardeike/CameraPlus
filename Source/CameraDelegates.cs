using HarmonyLib;
using System;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	class CameraDelegates
	{
		public Func<Pawn, Color[]> GetCameraColors;
		public Func<Pawn, Texture2D[]> GetCameraMarkers;

		readonly Type pawnType;

		public bool HasCameraMarkers => GetCameraMarkers != null;

		public CameraDelegates(Pawn pawn)
		{
			pawnType = pawn?.GetType();
			GetCameraColors = CreateDelegate<Color>("GetCameraPlusColors");
			GetCameraMarkers = CreateDelegate<Texture2D>("GetCameraPlusMarkers");
		}

		public bool TryGetCameraColors(Pawn pawn, out Color[] colors)
			=> TryInvoke(pawn, "GetCameraPlusColors", ref GetCameraColors, out colors);

		public bool TryGetCameraMarkers(Pawn pawn, out Texture2D[] textures)
			=> TryInvoke(pawn, "GetCameraPlusMarkers", ref GetCameraMarkers, out textures);

		public void WarnInvalidResult(string methodName, string expectedResult)
			=> WarnOnce(methodName,
				$"returned an invalid value; expected {expectedResult}. Default marker values will be used.",
				126509717);

		bool TryInvoke<T>(Pawn pawn, string methodName, ref Func<Pawn, T[]> provider, out T[] values)
		{
			if (provider == null)
			{
				values = null;
				return false;
			}

			try
			{
				values = provider(pawn);
				return true;
			}
			catch (Exception exception)
			{
				provider = null;
				values = null;
				WarnOnce(methodName,
					$"failed and has been disabled for this pawn type; default marker values will be used: {exception}",
					144499043);
				return false;
			}
		}

		Func<Pawn, T[]> CreateDelegate<T>(string methodName)
		{
			if (pawnType == null)
				return null;

			try
			{
				var supportType = pawnType.Assembly?.GetType("CameraPlusSupport.Methods", false);
				var method = supportType == null ? null : AccessTools.Method(supportType, methodName, [typeof(Pawn)]);
				if (method == null || method.IsStatic == false || method.ReturnType != typeof(T[]))
					return null;

				return Delegate.CreateDelegate(typeof(Func<Pawn, T[]>), method, false) as Func<Pawn, T[]>;
			}
			catch (Exception exception)
			{
				WarnOnce(methodName, $"could not be bound; default marker values will be used: {exception}", 130636057);
				return null;
			}
		}

		void WarnOnce(string methodName, string message, int salt)
		{
			var typeName = pawnType?.FullName ?? "unknown pawn type";
			var key = Gen.HashCombineInt(typeName.GetHashCode(), methodName?.GetHashCode() ?? 0);
			Log.WarningOnce($"CameraPlus support method {typeName}.{methodName} {message}", Gen.HashCombineInt(key, salt));
		}
	}
}
