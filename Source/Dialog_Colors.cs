using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CameraPlus
{
	public class Dialog_Colors : Window
	{
		const float spacing = 4;

		const float dialogWidth = 1200;
		const float dialogHeight = 640;
		const float headerHeight = 30;
		const float rowHeight = 50;
		const float rowSpacing = 20;
		const float columnSpacing = 10;
		const float scrollbarWidth = 20f;
		const float actionButtonsWidth = 24f;

		static readonly float[] columnRatios = [4f, 1f, 1.5f, 0.5f, 0.5f, 0.5f, 1f, 1f, 1f];
		static readonly float ratioSum = columnRatios.Sum();
		static readonly float columnAvailableSpace = dialogWidth - 2 * 18f - scrollbarWidth - columnRatios.Length * columnSpacing - actionButtonsWidth;
		static readonly float[] columnWidths = columnRatios.Select(f => columnAvailableSpace * f / ratioSum).ToArray();

		static readonly Color borderColor = Color.gray.ToTransparent(0.5f);
		static readonly Color dragColor = new(1f, 220f / 255, 56f / 255);
		static readonly string[] columnHeaders = ["Conditions", "Mode", "Colors", "Map", "Edge", "Mouse hide", "Show below", "Size", "Border"];

		public override Vector2 InitialSize => new(dialogWidth, dialogHeight);
		private static List<DotConfig> dotConfigs;
		private static Vector2 scrollPosition = Vector2.zero;
		private static ValueChanger valueChanger = null;
		private static int rowToDelete = -1;
		private static int rowDragged = -1;
		private static int rowInsert = -1;

		public Dialog_Colors()
		{
			dotConfigs = Find.World.GetComponent<CameraSettings>().dotConfigs;
			doCloseButton = true;
			// draggable = true;
		}

		public void Tick()
		{
			if (Input.GetMouseButton(0) == false)
			{
				valueChanger = null;

				if (rowInsert > -1)
				{
					var dotConfig = dotConfigs[rowDragged];
					dotConfigs.RemoveAt(rowDragged);
					if (rowInsert >= rowDragged) rowInsert--;
					if (rowInsert >= dotConfigs.Count)
						dotConfigs.Add(dotConfig);
					else
						dotConfigs.Insert(rowInsert, dotConfig);
				}
				rowDragged = -1;
				rowInsert = -1;
			}

			if (rowToDelete > -1)
			{
				dotConfigs.RemoveAt(rowToDelete);
				rowToDelete = -1;
			}

			valueChanger?.Tick();
		}

		static bool ShouldInsert(float curY, int row)
			=> row > -1
			&& rowDragged > -1
			&& row != rowDragged
			&& row != rowDragged + 1
			&& Mathf.Abs(Event.current.mousePosition.y - curY) < 20f;

		static void PrepareRow(Listing_Standard list, float height, int row, out Rect[] columns, out Rect actionButtons)
		{
			if (row > 0)
			{
				var shouldInsert = ShouldInsert(list.curY, row);
				if (shouldInsert)
					rowInsert = row;

				var color = shouldInsert ? dragColor : Color.white.ToTransparent(0.2f);
				var lineWidth = shouldInsert ? 2f : 1f;

				list.Gap((rowSpacing - lineWidth) / 2);
				var line = list.GetRect(lineWidth);
				line.width -= columnSpacing + actionButtonsWidth;
				Widgets.DrawBoxSolid(line, color);
				list.Gap((rowSpacing - lineWidth) / 2);
			}

			var x = 0f;
			var rect = list.GetRect(height);
			columns = columnWidths.Select(w =>
			{
				var column = new Rect(x, rect.y, w, rect.height);
				x += w + columnSpacing;
				return column;
			})
			.ToArray();

			actionButtons = new Rect(rect.width - actionButtonsWidth, rect.y, actionButtonsWidth, rect.height);
		}

		static void DrawConditionTags(Rect rect, List<ConditionTag> tags)
		{
			var font = Text.Font;
			Text.Font = GameFont.Tiny;
			var allTags = tags.Union([new TagAddButton()]).ToList();
			GenUI.DrawElementStack(rect, Text.LineHeightOf(GameFont.Tiny), allTags, (rect, tag) => tag.Draw(rect, delegate ()
			{
				if (tag is TagAddButton)
					Find.WindowStack.Add(new Dialog_AddTag(newTag => tags.Add(newTag.Clone())));
				else
					LongEventHandler.ExecuteWhenFinished(() => tags.RemoveWhere(t => t == tag));
			}),
			ConditionTag.WidthGetter, 2, 2, false);
			Text.Font = font;
		}

		static void DrawColorButton(Rect rect, string title, Color color, Action<Color> newColorCallback)
		{
			Widgets.DrawBoxSolidWithOutline(rect, color, borderColor, 2);
			var deleteRect = rect.RightPartPixels(rect.height).ExpandedBy(-4);
			GUI.color = Color.white;
			var current = Event.current;
			if (Widgets.ButtonInvisible(rect))
				Find.WindowStack.Add(new Dialog_ColorPicker(title, color, newColorCallback));
		}

		static void DrawDot(Rect rect, Color outerColor, Color innerColor, bool selected)
		{
			if (selected)
				GUI.DrawTexture(rect, Assets.bracket);
			GUI.color = outerColor;
			GUI.DrawTexture(rect.ContractedBy(4).Rounded(), Assets.outerColonistTexture);
			GUI.color = innerColor;
			GUI.DrawTexture(rect.ContractedBy(4).Rounded(), Assets.innerColonistTexture);
			GUI.color = Color.white;
		}

		static string DotModeName(DotMode mode) => $"DotMode{mode}".Translate();

		static void DrawMode(Rect rect, DotConfig dotConfig)
		{
			DrawLabel(rect, DotModeName(dotConfig.mode));
			if (Widgets.ButtonInvisible(rect))
			{
				var modes = (DotMode[])Enum.GetValues(typeof(DotMode));
				var options = modes.Select(mode => new FloatMenuOption(DotModeName(mode), () => dotConfig.mode = mode));
				Find.WindowStack.Add(new FloatMenu(options.ToList()));
			}
		}

		static void DrawColors(Rect rect, DotConfig dotConfig)
		{
			GUI.BeginGroup(rect);
			var h = (rect.height - spacing) / 2;
			var w = (rect.width - h - 2 * spacing) / 2;
			var preview1 = new Rect(0, 0, h, h);
			var preview2 = new Rect(0, spacing + h, h, h);
			DrawDot(preview1, dotConfig.lineColor, dotConfig.fillColor, false);
			DrawDot(preview2, dotConfig.lineSelectedColor, dotConfig.fillSelectedColor, true);
			var color1 = new Rect(h + spacing, 0, w, h);
			var color2 = new Rect(h + 2 * spacing + w, 0, w, h);
			var color3 = new Rect(h + spacing, spacing + h, w, h);
			var color4 = new Rect(h + 2 * spacing + w, spacing + h, w, h);
			DrawColorButton(color1, $"{dotConfig.name}, {"Outline".Translate()}", dotConfig.lineColor, c => dotConfig.lineColor = c);
			DrawColorButton(color2, $"{dotConfig.name}, {"Fill".Translate()}", dotConfig.fillColor, c => dotConfig.fillColor = c);
			var selected = "Selected".Translate();
			DrawColorButton(color3, $"{dotConfig.name}, {"Outline".Translate()}, {selected}", dotConfig.lineSelectedColor, c => dotConfig.lineSelectedColor = c);
			DrawColorButton(color4, $"{dotConfig.name}, {"Fill".Translate()}, {selected}", dotConfig.fillSelectedColor, c => dotConfig.fillSelectedColor = c);
			GUI.EndGroup();
		}

		static void DrawCheckbox(Rect rect, ref bool value)
		{
			GUI.BeginGroup(rect);
			var x = (rect.width - 24) / 2;
			var y = (rect.height - 24) / 2;
			Widgets.Checkbox(x, y, ref value, 24, false, true);
			GUI.EndGroup();
		}

		static void DrawLabel(Rect rect, string text)
		{
			var font = Text.Font;
			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect, text);
			Text.Anchor = TextAnchor.UpperLeft;
			Text.Font = font;
		}

		static void DrawStepper<T>(Rect rect, ref T value, Func<T, T> incr, Func<T, T> decr, Action<object, int> deltaCallback, Func<T, string> stringConverter)
		{
			GUI.BeginGroup(rect);
			var s = stringConverter(value);
			var size = Text.CalcSize(s);
			var sw = Assets.steppers[0].width;
			var sh = Assets.steppers[0].height;
			var dh = (rect.height - 2 * sh) / 2;
			var dx = (rect.width - sw - spacing - size.x) / 2;
			var dy = (rect.height - size.y) / 2;
			var textRect = new Rect(dx, dy, size.x, size.y);
			Widgets.Label(textRect, s);
			if (Mouse.IsOver(textRect) && Event.current.type == EventType.MouseDown && Input.GetMouseButtonDown(0))
			{
				valueChanger = new ValueChanger(value, 10, deltaCallback);
				Event.current.Use();
			}
			var up = new Rect(rect.width - dx - sw, dh, sw, sh);
			if (Widgets.ButtonImageFitted(up, Assets.steppers[0]))
				value = incr(value);
			var down = new Rect(rect.width - dx - sw, dh + sh, sw, sh);
			if (Widgets.ButtonImageFitted(down, Assets.steppers[1]))
				value = decr(value);
			GUI.EndGroup();
		}

		static int Px(int v, int d) => Mathf.Max(-1, v + d);
		static float Pc(float v, float d) => Mathf.Max(0, v + d);
		static void DeltaInt(object v, int delta, int step, ref int val) => val = Mathf.Max(-1, (int)v + delta * step);
		static void DeltaFloat(object v, float delta, float step, ref float val) => val = Mathf.Max(0, (float)v + delta * step);

		static void ColorEditorRow(Listing_Standard list, int row, DotConfig dotConfig)
		{
			PrepareRow(list, rowHeight, row, out var columnRects, out var actionButtons);

			if (rowDragged == row)
				Widgets.DrawBoxSolid(columnRects[0].Union(columnRects[8]), dragColor.ToTransparent(0.2f));

			DrawConditionTags(columnRects[0], dotConfig.conditions);
			DrawMode(columnRects[1], dotConfig);
			DrawColors(columnRects[2], dotConfig);
			DrawCheckbox(columnRects[3], ref dotConfig.useInside);
			DrawCheckbox(columnRects[4], ref dotConfig.useEdge);
			DrawCheckbox(columnRects[5], ref dotConfig.hideOnMouseover);
			DrawStepper(columnRects[6], ref dotConfig.showBelowPixels, v => Px(v, 1), v => Px(v, -1), (v, d) => DeltaInt(v, d, 1, ref dotConfig.showBelowPixels), v => v == -1 ? "default" : $"{v} px");
			DrawStepper(columnRects[7], ref dotConfig.relativeSize, v => Pc(v, 0.01f), v => Pc(v, -0.01f), (v, d) => DeltaFloat(v, d, 0.01f, ref dotConfig.relativeSize), v => $"{(int)(v * 100 + 0.5f)}%");
			DrawStepper(columnRects[8], ref dotConfig.outlineFactor, v => Pc(v, 0.01f), v => Pc(v, -0.01f), (v, d) => DeltaFloat(v, d, 0.01f, ref dotConfig.outlineFactor), v => $"{(int)(v * 100 + 0.5f)}%");

			var dragStartRect = actionButtons.TopPartPixels(actionButtonsWidth);
			var mouseOver = Mouse.IsOver(dragStartRect);
			GUI.color = mouseOver ? GenUI.MouseoverColor : Color.white;
			GUI.DrawTexture(dragStartRect, TexButton.DragHash);
			GUI.color = Color.white;
			if (mouseOver && Event.current.type == EventType.MouseDown && Input.GetMouseButtonDown(0))
			{
				rowDragged = row;
				Event.current.Use();
			}

			if (Widgets.ButtonImage(actionButtons.BottomPartPixels(actionButtonsWidth), TexButton.Delete))
				rowToDelete = row;
		}

		public override void DoWindowContents(Rect inRect)
		{
			Tick();

			inRect.yMax -= 40; // close button footer

			var list = new Listing_Standard();
			list.Begin(inRect);

			PrepareRow(list, headerHeight, -1, out var columnRects, out _);
			for (var i = 0; i < columnHeaders.Length; i++)
				DrawLabel(columnRects[i], columnHeaders[i]);
			list.Gap(rowSpacing);
			var outRect = list.GetRect(inRect.height - list.curY - rowSpacing - 30);

			var configCount = dotConfigs.Count;
			var viewRect = new Rect(0, 0, inRect.width - scrollbarWidth, rowHeight * configCount + rowSpacing * configCount);
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

			var innerList = new Listing_Standard();
			innerList.Begin(viewRect);
			for (var row = 0; row < configCount; row++)
				ColorEditorRow(innerList, row, dotConfigs[row]);
			if (ShouldInsert(innerList.curY, configCount))
			{
				rowInsert = configCount;
				var line = new Rect(0, innerList.curY + rowSpacing / 2 - 1, innerList.ColumnWidth - columnSpacing - actionButtonsWidth, 2f);
				Widgets.DrawBoxSolid(line, dragColor);
			}
			innerList.End();

			Widgets.EndScrollView();
			if (scrollPosition.y == 0 && ShouldInsert(outRect.y, 0))
			{
				rowInsert = 0;
				var line = new Rect(outRect.x, outRect.y - rowSpacing / 2 - 1, viewRect.width - columnSpacing - actionButtonsWidth, 2f);
				Widgets.DrawBoxSolid(line, dragColor);
			}

			list.Gap(rowSpacing);
			if (list.ButtonText("New".TranslateSimple(), null, 0.2f))
				dotConfigs.Add(new DotConfig());

			list.End();
		}
	}
}