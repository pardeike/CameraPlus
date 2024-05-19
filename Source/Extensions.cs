using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CameraPlus
{
	static class Extensions
	{
		public static void Slider(this Listing_Standard list, ref int value, int min, int max, Func<string> label)
		{
			float f = value;
			var h = HorizontalSlider(list.GetRect(22f), ref f, min, max, label == null ? null : label(), 1f);
			value = (int)f;
			list.Gap(h);
		}

		public static void Slider(this Listing_Standard list, ref float value, float min, float max, Func<string> label, float roundTo = -1f)
		{
			var rect = list.GetRect(22f);
			var h = HorizontalSlider(rect, ref value, min, max, label == null ? null : label(), roundTo);
			list.Gap(h - 4);
		}

		public static float HorizontalSlider(Rect rect, ref float value, float leftValue, float rightValue, string label = null, float roundTo = -1f)
		{
			if (label != null)
			{
				var anchor = Text.Anchor;
				var font = Text.Font;
				Text.Font = GameFont.Tiny;
				Text.Anchor = TextAnchor.UpperLeft;
				Widgets.Label(rect, label);
				Text.Anchor = anchor;
				Text.Font = font;
				rect.y += 18f;
			}
			value = GUI.HorizontalSlider(rect, value, leftValue, rightValue);
			if (roundTo > 0f)
				value = Mathf.RoundToInt(value / roundTo) * roundTo;
			return 4f + label != null ? 18f : 0f;
		}

		public static void TwoColumns(this Listing_Standard list, Action left, Action right, float ratio = 0.5f)
		{
			var cWidth = list.ColumnWidth;
			var available = cWidth - 12;

			var firstWidth = available * ratio;
			list.ColumnWidth = firstWidth;
			var (x, y) = (list.curX, list.curY);
			left();
			list.curY = y;
			list.curX += firstWidth + 12f;
			list.ColumnWidth = available - firstWidth;
			right();
			list.ColumnWidth = cWidth;
			list.curX = x;
		}

		public static bool ButtonText(this Listing_Standard list, string label, bool active)
		{
			Rect rect = list.GetRect(30f);
			bool flag = false;
			if (list.BoundingRectCached == null || rect.Overlaps(list.BoundingRectCached.Value))
			{
				var isOver = active && Mouse.IsOver(rect);
				var anchor = Text.Anchor;
				var color = GUI.color;
				var texture2D = Widgets.ButtonBGAtlas;
				if (isOver)
				{
					texture2D = Widgets.ButtonBGAtlasMouseover;
					if (Input.GetMouseButton(0))
						texture2D = Widgets.ButtonBGAtlasClick;
				}
				Widgets.DrawAtlas(rect, texture2D);
				if (active)
					MouseoverSounds.DoRegion(rect);
				GUI.color = active ? Color.white : Color.white.ToTransparent(0.5f);
				Text.Anchor = TextAnchor.MiddleCenter;
				var wordWrap = Text.WordWrap;
				if (rect.height < Text.LineHeight * 2f)
					Text.WordWrap = false;
				Widgets.Label(rect, label);
				Text.Anchor = anchor;
				GUI.color = color;
				Text.WordWrap = wordWrap;
				flag = active && Widgets.ButtonInvisible(rect, false);
			}
			list.Gap(list.verticalSpacing);
			return flag;
		}

		public static DotConfig GetDotConfig(this Pawn pawn) =>
			CameraSettings.settings.dotConfigs.FirstOrDefault(dotConfig => dotConfig.conditions.All(condition => condition.Matches(pawn)));

		public static string ToHex(this Color color)
		{
			Color32 color32 = color;
			if (color32.a == 255)
				return $"{color32.r:X2}{color32.g:X2}{color32.b:X2}";
			else
				return $"{color32.r:X2}{color32.g:X2}{color32.b:X2}{color32.a:X2}";
		}

		public static Color ToColor(this string hex)
		{
			if (hex.Length == 6)
			{
				var r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
				var g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
				var b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
				return new Color32(r, g, b, 255);
			}
			else if (hex.Length == 8)
			{
				var r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
				var g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
				var b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
				var a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
				return new Color32(r, g, b, a);
			}
			else if (hex.Length == 3)
			{
				var r = byte.Parse(hex[0].ToString() + hex[0], System.Globalization.NumberStyles.HexNumber);
				var g = byte.Parse(hex[1].ToString() + hex[1], System.Globalization.NumberStyles.HexNumber);
				var b = byte.Parse(hex[2].ToString() + hex[2], System.Globalization.NumberStyles.HexNumber);
				return new Color32(r, g, b, 255);
			}

			return Color.clear;
		}
	}
}
