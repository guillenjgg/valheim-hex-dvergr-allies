using HarmonyLib;
using HexDvergrAllies.Core.Companions;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class MonsterAICompanionBehaviorPatch
    {
        private static void Prefix(MonsterAI __instance)
        {
            DvergrCompanionBehaviorManager.UpdateBehavior(__instance);
        }
    }

    [HarmonyPatch(typeof(MonsterAI), "UpdateTarget")]
    internal static class MonsterAICompanionTargetScanPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var timerField = AccessTools.Field(typeof(MonsterAI), "m_updateTargetTimer");
            var intervalMethod = AccessTools.Method(typeof(DvergrCompanionBehaviorManager), nameof(DvergrCompanionBehaviorManager.GetTargetScanInterval));
            var replacements = 0;

            for (var i = 1; i < codes.Count; i++)
            {
                // Override the normal near/far scan reset, leaving countdown and chase delays intact.
                if (codes[i].opcode != OpCodes.Stfld || !Equals(codes[i].operand, timerField) ||
                    codes[i - 1].opcode != OpCodes.Ldc_R4 || (float)codes[i - 1].operand != 2f)
                {
                    continue;
                }

                var loadAI = new CodeInstruction(OpCodes.Ldarg_0);
                loadAI.labels.AddRange(codes[i].labels);
                codes[i].labels.Clear();
                codes.InsertRange(i, new[] { loadAI, new CodeInstruction(OpCodes.Call, intervalMethod) });
                i += 2;
                replacements++;
            }

            if (replacements != 1)
            {
                throw new InvalidOperationException("MonsterAICompanionTargetScanPatch: Expected one normal target scan timer reset.");
            }

            return codes;
        }
    }
}
