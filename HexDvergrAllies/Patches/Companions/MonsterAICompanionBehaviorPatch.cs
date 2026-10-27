using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class MonsterAICompanionBehaviorPatch
    {
        private static void Prefix(MonsterAI __instance)
        {
            DvergrCompanionBehaviorManager.UpdateBehavior(__instance);
        }
    }
}