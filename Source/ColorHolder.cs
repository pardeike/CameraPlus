using System;
using UnityEngine;

namespace CameraPlus
{
	class ColorHolder(Color color, Action<Color> update)
	{
		public Color color = color;
		public Action<Color> update = update;

		public static ColorHolder With(Color color, Action<Color> update) => new(color, update);
	}
}