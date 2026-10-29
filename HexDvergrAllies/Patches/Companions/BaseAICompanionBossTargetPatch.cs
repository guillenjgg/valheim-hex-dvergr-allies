using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IsEnemy), new[] { typeof(Character), typeof(Character) })]
    internal static class BaseAICompanionBossTargetPatch
    {
        private static void Postfix(Character a, Character b, ref bool __result)
        {
            if (__result)
            {
                return;
            }

            if (a == null || b == null || a == b)
            {
                return;
            }

            if (a.GetFaction() != Character.Faction.Dverger)
            {
                return;
            }

            if (b.GetFaction() != Character.Faction.Boss)
            {
                return;
            }

            if (!DvergrCompanionManager.IsHired(a))
            {
                return;
            }

            if (b.IsDead() || b.m_aiSkipTarget)
            {
                return;
            }

            __result = true;
        }
    }
}
