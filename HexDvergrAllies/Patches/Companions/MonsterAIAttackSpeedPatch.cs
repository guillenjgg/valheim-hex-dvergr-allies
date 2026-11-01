using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class MonsterAIAttackSpeedPatch
    {
        private static readonly FieldInfo AttackIntervalField = AccessTools.Field(typeof(ItemDrop.ItemData.SharedData), "m_aiAttackInterval");

        private static readonly MethodInfo AdjustAttackIntervalMethod = AccessTools.Method(typeof(MonsterAIAttackSpeedPatch), nameof(AdjustAttackInterval));

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var replacements = 0;

            if (AttackIntervalField == null || AdjustAttackIntervalMethod == null)
            {
                throw new InvalidOperationException("Unable to resolve MonsterAI attack-speed patch members.");
            }

            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Ldfld || !Equals(codes[i].operand, AttackIntervalField))
                {
                    continue;
                }

                // Stack before: SharedData
                // Original instruction loads the weapon attack interval.
                // We additionally pass the MonsterAI instance to our helper.

                codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, AdjustAttackIntervalMethod));

                replacements++;
                i += 2;
            }

            if (replacements != 1)
            {
                throw new InvalidOperationException($"Expected one MonsterAI weapon attack interval read, found {replacements}.");
            }

            return codes;
        }

        private static float AdjustAttackInterval(float originalInterval, MonsterAI monsterAI)
        {
            var character = monsterAI.GetComponent<Character>();

            if (character == null || !DvergrCompanionManager.IsHired(character))
            {
                return originalInterval;
            }

            var multiplier = HexDvergrAlliesConfiguration.CompanionAttackSpeedMultiplier.Value;

            if (multiplier <= 0f)
            {
                return originalInterval;
            }

            return originalInterval / multiplier;
        }
    }
}
