using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;
using Verse;
using Verse.Sound;
using static CameraPlus.CameraPlusMain;

namespace CameraPlus
{
	class Tools
	{
		public static bool IsHiddenFromPlayer(Pawn pawn) => pawn?.Map == null || pawn.Map.fogGrid.IsFogged(pawn.Position) || InvisibilityUtility.IsHiddenFromPlayer(pawn);
		public static string DefaultRulesFilePath => Path.Combine(GenFilePaths.ConfigFolderPath, "CameraPlusDefaultRules.xml");

		public static Color GetMainColor(Pawn pawn)
		{
			var renderer = pawn.Drawer.renderer;
			var pawnRenderFlags = renderer.DefaultRenderFlagsNow | PawnRenderFlags.Clothes | PawnRenderFlags.Headgear;
			renderer.renderTree.EnsureInitialized(PawnRenderFlags.DrawNow);
			if (renderer.renderTree.nodesByTag.TryGetValue(PawnRenderNodeTagDefOf.Body, out var bodyNode) == false)
				return Color.clear;

            var graphic = renderer.BodyGraphic;

			var key = pawn.GetType().FullName + ":" + graphic.path;
			if (Caches.cachedMainColors.TryGetValue(key, out var color) == false)
			{
				color = Color.clear;

				if (graphic.color != Color.white)
				{
					color = graphic.color;
					goto SET_COLOR;
				}

				var material = graphic.MatEast ?? graphic.MatSingle;
				if (material == null)
				{
					color = Color.clear;
					goto SET_COLOR;
				}

				var texture = (Texture2D)material.mainTexture;
				if (texture == null)
				{
					color = Color.clear;
					goto SET_COLOR;
				}

				var readableTexture = Tools.DownsampleTexture(texture, 16);
				var combinedColors = readableTexture
					.GetPixels32()
					.Where(c => c.a == 255 && c.r + c.g + c.b > 0)
					.GroupBy(color => color)
					.OrderByDescending(group => group.Count())
					.Select(group => (color: group.Key, hsl: Tools.HSL(group.Key), count: group.Count()))
					.ToList().ToArray(); // Disambiguate ToArray by converting to List first
				if (combinedColors.Length == 0)
				{
					color = Color.clear;
					goto SET_COLOR;
				}

				var colorIndex = combinedColors.FirstIndexOf(tuple => tuple.hsl.s > 0.2 && tuple.hsl.l < 0.8 && tuple.hsl.l > 0.2);
				if (combinedColors.Length > 0 && colorIndex == 1 && combinedColors[0].hsl.s < 0.2)
					color = combinedColors[1].color;
				else
					color = combinedColors[0].color;

				SET_COLOR:
				Caches.cachedMainColors[key] = color;
			}
			return color;
		}

		public static Texture2D DownsampleTexture(Texture2D nonReadableTexture, int n)
		{
			RenderTexture tempRT = RenderTexture.GetTemporary(n, n, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			tempRT.filterMode = FilterMode.Point;
			Graphics.Blit(nonReadableTexture, tempRT);
			RenderTexture previous = RenderTexture.active;
			RenderTexture.active = tempRT;
			Texture2D readableTexture = new Texture2D(n, n, TextureFormat.ARGB32, false);
			readableTexture.ReadPixels(new Rect(0, 0, n, n), 0, 0);
			readableTexture.Apply();
			RenderTexture.active = previous;
			RenderTexture.ReleaseTemporary(tempRT);
			return readableTexture;
		}

		public static (float h, float s, float l) HSL(Color color)
		{
			var (r, g, b) = (color.r, color.g, color.b);
			var max = Mathf.Max(r, Mathf.Max(g, b));
			var min = Mathf.Min(r, Mathf.Min(g, b));
			float h, s, l;
			l = (max + min) / 2;
			if (max == min)
				h = s = 0; // achromatic
			else
			{
				var d = max - min;
				s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
				if (max == r)
					h = (g - b) / d + (g < b ? 6 : 0);
				else if (max == g)
					h = (b - r) / d + 2;
				else
					h = (r - g) / d + 4;
				h /= 6;
			}
			return (h, s, l);
		}

		public static float MouseDistanceSquared(Vector3 pos, bool mapCoordinates)
		{
			if (mapCoordinates)
			{
				var mouse = FastUI.MouseMapPosition;
				var dx1 = mouse.x - pos.x;
				var dz = mouse.z - pos.z;
				return dx1 * dx1 + dz * dz;
			}
			else
			{
				var mouse = FastUI.MousePositionOnUIInverted;
				var len = FastUI.CurUICellSize;
				mouse.y += len / 2;
				var dx2 = mouse.x - pos.x;
				var dy = mouse.y - pos.y;
				var delta = dx2 * dx2 + dy * dy;
				return delta / len / len;
			}
		}

		public static void DefaultMarkerTextures(Pawn pawn, out Texture2D innerTexture, out Texture2D outerTexture)
		{
			if (pawn.IsEntity)
			{
				innerTexture = Assets.innerEntityTexture;
				outerTexture = Assets.outerEntityTexture;
				return;
			}

			if (pawn.IsColonistPlayerControlled == false)
			{
				innerTexture = Assets.innerColonistTexture;
				outerTexture = Assets.outerColonistTexture;
				return;
			}

			var isAnimal = pawn.RaceProps.Animal;
			var customAnimalStyle = Settings.customNameStyle == LabelStyle.AnimalsDifferent;
			innerTexture = isAnimal && customAnimalStyle ? Assets.innerAnimalTexture : Assets.innerColonistTexture;
			outerTexture = isAnimal && customAnimalStyle ? Assets.outerAnimalTexture : Assets.outerColonistTexture;
		}

		public static bool ShouldShowLabel(Pawn pawn, Vector2 screenPos = default)
		{
			if (Settings.dotStyle <= DotStyle.VanillaDefault)
				return true;
			if (Settings.mouseOverShowsLabels && MouseDistanceSquared(pawn?.DrawPos ?? screenPos, pawn != null) <= 2.25f) // TODO
				return true;
			return pawn == null ? FastUI.CurUICellSize > Settings.hideThingLabelBelow : Caches.shouldShowLabelCache.Get(pawn);
		}

		public static float LerpDoubleSafe(float inFrom, float inTo, float outFrom, float outTo, float x)
		{
			if (inFrom == inTo)
				return (outFrom + outTo) / 2;
			return GenMath.LerpDouble(inFrom, inTo, outFrom, outTo, x);
		}

		static bool ArrayEquals<T>(T[] a, T[] b)
		{
			if (a.Length != b.Length)
				return false;

			for (int i = 0; i < a.Length; i++)
				if (a[i].Equals(b[i]) == false)
					return false;

			return true;
		}

		public static bool ContainsCaseInsensitive(string source, string fragment)
			=> source?.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;

		public static bool ContainsCaseInsensitive(Def def, string fragment)
			=> def?.label.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;

		public static void ScribeArrays<T>(ref T[] codes, string name, T[] defaults)
		{
			codes ??= defaults;
			if (Scribe.mode == LoadSaveMode.Saving && ArrayEquals(codes, defaults))
				return;
			var list = codes.ToList();
			Scribe_Collections.Look(ref list, name, typeof(T) == typeof(OptionalColor) ? LookMode.Deep : LookMode.Value, System.Array.Empty<T>());
			codes = list?.ToList().ToArray() ?? System.Array.Empty<T>(); // Disambiguate ToArray by converting to List first
			if (codes.Length == 0)
				codes = defaults;
		}

		public static string ScribeToString(IExposable exposable, string rootElementName = null)
		{
			Scribe.mode = LoadSaveMode.Saving;
			using var memoryStream = new MemoryStream();
			var xmlWriterSettings = new XmlWriterSettings { Indent = true, IndentChars = "\t" };
			using (var xmlWriter = XmlWriter.Create(memoryStream, xmlWriterSettings))
			{
				Scribe.saver.saveStream = memoryStream;
				Scribe.saver.writer = xmlWriter;
				Scribe.saver.writer.WriteStartDocument();
				Scribe.saver.EnterNode(rootElementName ?? exposable.GetType().Name);
				exposable.ExposeData();
				Scribe.saver.ExitNode();
				Scribe.saver.writer.WriteEndDocument();
				Scribe.saver.writer.Flush();
			}
			memoryStream.Seek(0, SeekOrigin.Begin);
			using var reader = new StreamReader(memoryStream, Encoding.UTF8);
			return reader.ReadToEnd();
		}

		public static T ScribeFromString<T>(string xml) where T : IExposable, new()
		{
			try
			{
				using var memoryStream = new MemoryStream();
				using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
				writer.Write(xml);
				writer.Flush();
				memoryStream.Seek(0, SeekOrigin.Begin);
				using var xmlTextReader = new XmlTextReader(memoryStream);
				var xmlDocument = new XmlDocument();
				xmlDocument.Load(xmlTextReader);
				Scribe.loader.curXmlParent = xmlDocument.DocumentElement;
				Scribe.mode = LoadSaveMode.LoadingVars;
				var value = new T();
				value.ExposeData();
				Scribe.loader.FinalizeLoading();
				Scribe.loader.curXmlParent = null;
				Scribe.mode = LoadSaveMode.Inactive;
				return value;
			}
			catch (Exception ex)
			{
				Log.Warning($"Error reading {typeof(T)} from xml: {ex}");
				return default;
			}
		}

		public static float LerpRootSize(float x)
		{
			var n = Settings.exponentiality;
			if (n == 0)
				return LerpDoubleSafe(CameraPlusSettings.minRootInput, CameraPlusSettings.maxRootInput, CameraPlusSettings.minRootResult, CameraPlusSettings.maxRootResult, x);

			if (CameraPlusSettings.minRootResult == CameraPlusSettings.maxRootResult)
				return CameraPlusSettings.minRootResult;
			var factor = (CameraPlusSettings.maxRootResult - CameraPlusSettings.minRootResult) / Mathf.Pow(CameraPlusSettings.maxRootInput - CameraPlusSettings.minRootInput, 2 * n);
			var y = CameraPlusSettings.minRootResult + Mathf.Pow(x - CameraPlusSettings.minRootInput, 2 * n) * factor;
			return (float)y;
		}

		public static float GetDollyRateKeys(float orthSize)
		{
			var f = GetScreenEdgeDollyFactor(orthSize);
			var zoomedIn = Settings.zoomedInDollyPercent * f / 9f;
			var zoomedOut = Settings.zoomedOutDollyPercent * f * 1.1f;
			return LerpDoubleSafe(CameraPlusSettings.minRootResult, CameraPlusSettings.maxRootResult, zoomedIn, zoomedOut, orthSize);
		}

		public static float GetScreenEdgeDollyFactor(float orthSize)
		{
			var zoomedIn = Settings.zoomedInScreenEdgeDollyFactor * 30;
			var zoomedOut = Settings.zoomedOutScreenEdgeDollyFactor * 30;
			return LerpDoubleSafe(CameraPlusSettings.minRootResult, CameraPlusSettings.maxRootResult, zoomedIn, zoomedOut, orthSize);
		}

		public static float GetDollyRateMouse(float orthSize)
		{
			var zoomedIn = 1f * Settings.zoomedInDollyPercent;
			var zoomedOut = 10f * Settings.zoomedOutDollyPercent;
			return LerpDoubleSafe(CameraPlusSettings.minRootResult, CameraPlusSettings.maxRootResult, zoomedIn, zoomedOut, orthSize);
		}

		public static float GetDollySpeedDecay(float orthSize)
		{
			// 0.15f comes from the old zoomedInDollyFrictionPercent/zoomedOutDollyFrictionPercent
			//
			var minVal = 1f - 0.15f;
			var maxVal = 1f - 0.15f;
			return LerpDoubleSafe(CameraPlusSettings.minRootResult, CameraPlusSettings.maxRootResult, minVal, maxVal, orthSize);
		}

		public static List<DotConfig> LoadDotConfigs(string filePath)
		{
			try
			{
				Scribe.loader.InitLoading(filePath);
				var dotConfigs = new List<DotConfig>();
				Scribe_Collections.Look(ref dotConfigs, nameof(dotConfigs), LookMode.Deep);
				Scribe.loader.FinalizeLoading();
				return dotConfigs;
			}
			catch
			{
				Scribe.ForceStop();
				return [];
			}
		}

		public static void SaveDotConfigs(string filePath, List<DotConfig> dotConfigs)
		{
			SafeSaver.Save(filePath, Dialog_CustomizationList_Save.rootElementName, () => Scribe_Collections.Look(ref dotConfigs, nameof(dotConfigs), LookMode.Deep), false);
		}

		public static string ToLabel(KeyCode code)
		{
			return code switch
			{
				// cannot be more optimized because the enum has multiple equal values
				//
				KeyCode.None => "None".Translate(),
				KeyCode.LeftShift => "KeyLeftShift".Translate(),
				KeyCode.LeftAlt => "KeyLeftAlt".Translate(),
				KeyCode.LeftControl => "KeyLeftControl".Translate(),
				KeyCode.LeftCommand => "KeyLeftCommand".Translate(),
				KeyCode.LeftWindows => "KeyLeftWindows".Translate(),
				KeyCode.RightShift => "KeyRightShift".Translate(),
				KeyCode.RightAlt => "KeyRightAlt".Translate(),
				KeyCode.RightControl => "KeyRightControl".Translate(),
				KeyCode.RightCommand => "KeyRightCommand".Translate(),
				KeyCode.RightWindows => "KeyRightWindows".Translate(),
				_ => code.ToStringReadable(),
			};
		}

		public static void KeySettingsButton(Rect rect, bool allKeys, KeyCode setting, KeyCode defaultKey, Action<KeyCode> action)
		{
			if (allKeys)
			{
				TooltipHandler.TipRegionByKey(rect, "BindingButtonToolTip");
				if (Widgets.ButtonText(rect, setting == KeyCode.None ? "" : ToLabel(setting)))
				{
					if (Event.current.button == 0)
					{
						Find.WindowStack.Add(new Dialog_AskForKey(action));
						Event.current.Use();
						return;
					}
					if (Event.current.button == 1)
					{
						var list = new List<FloatMenuOption>
						{
							new("ResetBinding".Translate(), () => action(defaultKey), MenuOptionPriority.Default, null, null, 0f, null, null, true, 0),
							new("ClearBinding".Translate(), () => action(KeyCode.None), MenuOptionPriority.Default, null, null, 0f, null, null, true, 0)
						};
						Find.WindowStack.Add(new FloatMenu(list));
					}
				}
				return;
			}

			if (Widgets.ButtonText(rect, setting == KeyCode.None ? "" : ToLabel(setting)))
			{
				var keys = new List<KeyCode>()
				{
					KeyCode.None,
					KeyCode.LeftShift, KeyCode.LeftAlt, KeyCode.LeftControl, KeyCode.LeftCommand, KeyCode.LeftWindows,
					KeyCode.RightShift, KeyCode.RightAlt, KeyCode.RightControl, KeyCode.RightCommand, KeyCode.RightWindows,
				};
				var choices = keys
					.Select(code => new FloatMenuOption(ToLabel(code), delegate () { action(code); }, MenuOptionPriority.Default, null, null, 0f, null, null))
					.ToList();
				Find.WindowStack.Add(new FloatMenu(choices));
				Event.current.Use();
			}
		}

		static Vector3 snapbackRootPos;
		static float snapbackRootSize = 0;

		public static void CreateSnapback()
		{
			Defs.SnapBack.PlayOneShotOnCamera(null);
			var cameraDriver = Current.cameraDriverInt;
			snapbackRootPos = cameraDriver.rootPos;
			snapbackRootSize = cameraDriver.rootSize;
		}

		public static bool HasSnapback => snapbackRootSize != 0;

		public static void ResetSnapback()
		{
			snapbackRootPos = default;
			snapbackRootSize = default;
		}

		public static void RestoreSnapback()
		{
			var tm = Find.TickManager;
			var savedSpeed = tm.curTimeSpeed;

			IEnumerator ApplyRootPosAndSize()
			{
				yield return new WaitForSeconds(0.35f);
				Current.cameraDriverInt.SetRootPosAndSize(snapbackRootPos, snapbackRootSize);
				ResetSnapback();
				tm.curTimeSpeed = savedSpeed;
			}

			tm.curTimeSpeed = TimeSpeed.Paused;
			Defs.ApplySnap.PlayOneShotOnCamera(null);
			_ = Current.cameraDriverInt.StartCoroutine(ApplyRootPosAndSize());
		}

		public static void HandleHotkeys()
		{
			if (Event.current.type == EventType.Repaint || Current.ProgramState != ProgramState.Playing)
				return;

			KeyCode m1, m2;

			if (Input.GetKey(Settings.cameraSettingsKey))
			{
				m1 = Settings.cameraSettingsMod[0];
				m2 = Settings.cameraSettingsMod[1];
				if (m1 == KeyCode.None && m2 == KeyCode.None)
					return;

				if (m1 == KeyCode.None || Input.GetKey(m1))
					if (m2 == KeyCode.None || Input.GetKey(m2))
					{
						var stack = Find.WindowStack;
						if (stack.IsOpen<Dialog_ModSettings>() == false)
						{
							var me = LoadedModManager.GetMod<CameraPlusMain>();
							var dialog = new Dialog_ModSettings(me);
							stack.Add(dialog);
						}
						Event.current.Use();
						return;
					}
			}

			var numKey = 0;
			for (var i = 1; i <= 9; i++)
				if (Input.GetKey("" + i))
				{
					numKey = i;
					break;
				}
			if (numKey == 0)
				return;

			var map = Current.gameInt.CurrentMap;
			if (map == null)
				return;

			var savedViews = map.GetComponent<SavedViews>();

			m1 = Settings.cameraSettingsLoad[0];
			m2 = Settings.cameraSettingsLoad[1];
			if (m1 != KeyCode.None || m2 != KeyCode.None)
				if (m1 == KeyCode.None || Input.GetKey(m1))
					if (m2 == KeyCode.None || Input.GetKey(m2))
					{
						var view = savedViews.views[numKey - 1];
						if (view != null)
							Current.cameraDriverInt.SetRootPosAndSize(view.rootPos, view.rootSize);
						Event.current.Use();
					}

			m1 = Settings.cameraSettingsSave[0];
			m2 = Settings.cameraSettingsSave[1];
			if (m1 != KeyCode.None || m2 != KeyCode.None)
				if (m1 == KeyCode.None || Input.GetKey(m1))
					if (m2 == KeyCode.None || Input.GetKey(m2))
					{
						var cameraDriver = Current.cameraDriverInt;
						savedViews.views[numKey - 1] = new RememberedCameraPos(map)
						{
							rootPos = cameraDriver.rootPos,
							rootSize = cameraDriver.rootSize
						};
						Event.current.Use();
					}
		}
	}
}
