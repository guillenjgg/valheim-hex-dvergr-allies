using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
namespace HexDvergrAllies.Patches
{
    /// <summary>
    /// Prevents player area attacks from hitting dvergrs
    /// </summary>
    [HarmonyPatch]
    internal static class AttackAreaHitPatch
    {
        private static readonly FieldInfo CharacterField = AccessTools.Field(typeof(Attack), "m_character");

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Attack), "<DoAreaAttack>g__checkHits|27_0");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            var findHitObject = AccessTools.Method(typeof(Projectile), nameof(Projectile.FindHitObject));
            var isProtectedDvergr = AccessTools.Method(typeof(AttackAreaHitPatch), nameof(IsProtectedDvergr));
            var loopContinue = generator.DefineLabel();

            bool foundLoopContinue = false;
            bool foundHitObject = false;

            for (int i = 0; i < codes.Count - 3; i++)
            {
                if (codes[i].opcode == OpCodes.Ldloc_0 && codes[i + 1].opcode == OpCodes.Ldc_I4_1 && codes[i + 2].opcode == OpCodes.Add && codes[i + 3].opcode == OpCodes.Stloc_0)
                {
                    codes[i].labels.Add(loopContinue);
                    foundLoopContinue = true;
                    break;
                }
            }

            if (!foundLoopContinue)
            {
                throw new InvalidOperationException("AttackAreaHitPatch: Could not find area attack loop continuation.");
            }

            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (!codes[i].Calls(findHitObject) || codes[i + 1].opcode != OpCodes.Stloc_2)
                {
                    continue;
                }

                codes.InsertRange(i + 2, new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldloc_2), new CodeInstruction(OpCodes.Call, isProtectedDvergr), new CodeInstruction(OpCodes.Brtrue, loopContinue) });

                foundHitObject = true;
                break;
            }

            if (!foundHitObject)
            {
                throw new InvalidOperationException("AttackAreaHitPatch: Could not find area attack target registration.");
            }
            
            return codes;
        }

        private static bool IsProtectedDvergr(Attack attack, GameObject target)
        {
            if (!HexDvergrAlliesConfiguration.EnablePlayerDamageImmunity.Value)
            {
                return false;
            }

            var attacker = CharacterField.GetValue(attack) as Humanoid;

            if (!attacker.IsPlayer())
            {
                return false;
            }

            var character = target.GetComponent<Character>();

            return character != null && character.GetFaction() == Character.Faction.Dverger;
        }
    }
}