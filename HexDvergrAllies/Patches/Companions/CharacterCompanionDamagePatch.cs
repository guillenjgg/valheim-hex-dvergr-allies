using HarmonyLib;
using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    internal static class CharacterCompanionDamagePatch
    {
        private static void Prefix(Character __instance, HitData hit, out DvergrCompanionDamageManager.DamageState __state)
        {
            DvergrCompanionDamageManager.BeforeDamage(__instance, hit, out __state);
        }

        private static void Postfix(Character __instance, DvergrCompanionDamageManager.DamageState __state)
        {
            DvergrCompanionDamageManager.AfterDamage(__instance, __state);
        }
    }
}
