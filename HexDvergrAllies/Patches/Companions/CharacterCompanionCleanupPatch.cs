using HarmonyLib;
using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Character), "OnDestroy")]
    internal static class CharacterCompanionCleanupPatch
    {
        private static void Prefix(Character __instance)
        {
            DvergrCompanionBehaviorManager.RemoveCharacter(__instance);
            DvergrCompanionShipManager.RemoveCharacter(__instance);
            DvergrCompanionDungeonTargeting.RemoveCharacter(__instance);
            DvergrCompanionDungeonFollowManager.RemoveCharacter(__instance);
            DvergrCompanionFollowManager.RemoveCharacter(__instance);
            var monsterAI = __instance.GetComponent<MonsterAI>();

            if (!object.ReferenceEquals(monsterAI, null))
            {
                DvergrCompanionSpawnerTargeting.RemoveCharacter(monsterAI);
            }
        }
    }
}
