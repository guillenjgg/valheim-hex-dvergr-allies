using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    internal static class CharacterCompanionDamagePatch
    {
        private static void Prefix(Character __instance, HitData hit)
        {
            if (!DvergrCompanionManager.IsHired(__instance))
            {
                return;
            }

            var netView = __instance.GetComponent<ZNetView>();

            if (netView == null || !netView.IsValid() || !netView.IsOwner())
            {
                return;
            }

            var multiplier = HexDvergrAlliesConfiguration.CompanionDamageTakenMultiplier.Value;

            hit.ApplyModifier(multiplier);
        }
    }
}