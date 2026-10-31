using HarmonyLib;
using HexDvergrAllies.Core;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    /// <summary>
    /// This patch allows players to put down certain pieces (workbench, stonecutter, and portals) in dvergr warded zones
    /// </summary>
    [HarmonyPatch(typeof(Player), "RemovePiece")]
    internal static class PlayerRemovePiecePatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var original = AccessTools.Method(typeof(PrivateArea), nameof(PrivateArea.CheckAccess));
            var replacement = AccessTools.Method(typeof(PlayerRemovePiecePatch), nameof(CheckRemovalAccess));

            foreach (var instruction in instructions)
            {
                if (instruction.Calls(original))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                }

                yield return instruction;
            }
        }

        private static bool CheckRemovalAccess(Vector3 point, float radius, bool flash, bool wardCheck, Piece piece)
        {
            if (PrivateArea.CheckAccess(point, radius, flash, wardCheck))
            {
                return true;
            }

            if (!HexDvergrAlliesConfiguration.AllowTemporaryHomestead.Value)
            {
                return false;
            }

            var prefabName = Utils.GetPrefabName(piece.gameObject);

            if (!PrefabHelper.AllowedHomesteadPieces.Contains(prefabName))
            {
                return false;
            }

            return PrivateArea.InsideFactionArea(point, Character.Faction.Dverger);
        }
    }
}
