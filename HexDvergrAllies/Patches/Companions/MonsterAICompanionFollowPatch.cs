using System.Collections.Generic;
using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class MonsterAICompanionFollowPatch
    {
        private static readonly HashSet<MonsterAI> _loggedInstances = new HashSet<MonsterAI>();

        private static void Prefix(MonsterAI __instance)
        {
            Character character = __instance.GetComponent<Character>();

            if (!character)
            {
                return;
            }

            if (character.GetFaction() == Character.Faction.Dverger && _loggedInstances.Add(__instance))
            {
                Plugin.Log.LogInfo($"[Dvergr Companion] MonsterAI.UpdateAI patch active: " + $"Dvergr={character.name}, " + $"IsHired={DvergrCompanionManager.IsHired(character)}");
            }

            if (DvergrCompanionShipManager.IsPassenger(character))
            {
                return;
            }

            DvergrCompanionManager.UpdateFollow(__instance);
        }
    }
}