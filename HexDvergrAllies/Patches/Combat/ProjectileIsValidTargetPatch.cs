using HarmonyLib;

namespace HexDvergrAllies.Patches
{
    /// <summary>
    /// Prevents players from damaging Dvergrs
    /// </summary>
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.IsValidTarget))]
    internal static class ProjectileIsValidTargetPatch
    {
        private static bool Prefix(IDestructible destr, Character ___m_owner, ref bool __result)
        {
            if (!HexDvergrAlliesConfiguration.EnablePlayerDamageImmunity.Value)
            {
                return true;
            }

            if (___m_owner == null || !___m_owner.IsPlayer())
            {
                return true;
            }

            var character = destr as Character;

            if (character == null || character.GetFaction() != Character.Faction.Dverger)
            {
                return true;
            }

            __result = false;
            return false;
        }
    }
}
