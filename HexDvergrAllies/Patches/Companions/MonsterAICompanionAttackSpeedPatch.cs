using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class MonsterAICompanionAttackSpeedPatch
    {
        private static readonly FieldInfo AttackIntervalField =
            AccessTools.Field(typeof(ItemDrop.ItemData.SharedData), "m_aiAttackInterval");

        private static readonly MethodInfo GetAttackIntervalMethod =
            AccessTools.Method(typeof(MonsterAICompanionAttackSpeedPatch), nameof(GetAttackInterval));

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var replacements = 0;

            for (var i = 0; i < codes.Count; i++)
            {
                if (!codes[i].LoadsField(AttackIntervalField))
                {
                    continue;
                }

                codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, GetAttackIntervalMethod));

                replacements++;
                i += 2;
            }

            if (replacements != 1)
            {
                throw new System.InvalidOperationException(
                    $"Expected one MonsterAI weapon attack interval access, found {replacements}.");
            }

            return codes;
        }

        private static float GetAttackInterval(float originalInterval, MonsterAI monsterAI)
        {
            var character = monsterAI.GetComponent<Character>();

            if (character == null || !DvergrCompanionManager.IsHired(character))
            {
                return originalInterval;
            }

            var multiplier = HexDvergrAlliesConfiguration.CompanionAttackSpeedMultiplier.Value;

            return originalInterval / multiplier;
        }
    }
}