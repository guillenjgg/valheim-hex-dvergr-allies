using HarmonyLib;
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

            DvergrCompanionManager.UpdateFollow(__instance);
            DvergrCompanionBehaviorManager.UpdateCombatLeash(__instance);
        }
    }
}