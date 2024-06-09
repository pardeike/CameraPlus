using System;
using UnityEngine;

namespace CameraPlus
{
	public class ValueChanger(object startValue, int step, Action<object, int> deltaCallback)
	{
		private readonly object startValue = startValue;
		private readonly Vector3 start = Input.mousePosition;
		private readonly Action<object, int> deltaCallback = deltaCallback;
		private readonly float step = step;
		private int delta = 0;

		public void Tick()
		{
			var mp = Input.mousePosition;
			var x = mp.x - start.x + mp.y - start.y;
			var newDelta = (int)(x * x * x / 10000 / step);
			while (delta != newDelta)
			{
				deltaCallback(startValue, newDelta);
				delta = newDelta;
			}
		}
	}
}