using HarmonyLib;
using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.Features.Progression;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    internal static class CharacterCompanionDamagePatch
    {
        private static bool Prefix(Character __instance, HitData hit, out DvergrCompanionDamageManager.DamageState __state)
        {
            __state = default(DvergrCompanionDamageManager.DamageState);

            if (DvergrStarterCharacterState.IsProtected(__instance))
            {
                return false;
            }

            DvergrCompanionDamageManager.BeforeDamage(__instance, hit, out __state);

            return true;
        }

        private static void Postfix(Character __instance, DvergrCompanionDamageManager.DamageState __state)
        {
            DvergrCompanionDamageManager.AfterDamage(__instance, __state);
        }
    }
}
