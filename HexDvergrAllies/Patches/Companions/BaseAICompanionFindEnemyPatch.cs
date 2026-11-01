using HarmonyLib;
using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(BaseAI), "FindEnemy")]
    internal static class BaseAICompanionFindEnemyPatch
    {
        private static void Postfix(BaseAI __instance, ref Character __result)
        {
            var monsterAI = __instance as MonsterAI;

            if (!monsterAI)
            {
                return;
            }

            var character = monsterAI.GetComponent<Character>();

            if (DvergrCompanionSentryMode.IsActive(character))
            {
                if (__result && !DvergrCompanionSentryMode.IsWithinCombatRadius(character, __result))
                {
                    __result = null;
                }

                return;
            }

            if (DvergrCompanionModeManager.ShouldSuppressAutomaticTargeting(__instance))
            {
                __result = null;
            }
        }
    }
}
