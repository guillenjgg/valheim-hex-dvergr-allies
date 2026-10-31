using HarmonyLib;

namespace HexDvergrAllies.Patches
{
    /// <summary>
    /// Prevents players from damaging dvergr structures
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), "RPC_Damage")]
    internal static class WearNTearRPCDamagePatch
    {
        private static bool Prefix(WearNTear __instance, HitData hit)
        {
            if (!HexDvergrAlliesConfiguration.EnablePlayerDamageImmunity.Value)
            {
                return true;
            }

            var attacker = hit.GetAttacker();

            if (attacker == null || !attacker.IsPlayer())
            {
                return true;
            }

            return !PrivateArea.InsideFactionArea(__instance.transform.position, Character.Faction.Dverger);
        }
    }
}
