using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(BaseAI), "FindEnemy")]
    internal static class BaseAICompanionFindEnemyPatch
    {
        private static void Postfix(BaseAI __instance, ref Character __result)
        {
            if (!DvergrCompanionBehaviorManager.ShouldSuppressAutomaticTargeting(__instance))
            {
                return;
            }

            __result = null;
        }
    }
}
