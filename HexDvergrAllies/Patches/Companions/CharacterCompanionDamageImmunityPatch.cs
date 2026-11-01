using HarmonyLib;
using HexDvergrAllies.Core.Monoscripts;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    internal static class CharacterCompanionDamageImmunityPatch
    {
        private static bool Prefix(Character __instance, HitData hit)
        {
            if (!__instance.TryGetComponent<DvergrCompanionComponent>(out var companion))
            {
                return true;
            }

            return companion.ProcessDamage(hit);
        }
    }
}