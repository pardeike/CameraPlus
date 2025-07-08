using Verse;
using UnityEngine;

namespace CameraPlus
{
	static class FastUI
	{
		static int lastUpdateFrameForCurUICellSize = -1;
		static int lastUpdateFrameForMouseMapPosition = -1;
		static int lastUpdateFrameForMousePositionOnUIInverted = -1;
		static float curUICellSize = 0f;
		static Vector3 mouseMapPosition = Vector3.zero;
		static Vector2 mousePositionOnUIInverted = Vector2.zero;

		public static float CurUICellSize
		{
			get
			{
				if (lastUpdateFrameForCurUICellSize != Time.frameCount)
				{
					curUICellSize = UI.CurUICellSize();
					lastUpdateFrameForCurUICellSize = Time.frameCount;
				}
				return curUICellSize;
			}
		}

		public static Vector3 MouseMapPosition
		{
			get
			{
				if (lastUpdateFrameForMouseMapPosition != Time.frameCount)
				{
					mouseMapPosition = UI.MouseMapPosition();
					lastUpdateFrameForMouseMapPosition = Time.frameCount;
				}
				return mouseMapPosition;
			}
		}

		public static Vector2 MousePositionOnUIInverted
		{
			get
			{
				if (lastUpdateFrameForMousePositionOnUIInverted != Time.frameCount)
				{
					mousePositionOnUIInverted = UI.MousePositionOnUIInverted;
					lastUpdateFrameForMousePositionOnUIInverted = Time.frameCount;
				}
				return mousePositionOnUIInverted;
			}
		}
	}
}