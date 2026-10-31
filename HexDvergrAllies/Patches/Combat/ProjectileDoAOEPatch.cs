using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    /// <summary>
    /// Prevents player projectiles from damaging dvergrs
    /// </summary>
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.DoAOE))]
    internal static class ProjectileDoAOEPatch
    {
        private static readonly FieldInfo OwnerField = AccessTools.Field(typeof(Projectile), "m_owner");

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = new List<CodeInstruction>(instructions);
            var findHitObject = AccessTools.Method(typeof(Projectile), nameof(Projectile.FindHitObject));
            var isProtectedDvergr = AccessTools.Method(typeof(ProjectileDoAOEPatch), nameof(IsProtectedDvergr));

            var targetIndex = -1;
            var loopContinueIndex = -1;

            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].Calls(findHitObject) && codes[i + 1].IsStloc())
                {
                    targetIndex = i + 1;
                    break;
                }
            }

            if (targetIndex == -1)
            {
                throw new InvalidOperationException("ProjectileDoAOEPatch: Could not find AOE target.");
            }

            // Locate the loop increment near the end of the method.
            for (int i = codes.Count - 4; i > targetIndex; i--)
            {
                if (!codes[i].IsLdloc() || codes[i + 1].opcode != OpCodes.Ldc_I4_1 || codes[i + 2].opcode != OpCodes.Add || !codes[i + 3].IsStloc())
                {
                    continue;
                }

                loopContinueIndex = i;
                break;
            }

            if (loopContinueIndex == -1)
            {
                throw new InvalidOperationException("ProjectileDoAOEPatch: Could not find AOE loop continuation.");
            }

            var skipTarget = generator.DefineLabel();
            codes[loopContinueIndex].labels.Add(skipTarget);

            // Reuse the original local variable index.
            var storeInstruction = codes[targetIndex];
            CodeInstruction loadTarget;

            if (storeInstruction.opcode == OpCodes.Stloc_0)
            {
                loadTarget = new CodeInstruction(OpCodes.Ldloc_0);
            }
            else if (storeInstruction.opcode == OpCodes.Stloc_1)
            {
                loadTarget = new CodeInstruction(OpCodes.Ldloc_1);
            }
            else if (storeInstruction.opcode == OpCodes.Stloc_2)
            {
                loadTarget = new CodeInstruction(OpCodes.Ldloc_2);
            }
            else if (storeInstruction.opcode == OpCodes.Stloc_3)
            {
                loadTarget = new CodeInstruction(OpCodes.Ldloc_3);
            }
            else
            {
                loadTarget = new CodeInstruction(OpCodes.Ldloc, storeInstruction.operand);
            }

            codes.InsertRange(targetIndex + 1, new[] { new CodeInstruction(OpCodes.Ldarg_0), loadTarget, new CodeInstruction(OpCodes.Call, isProtectedDvergr), new CodeInstruction(OpCodes.Brtrue, skipTarget) });

            return codes;
        }

        private static bool IsProtectedDvergr(Projectile projectile, GameObject target)
        {
            if (!HexDvergrAlliesConfiguration.EnablePlayerDamageImmunity.Value)
            {
                return false;
            }

            var owner = OwnerField.GetValue(projectile) as Character;

            if (owner == null || !owner.IsPlayer())
            {
                return false;
            }

            var character = target.GetComponent<Character>();

            return character != null && character.GetFaction() == Character.Faction.Dverger;
        }
    }
}