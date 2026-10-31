using HarmonyLib;
using HexDvergrAllies.Core;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    internal static class PlayerUpdatePlacementGhostPatch
    {
        private static readonly AccessTools.FieldRef<Player, PieceTable> BuildPieces = AccessTools.FieldRefAccess<Player, PieceTable>("m_buildPieces");

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var original = AccessTools.Method(typeof(PrivateArea), nameof(PrivateArea.CheckAccess));
            var replacement = AccessTools.Method(typeof(PlayerUpdatePlacementGhostPatch), nameof(CheckPlacementAccess));

            foreach (var instruction in instructions)
            {
                if (instruction.Calls(original))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                }

                yield return instruction;
            }
        }

        private static bool CheckPlacementAccess(Vector3 point, float radius, bool flash, bool wardCheck)
        {
            if (PrivateArea.CheckAccess(point, radius, flash, wardCheck))
            {
                return true;
            }

            if (!HexDvergrAlliesConfiguration.AllowTemporaryHomestead.Value)
            {
                return false;
            }

            var selectedPiece = BuildPieces(Player.m_localPlayer).GetSelectedPrefab();

            if (!PrefabHelper.AllowedHomesteadPieces.Contains(selectedPiece.name))
            {
                return false;
            }

            return PrivateArea.InsideFactionArea(point, Character.Faction.Dverger);
        }
    }
}
