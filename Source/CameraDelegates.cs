using HarmonyLib;
using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	class CameraDelegates
	{
		public Func<Pawn, Color[]> GetCameraColors = null;
		public Func<Pawn, Texture2D[]> GetCameraMarkers = null;

		static MethodInfo GetMethod(Pawn pawn, string name)
		{
			return pawn.GetType().Assembly
				.GetType("CameraPlusSupport.Methods", false)?
				.GetMethod(name, AccessTools.all);
		}

		public CameraDelegates(Pawn pawn)
		{
			var m_GetCameraColors = GetMethod(pawn, "GetCameraPlusColors");
			if (m_GetCameraColors != null)
			{
				var funcType = Expression.GetFuncType(new[] { typeof(Pawn), typeof(Color[]) });
				GetCameraColors = (Func<Pawn, Color[]>)Delegate.CreateDelegate(funcType, m_GetCameraColors);
			}

			var m_GetCameraTextures = GetMethod(pawn, "GetCameraPlusMarkers");
			if (m_GetCameraTextures != null)
			{
				var funcType = Expression.GetFuncType(new[] { typeof(Pawn), typeof(Texture2D[]) });
				GetCameraMarkers = (Func<Pawn, Texture2D[]>)Delegate.CreateDelegate(funcType, m_GetCameraTextures);
			}
		}
	}
}
