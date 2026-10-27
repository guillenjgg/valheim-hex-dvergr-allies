
using System.Collections.Generic;
using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class MonsterAICompanionFollowPatch
    {
        private static readonly HashSet<MonsterAI> _loggedInstances =
            new HashSet<MonsterAI>();

        private static void Prefix(MonsterAI __instance)
        {
            if (!_loggedInstances.Contains(__instance))
            {
                var character = __instance.GetComponent<Character>();

                if (character &&
                    character.GetFaction() == Character.Faction.Dverger)
                {
                    _loggedInstances.Add(__instance);

                    Plugin.Log.LogInfo(
                        $"[Dvergr Companion] MonsterAI.UpdateAI patch active: Dvergr={character.name}, IsHired={DvergrCompanionManager.IsHired(character)}");
                }
            }

            DvergrCompanionManager.UpdateFollow(__instance);
        }
    }
}
