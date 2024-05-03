using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	[HarmonyPatch(typeof(UIRoot_Entry))]
	[HarmonyPatch(nameof(UIRoot_Entry.Init))]
	static class UIRoot_Entry_Init_Patch
	{
		public static void Postfix()
		{
			Find.WindowStack.Add(new LoadingDialog("Silhouette Border", new Color(Rand.Range(0f, 1f), Rand.Range(0f, 1f), Rand.Range(0f, 1f))));
		}
	}

	[StaticConstructorOnStartup]
	public class LoadingDialog : Window
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

		static readonly Texture2D texture = new(1, 1);
		static readonly Color borderColor = Color.white.ToTransparent(0.2f);
		static readonly Vector2 swatchSize = new(16, 16);

		static Color[] swatches = new Color[swatchXCount * swatchYCount];
		static bool MouseButtonDown => UnityGUIBugsFixer.IsLeftMouseButtonPressed();

		Tracking tracking = Tracking.Nothing;
		readonly string title;

		float hue, sat, light;
		Color _color;
		Color? draggedColor = null;
		int targetSwatch = -1;

		Color CurrentColor { get => _color; set { _color = value; Color.RGBToHSV(value, out hue, out sat, out light); } }

		public override Vector2 InitialSize => new(
			StandardMargin + hueSize + spacing + bedSize + spacing + swatchesWidth + StandardMargin,
			StandardMargin + titleHeight + spacing + bedSize + spacing + CloseButSize.y + StandardMargin
		);

		public LoadingDialog(string title, Color color)
		{
			this.title = title;
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
			GUI.DrawTexture(cursorRect, Tools.colorMarkerTexture, ScaleMode.ScaleToFit);

			list.End();

			inRect.xMin += hueSize + spacing + bedSize + spacing;
			list.Begin(inRect);
			list.curY += titleHeight + spacing;

			var colorRect = list.GetRect(colorHeight);
			Widgets.DrawBoxSolidWithOutline(colorRect, CurrentColor, draggedColor.HasValue ? Color.white : borderColor);
			if (MouseButtonDown && Mouse.IsOver(colorRect))
				draggedColor = CurrentColor;

			list.Gap(spacing);

			var swatchesRect = list.GetRect(inRect.height - list.curY - spacing - CloseButSize.y);
			var sw = (swatchesRect.width - (swatchXCount - 1) * swatchSpace) / swatchXCount;
			var sh = (swatchesRect.height - (swatchYCount - 1) * swatchSpace) / swatchYCount;
			var draggedTo = -1;
			for (var sy = 0; sy < swatchYCount; sy++)
				for (var sx = 0; sx < swatchXCount; sx++)
				{
					var rx = swatchesRect.xMin + sx * (sw + swatchSpace);
					var ry = swatchesRect.yMin + sy * (sh + swatchSpace);
					var swatchRect = new Rect(rx, ry, sw, sh);
					var n = sy * swatchXCount + sx;
					if (draggedColor.HasValue && Mouse.IsOver(swatchRect))
						draggedTo = n;
					Widgets.DrawBoxSolidWithOutline(swatchRect, swatches[n], draggedTo == n ? Color.white : borderColor);
					if (Widgets.ButtonInvisible(swatchRect))
						CurrentColor = swatches[n];
				}
			targetSwatch = draggedTo;

			list.End();

			if (draggedColor.HasValue)
			{
				var swatchRect = new Rect(Event.current.mousePosition - swatchSize / 2, swatchSize);
				Widgets.DrawBoxSolidWithOutline(swatchRect, draggedColor.Value, Color.white);
			}

			HandleTracking(originalInRect, bedRect, hueRect);
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
			if (MouseButtonDown && tracking == Tracking.Nothing)
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

			if (MouseButtonDown == false || Mouse.IsOver(inRect) == false)
			{
				tracking = Tracking.Nothing;
				if (targetSwatch > -1 && draggedColor.HasValue)
					swatches[targetSwatch] = draggedColor.Value;
				targetSwatch = -1;
				draggedColor = null;
			}

			var mousePosition = Event.current.mousePosition;
			switch (tracking)
			{
				case Tracking.Hues:
					{
						hue = Mathf.Clamp01((mousePosition.y - hueRect.yMin) / hueRect.height);
						_color = Color.HSVToRGB(hue, sat, light);
						if (targetSwatch > -1)
							swatches[targetSwatch] = CurrentColor;
						break;
					}
				case Tracking.ColorBed:
					{
						sat = Mathf.Clamp01((mousePosition.x - bedRect.xMin) / bedRect.width);
						light = 1 - Mathf.Clamp01((mousePosition.y - bedRect.yMin) / bedRect.height);
						_color = Color.HSVToRGB(hue, sat, light);
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