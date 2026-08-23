using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class Dialog_ColorPicker : Window
	{
		enum Tracking
		{
			Init,
			Nothing,
			Value
		}

		const string swatchesFileName = "CameraPlusColors.txt";
		const int titleHeight = 35;
		const int hueSize = 40;
		const int bedSize = 320;
		const int alphaSliderHeight = 20;
		const int swatchesWidth = 160;
		const int swatchXCount = 5;
		const int swatchYCount = 8;
		const int swatchSpace = 5;
		const int colorHeight = 60;
		public const float spacing = 10f;

		static Color?[] swatches = new Color?[swatchXCount * swatchYCount];

		public static readonly Color borderEmptyColor = Color.white.ToTransparent(0.1f);
		public static readonly Color borderFullColor = Color.white.ToTransparent(0.6f);
		static readonly Vector2 swatchSize = new(16, 16);
		static bool LeftMouseDown => Input.GetMouseButton(0);
		static bool RightMouseDown => Input.GetMouseButton(1);

		Tracking tracking = Tracking.Init;
		readonly string title;
		readonly Action<Color> callback;

		float hue, sat, light;
		Color _color;
		Color? draggedColor = null;
		int draggedSwatch = -1;
		int targetSwatch = -1;
		bool colorWheelDragging;

		bool IsDragging => draggedColor.HasValue;
		public Color CurrentColor
		{
			get => _color;
			set
			{
				_color = value;
				Color.RGBToHSV(value, out hue, out sat, out light);
				callback(value);
			}
		}

		void UpdateHSL(float hue, float sat, float light)
		{
			var c = Color.HSVToRGB(hue, sat, light);
			c.a = _color.a;
			_color = c;
			callback(_color);
		}

		public override Vector2 InitialSize => new(
			StandardMargin + hueSize + spacing + bedSize + spacing + swatchesWidth + StandardMargin,
			StandardMargin + titleHeight + spacing + bedSize + spacing + alphaSliderHeight + spacing + CloseButSize.y + StandardMargin
		);

		public Dialog_ColorPicker(string title, Color color, Action<Color> callback)
		{
			this.title = title;
			this.callback = callback;
			CurrentColor = color;
			doCloseButton = true;
			draggable = true;
		}

		public override void PreOpen()
		{
			base.PreOpen();
			LoadSwatches();
		}

		public override void PreClose()
		{
			base.PreClose();
			SaveSwatches();
		}

		public override void DoWindowContents(Rect inRect)
		{
			if (tracking == Tracking.Init && LeftMouseDown == false && RightMouseDown == false)
				tracking = Tracking.Nothing;

			var list = new Listing_Standard();

			var originalInRect = inRect;
			list.Begin(inRect);

			var titleRect = list.GetRect(titleHeight);
			Text.Font = GameFont.Medium;
			Widgets.Label(titleRect, title);
			Text.Font = GameFont.Small;

			list.Gap(spacing);

			var bedRect = list.GetRect(bedSize).LeftPartPixels(bedSize);
			var hueRect = bedRect.LeftPartPixels(hueSize);
			bedRect.x += spacing + hueSize;

			list.Gap(spacing);

			var alphaRect = list.GetRect(alphaSliderHeight).LeftPartPixels(hueSize + spacing + bedSize);

			DrawValueSelector(hueRect);
			var wheelColor = _color;
			Widgets.HSVColorWheel(bedRect, ref wheelColor, ref colorWheelDragging, light);
			Color.RGBToHSV(wheelColor, out var wheelHue, out var wheelSat, out _);
			if (Mathf.Approximately(hue, wheelHue) == false || Mathf.Approximately(sat, wheelSat) == false)
			{
				hue = wheelHue;
				sat = wheelSat;
				UpdateHSL(hue, sat, light);
			}

			var oldAlpha = _color.a;
			var alpha = Widgets.HorizontalSlider(alphaRect, oldAlpha, 0, 1, true);
			if (oldAlpha != alpha)
			{
				var newColor = _color;
				newColor.a = alpha;
				CurrentColor = newColor;
			}

			list.End();

			inRect.xMin += hueSize + spacing + bedSize + spacing;
			list.Begin(inRect);
			list.curY += titleHeight + spacing;

			var colorRect = list.GetRect(colorHeight);
			GUI.DrawTexture(colorRect, Assets.editoBackgroundPattern, ScaleMode.StretchToFill);
			Widgets.DrawBoxSolidWithOutline(colorRect, CurrentColor, IsDragging ? Color.white : borderEmptyColor);
			if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && IsDragging == false && Mouse.IsOver(colorRect) && tracking == Tracking.Nothing && colorWheelDragging == false)
			{
				draggedColor = CurrentColor;
				Event.current.Use();
			}

			list.Gap(spacing);

			var swatchesRect = list.GetRect(inRect.height - list.curY - spacing - CloseButSize.y);
			var draggedTo = -1;
			for (var sy = 0; sy < swatchYCount; sy++)
				for (var sx = 0; sx < swatchXCount; sx++)
					DoSwatch(swatchesRect, sx, sy, ref draggedTo);
			targetSwatch = draggedTo;

			list.End();

			if (IsDragging)
			{
				var swatchRect = new Rect(Event.current.mousePosition - swatchSize / 2, swatchSize);
				Widgets.DrawBoxSolidWithOutline(swatchRect, draggedColor.Value, Color.white);
			}

			HandleTracking(originalInRect, hueRect);
		}

		void DrawValueSelector(Rect rect)
		{
			const int segmentCount = 64;
			var segmentHeight = rect.height / segmentCount;
			for (var i = 0; i < segmentCount; i++)
			{
				var value = 1f - i / (segmentCount - 1f);
				var segmentRect = new Rect(rect.x, rect.y + i * segmentHeight, rect.width, segmentHeight + 1f);
				Widgets.DrawBoxSolid(segmentRect, Color.HSVToRGB(hue, sat, value));
			}

			var markerY = rect.yMax - rect.height * light;
			var markerColor = light > 0.5f ? Color.black : Color.white;
			Widgets.DrawBoxSolid(new Rect(rect.x, markerY - 1f, rect.width, 2f), markerColor);
		}

		void DoSwatch(Rect swatchesRect, int sx, int sy, ref int draggedTo)
		{
			var size = (swatchesRect.width - (swatchXCount - 1) * swatchSpace) / swatchXCount;
			var rx = swatchesRect.xMin + sx * (size + swatchSpace);
			var ry = swatchesRect.yMin + sy * (size + swatchSpace);
			var swatchRect = new Rect(rx, ry, size, size);
			var n = sy * swatchXCount + sx;
			var over = Mouse.IsOver(swatchRect) && tracking == Tracking.Nothing && colorWheelDragging == false;
			if (IsDragging && over)
				draggedTo = n;
			var borderColor = swatches[n].HasValue ? borderFullColor : borderEmptyColor;
			if (swatches[n].HasValue)
				GUI.DrawTexture(swatchRect, Assets.swatchBackgroundPattern, ScaleMode.StretchToFill);
			Widgets.DrawBoxSolidWithOutline(swatchRect, swatches[n] ?? Color.clear, draggedTo == n ? Color.white : borderColor);
			if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && IsDragging == false && over)
			{
				draggedColor = swatches[n];
				draggedSwatch = n;
			}
			if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && over)
				swatches[n] = null;
			if (Widgets.ButtonInvisible(swatchRect) && swatches[n].HasValue && RightMouseDown == false)
				CurrentColor = swatches[n].Value;
		}

		static void LoadSwatches()
		{
			Array.Fill(swatches, null);
			var path = Path.Combine(GenFilePaths.ConfigFolderPath, swatchesFileName);
			if (File.Exists(path) == false)
				return;
			swatches = File.ReadAllText(path).Split('\n')
				.Where(l => l.NullOrEmpty() == false)
				.Select(l => l == "undefined" ? System.Array.Empty<float>() : l.Split(' ').Select(s => ParseHelper.ParseFloat(s)).ToList().ToArray())
				.Select(p => p.Length == 0 ? (Color?)null : new Color(p[0], p[1], p[2], p[3]))
				.ToList().ToArray(); // Disambiguate ToArray by converting to List first
		}

		static void SaveSwatches()
		{
			var text = swatches.Join(c => c.HasValue ? $"{c.Value.r} {c.Value.g} {c.Value.b} {c.Value.a}" : "undefined", "\n");
			var path = Path.Combine(GenFilePaths.ConfigFolderPath, swatchesFileName);
			File.WriteAllText(path, text);
		}

		void HandleTracking(Rect inRect, Rect valueRect)
		{
			if (tracking == Tracking.Init)
				return;

			if (LeftMouseDown == false || Mouse.IsOver(inRect) == false)
			{
				tracking = Tracking.Nothing;
				if (IsDragging)
				{
					if (targetSwatch > -1)
					{
						if (draggedSwatch > -1)
							(swatches[draggedSwatch], swatches[targetSwatch]) = (swatches[targetSwatch], swatches[draggedSwatch]);
						else
							swatches[targetSwatch] = draggedColor.Value;
					}
					draggedSwatch = -1;
					targetSwatch = -1;
					draggedColor = null;
				}
			}

			if (IsDragging)
				return;

			var currentEvent = Event.current;
			if (currentEvent.isMouse && currentEvent.button == 0)
			{
				if (tracking == Tracking.Value && currentEvent.type == EventType.MouseUp)
				{
					tracking = Tracking.Nothing;
					currentEvent.Use();
					return;
				}
				if ((currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) && (tracking == Tracking.Value || Mouse.IsOver(valueRect)))
				{
					tracking = Tracking.Value;
					light = 1f - Mathf.Clamp01((currentEvent.mousePosition.y - valueRect.yMin) / valueRect.height);
					UpdateHSL(hue, sat, light);
					currentEvent.Use();
				}
			}
		}
	}
}
