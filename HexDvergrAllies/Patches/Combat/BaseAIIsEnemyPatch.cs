using HarmonyLib;

namespace HexDvergrAllies.Patches
{
    /// <summary>
    /// Prevents dvergrs and players from being considered enemies of each other.
    /// </summary>
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IsEnemy), new[] { typeof(Character), typeof(Character) })]
    internal static class BaseAIIsEnemyPatch
    {
        private static bool Prefix(Character a, Character b, ref bool __result)
        {
            if (a == null || b == null)
            {
                return true;
            }

            bool dvergerVsPlayer = (a.GetFaction() == Character.Faction.Dverger && b.IsPlayer()) || (b.GetFaction() == Character.Faction.Dverger && a.IsPlayer());

            if (!dvergerVsPlayer)
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}