using HarmonyLib;
using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    internal static class CharacterCompanionDamagePatch
    {
        private static void Prefix(Character __instance, HitData hit)
        {
            var netView = __instance.GetComponent<ZNetView>();

            if (netView == null || !netView.IsValid() || !netView.IsOwner())
            {
                return;
            }

            if (DvergrCompanionManager.IsHired(__instance))
            {
                var damageTakenMultiplier = HexDvergrAlliesConfiguration.CompanionDamageTakenMultiplier.Value;
                hit.ApplyModifier(damageTakenMultiplier);
            }

            var attacker = hit.GetAttacker();

            if (attacker == null || !DvergrCompanionManager.IsHired(attacker))
            {
                return;
            }

            var damageDealtMultiplier = HexDvergrAlliesConfiguration.CompanionDamageDealtMultiplier.Value;
            hit.ApplyModifier(damageDealtMultiplier);
        }
    }
}
