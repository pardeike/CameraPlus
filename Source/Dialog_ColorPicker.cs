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
			Nothing,
			Hues,
			ColorBed,
			Swatch
		}

		const string swatchesFileName = "CameraPlusColors.txt";
		const int titleHeight = 35;
		const int hueSize = 40;
		const int bedSize = 320;
		const int swatchesWidth = 160;
		const int swatchXCount = 5;
		const int swatchYCount = 8;
		const int swatchSpace = 5;
		const int colorHeight = 60;
		const float spacing = 10f;

		static Color[] swatches = new Color[swatchXCount * swatchYCount];

		static readonly Texture2D texture = new(1, 1);
		static readonly Color borderEmptyColor = Color.white.ToTransparent(0.1f);
		static readonly Color borderFullColor = Color.white.ToTransparent(0.6f);
		static readonly Vector2 swatchSize = new(16, 16);
		static bool LeftMouseDown => Input.GetMouseButton(0);
		static bool RightMouseDown => Input.GetMouseButton(1);

		Tracking tracking = Tracking.Nothing;
		readonly string title;
		readonly Action<Color> callback;

		float hue, sat, light;
		Color _color;
		Color? draggedColor = null;
		int draggedSwatch = -1;
		int targetSwatch = -1;

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

		public override Vector2 InitialSize => new(
			StandardMargin + hueSize + spacing + bedSize + spacing + swatchesWidth + StandardMargin,
			StandardMargin + titleHeight + spacing + bedSize + spacing + CloseButSize.y + StandardMargin
		);

		public Dialog_ColorPicker(string title, Color color, Action<Color> callback)
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

		public override void PostClose()
		{
			base.PostClose();
			UnityEngine.Object.Destroy(texture);
		}

		public override void DoWindowContents(Rect inRect)
		{
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

			var material = Assets.HuesMaterial;
			material.SetFloat("_Hue", hue);
			GenUI.DrawTextureWithMaterial(hueRect, texture, material);

			material = Assets.ColorBedMaterial;
			material.SetFloat("_Hue", hue);
			GenUI.DrawTextureWithMaterial(bedRect, texture, material);

			var x = bedRect.xMin + bedRect.width * sat;
			var y = bedRect.yMax - bedRect.height * light;
			var cursorRect = new Rect(x - 4, y - 4, 8, 8);
			GUI.DrawTexture(cursorRect, Assets.colorMarkerTexture, ScaleMode.ScaleToFit);

			list.End();

			inRect.xMin += hueSize + spacing + bedSize + spacing;
			list.Begin(inRect);
			list.curY += titleHeight + spacing;

			var colorRect = list.GetRect(colorHeight);
			Widgets.DrawBoxSolidWithOutline(colorRect, CurrentColor, IsDragging ? Color.white : borderEmptyColor);
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

		private void DoSwatch(Rect swatchesRect, int sx, int sy, ref int draggedTo)
		{
			var sw = (swatchesRect.width - (swatchXCount - 1) * swatchSpace) / swatchXCount;
			var sh = (swatchesRect.height - (swatchYCount - 1) * swatchSpace) / swatchYCount;
			var rx = swatchesRect.xMin + sx * (sw + swatchSpace);
			var ry = swatchesRect.yMin + sy * (sh + swatchSpace);
			var swatchRect = new Rect(rx, ry, sw, sh);
			var n = sy * swatchXCount + sx;
			var over = Mouse.IsOver(swatchRect) && tracking == Tracking.Nothing;
			if (IsDragging && over)
				draggedTo = n;
			var borderColor = swatches[n].a == 0 ? borderEmptyColor : borderFullColor;
			Widgets.DrawBoxSolidWithOutline(swatchRect, swatches[n], draggedTo == n ? Color.white : borderColor);
			if (LeftMouseDown && IsDragging == false && over)
			{
				draggedColor = swatches[n];
				draggedSwatch = n;
			}
			if (Widgets.ButtonInvisible(swatchRect) && swatches[n].a > 0)
				CurrentColor = swatches[n];
			if (RightMouseDown && over)
				swatches[n] = new Color(0, 0, 0, 0);
		}

		static void LoadSwatches()
		{
			Array.Fill(swatches, new Color(0, 0, 0, 0));
			var path = Path.Combine(GenFilePaths.ConfigFolderPath, swatchesFileName);
			if (File.Exists(path) == false)
				return;
			swatches = File.ReadAllText(path).Split('\n')
				.Where(l => l.NullOrEmpty() == false)
				.Select(l => l.Split(' ').Select(s => ParseHelper.ParseFloat(s)).ToArray())
				.Select(p => new Color(p[0], p[1], p[2], p[3]))
				.ToArray();
		}

		static void SaveSwatches()
		{
			var text = swatches.Join(c => $"{c.r} {c.g} {c.b} {c.a}", "\n");
			var path = Path.Combine(GenFilePaths.ConfigFolderPath, swatchesFileName);
			File.WriteAllText(path, text);
		}

		void HandleTracking(Rect inRect, Rect bedRect, Rect hueRect)
		{
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
						_color = Color.HSVToRGB(hue, sat, light);
						callback(_color);
						if (targetSwatch > -1)
							swatches[targetSwatch] = CurrentColor;
						break;
					}
				case Tracking.ColorBed:
					{
						sat = Mathf.Clamp01((mousePosition.x - bedRect.xMin) / bedRect.width);
						light = 1 - Mathf.Clamp01((mousePosition.y - bedRect.yMin) / bedRect.height);
						_color = Color.HSVToRGB(hue, sat, light);
						callback(_color);
						if (targetSwatch > -1)
							swatches[targetSwatch] = CurrentColor;
						break;
					}
				case Tracking.Swatch:
					{
						break;
					}
				default:
					break;
			}
		}
	}
}