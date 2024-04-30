using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	/*
	public class LoadingDialog : Window
	{
		readonly IEnumerator<int> silhouetteLoader = DotTools.CreateCache();
		int percent;

		public LoadingDialog()
		{
			doCloseButton = false;
			doCloseX = false;
			resizeable = false;
			absorbInputAroundWindow = true;
		}

		public override Vector2 InitialSize => new(200, 100);

		public override void DoWindowContents(Rect inRect)
		{
			GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
			Widgets.Label(inRect, $"{percent}%");
			GenUI.ResetLabelAlign();
		}

		public override void WindowUpdate()
		{
			if (silhouetteLoader.MoveNext() == false)
			{
				Close();
				return;
			}
			percent = silhouetteLoader.Current;
		}
	}
	*/
}
