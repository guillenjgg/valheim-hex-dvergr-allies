using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class MonsterAICompanionFollowPatch
    {
        private static void Prefix(MonsterAI __instance)
        {
            Character character = __instance.GetComponent<Character>();

            if (!character)
            {
                return;
            }

            if (DvergrCompanionShipManager.IsPassenger(character))
            {
                return;
            }

            DvergrCompanionFollowManager.UpdateFollow(__instance);
            DvergrCompanionBehaviorManager.UpdateCombatLeash(__instance);
        }

        private static void Postfix(MonsterAI __instance)
        {
            DvergrCompanionSpawnerTargeting.Update(__instance);
            DvergrCompanionSentryMode.Update(__instance);
        }
    }

    [HarmonyPatch(typeof(BaseAI), "Follow")]
    internal static class BaseAICompanionFollowDistancePatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var replacements = 0;

            foreach (var instruction in instructions)
            {
                // Preserve vanilla following and pathfinding, replacing only its stopping distance.
                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 3f)
                {
                    replacements++;
                    instruction.opcode = OpCodes.Ldarg_0;
                    instruction.operand = null;
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BaseAICompanionFollowDistancePatch), nameof(GetFollowDistance)));
                }
                else
                {
                    yield return instruction;
                }
            }

            if (replacements != 1)
            {
                throw new InvalidOperationException("BaseAICompanionFollowDistancePatch: Expected one vanilla follow stopping distance.");
            }
        }

        private static float GetFollowDistance(BaseAI baseAI)
        {
            var character = baseAI.GetComponent<Character>();

            return character && DvergrCompanionState.IsHired(character)
                ? HexDvergrAlliesConfiguration.CompanionFollowDistance.Value
                : 3f;
        }
    }
}
