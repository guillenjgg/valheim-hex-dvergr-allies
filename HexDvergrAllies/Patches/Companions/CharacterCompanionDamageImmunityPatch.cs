using HarmonyLib;
using HexDvergrAllies.Core.Monoscripts;
using HexDvergrAllies.Core.Features.Progression;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    internal static class CharacterCompanionDamageImmunityPatch
    {
        private static bool Prefix(Character __instance, HitData hit)
        {
            if (DvergrStarterCharacterState.IsProtected(__instance))
            {
                return false;
            }

            if (!__instance.TryGetComponent<DvergrCompanionComponent>(out var companion))
            {
                return true;
            }

            return companion.ProcessDamage(hit);
        }
    }
}