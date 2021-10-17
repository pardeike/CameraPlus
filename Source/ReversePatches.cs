using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace CameraPlus
{
	[HarmonyPatch]
	static class ReversePatches
	{
		// mostly copied from the logic in the beginning od PawnUIOverlay.DrawPawnGUIOverlay
		//
		[HarmonyReversePatch(HarmonyReversePatchType.Original)]
		[HarmonyPatch(typeof(PawnUIOverlay), nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
		public static bool PerformsDrawPawnGUIOverlay(PawnUIOverlay me)
		{
			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				var mLabelDrawPosFor = SymbolExtensions.GetMethodInfo(() => GenMapUI.LabelDrawPosFor(null, 0f));

				var list = instructions.ToList();
				var idx = list.FirstIndexOf(code => code.Calls(mLabelDrawPosFor));
				if (idx < 3 || idx >= list.Count) throw new AmbiguousMatchException("Cannot find GenMapUI.LabelDrawPosFor in PawnUIOverlay.DrawPawnGUIOverlay");
				idx -= 3;
				var endLabels = list[idx].labels;
				list.RemoveRange(idx, list.Count - idx);

				foreach (var code in list)
				{
					if (code.opcode == OpCodes.Ret)
					{
						var labels = code.labels;
						yield return new CodeInstruction(OpCodes.Ldc_I4_0) { labels = labels };
						yield return new CodeInstruction(OpCodes.Ret);
					}
					else
						yield return code;
				}
				yield return new CodeInstruction(OpCodes.Ldc_I4_1) { labels = endLabels };
				yield return new CodeInstruction(OpCodes.Ret);
			}

			_ = Transpiler(default);
			try { _ = me; return default; } finally { }
		}
	}
}
