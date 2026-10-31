using HarmonyLib;
using HexDvergrAllies.Core.Monoscripts;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Character), nameof(Character.GetDamageModifiers))]
    internal static class CharacterCompanionDamageModifiersPatch
    {
        private static void Postfix(Character __instance, ref HitData.DamageModifiers __result)
        {
            if (__instance.TryGetComponent<DvergrCompanionComponent>(out var companion))
            {
                companion.ApplyDamageModifiers(ref __result);
            }
        }
    }
}