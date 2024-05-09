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
			Hues,
			ColorBed,
			Swatch
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
		readonly Action<Color?> callback;

		float hue, sat, light;
		Color? _color;
		Color? draggedColor = null;
		int draggedSwatch = -1;
		int targetSwatch = -1;

		bool IsDragging => draggedColor.HasValue;
		public Color? CurrentColor
		{
			get => _color;
			set
			{
				_color = value;
				if (value.HasValue)
					Color.RGBToHSV(value.Value, out hue, out sat, out light);
				callback(value);
			}
		}

		void UpdateHSL(float hue, float sat, float light)
		{
			var c = Color.HSVToRGB(hue, sat, light);
			c.a = _color?.a ?? 1;
			_color = c;
			callback(_color);
		}

		public override Vector2 InitialSize => new(
			StandardMargin + hueSize + spacing + bedSize + spacing + swatchesWidth + StandardMargin,
			StandardMargin + titleHeight + spacing + bedSize + spacing + alphaSliderHeight + spacing + CloseButSize.y + StandardMargin
		);

		public Dialog_ColorPicker(string title, Color? color, Action<Color?> callback)
		{
			this.title = title;
			this.callback = callback;
			CurrentColor = color;
			doCloseButton = true;
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

			var hueMaterial = Assets.HuesMaterial;
			hueMaterial.SetFloat("_Hue", hue);
			GenUI.DrawTextureWithMaterial(hueRect, Assets.dummyTexture, hueMaterial);

			var bedMaterial = Assets.ColorBedMaterial;
			bedMaterial.SetFloat("_Hue", hue);
			GenUI.DrawTextureWithMaterial(bedRect, Assets.dummyTexture, bedMaterial);

			var x = bedRect.xMin + bedRect.width * sat;
			var y = bedRect.yMax - bedRect.height * light;
			var cursorRect = new Rect(x - 4, y - 4, 8, 8);
			GUI.DrawTexture(cursorRect, Assets.colorMarkerTexture, ScaleMode.ScaleToFit);

			var oldAlpha = _color?.a ?? 0;
			var alpha = Widgets.HorizontalSlider(alphaRect, oldAlpha, 0, 1, true);
			if (oldAlpha != alpha)
			{
				var newColor = _color ?? Color.clear;
				newColor.a = alpha;
				CurrentColor = newColor;
			}

			list.End();

			inRect.xMin += hueSize + spacing + bedSize + spacing;
			list.Begin(inRect);
			list.curY += titleHeight + spacing;

			var colorRect = list.GetRect(colorHeight);
			GUI.DrawTexture(colorRect, Assets.editoBackgroundPattern, ScaleMode.StretchToFill);
			Widgets.DrawBoxSolidWithOutline(colorRect, CurrentColor ?? Color.clear, IsDragging ? Color.white : borderEmptyColor);
			if (LeftMouseDown && IsDragging == false && Mouse.IsOver(colorRect) && tracking == Tracking.Nothing)
				draggedColor = CurrentColor;

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

			HandleTracking(originalInRect, bedRect, hueRect);
		}

		void DoSwatch(Rect swatchesRect, int sx, int sy, ref int draggedTo)
		{
			var size = (swatchesRect.width - (swatchXCount - 1) * swatchSpace) / swatchXCount;
			var rx = swatchesRect.xMin + sx * (size + swatchSpace);
			var ry = swatchesRect.yMin + sy * (size + swatchSpace);
			var swatchRect = new Rect(rx, ry, size, size);
			var n = sy * swatchXCount + sx;
			var over = Mouse.IsOver(swatchRect) && tracking == Tracking.Nothing;
			if (IsDragging && over)
				draggedTo = n;
			var borderColor = swatches[n].HasValue ? borderFullColor : borderEmptyColor;
			if (swatches[n].HasValue)
				GUI.DrawTexture(swatchRect, Assets.swatchBackgroundPattern, ScaleMode.StretchToFill);
			Widgets.DrawBoxSolidWithOutline(swatchRect, swatches[n] ?? Color.clear, draggedTo == n ? Color.white : borderColor);
			if (LeftMouseDown && IsDragging == false && over)
			{
				draggedColor = swatches[n];
				draggedSwatch = n;
			}
			if (RightMouseDown && over)
				swatches[n] = null;
			if (Widgets.ButtonInvisible(swatchRect) && swatches[n].HasValue && RightMouseDown == false)
				CurrentColor = swatches[n];
		}

		static void LoadSwatches()
		{
			Array.Fill(swatches, null);
			var path = Path.Combine(GenFilePaths.ConfigFolderPath, swatchesFileName);
			if (File.Exists(path) == false)
				return;
			swatches = File.ReadAllText(path).Split('\n')
				.Where(l => l.NullOrEmpty() == false)
				.Select(l => l == "undefined" ? [] : l.Split(' ').Select(s => ParseHelper.ParseFloat(s)).ToArray())
				.Select(p => p.Length == 0 ? (Color?)null : new Color(p[0], p[1], p[2], p[3]))
				.ToArray();
		}

		static void SaveSwatches()
		{
			var text = swatches.Join(c => c.HasValue ? $"{c.Value.r} {c.Value.g} {c.Value.b} {c.Value.a}" : "undefined", "\n");
			var path = Path.Combine(GenFilePaths.ConfigFolderPath, swatchesFileName);
			File.WriteAllText(path, text);
		}

		void HandleTracking(Rect inRect, Rect bedRect, Rect hueRect)
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

			if (LeftMouseDown && tracking == Tracking.Nothing)
			{
				if (Mouse.IsOver(bedRect))
				{
					tracking = Tracking.ColorBed;
					Event.current.Use();
				}
				if (Mouse.IsOver(hueRect))
				{
					tracking = Tracking.Hues;
					Event.current.Use();
				}
			}

			var mousePosition = Event.current.mousePosition;
			switch (tracking)
			{
				case Tracking.Hues:
					{
						hue = Mathf.Clamp01((mousePosition.y - hueRect.yMin) / hueRect.height);
						UpdateHSL(hue, sat, light);
						if (targetSwatch > -1)
							swatches[targetSwatch] = CurrentColor;
						break;
					}
				case Tracking.ColorBed:
					{
						sat = Mathf.Clamp01((mousePosition.x - bedRect.xMin) / bedRect.width);
						light = 1 - Mathf.Clamp01((mousePosition.y - bedRect.yMin) / bedRect.height);
						UpdateHSL(hue, sat, light);
						if (targetSwatch > -1)
							swatches[targetSwatch] = CurrentColor;
						break;
					}
			}
		}
	}
}