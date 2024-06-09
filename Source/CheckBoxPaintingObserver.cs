using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace CameraPlus
{
	[HarmonyPatch(typeof(Widgets))]
	[HarmonyPatch(nameof(Widgets.WidgetsOnGUI))]
	static class Widgets_WidgetsOnGUI_Patch
	{
		static bool checkboxPainting;

		public static void Postfix()
		{
			var oldCheckboxPainting = checkboxPainting;
			checkboxPainting = Widgets.checkboxPainting;
			if (oldCheckboxPainting != checkboxPainting)
				CheckBoxPaintingObserver.Notify(checkboxPainting);
		}
	}

	public static class CheckBoxPaintingObserver
	{
		static int counter = 0;
		static readonly Dictionary<int, Action<bool>> observers = [];

		public static void Notify(bool state) => observers.Values.Do(o => o(state));

		public static int Register(Action<bool> callback)
		{
			observers[++counter] = callback;
			return counter;
		}

		public static void Unregister(int id)
		{
			observers.Remove(id);
		}
	}
}