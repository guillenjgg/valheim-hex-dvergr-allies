using HarmonyLib;
using HexDvergrAllies.Core.Monoscripts;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.AddStatusEffect), new[] { typeof(StatusEffect), typeof(bool), typeof(int), typeof(float), typeof(short) })]
    internal static class SEManCompanionDirectStatusEffectPatch
    {
        private static bool Prefix(SEMan __instance, StatusEffect statusEffect, ref StatusEffect __result)
        {
            var character = Traverse.Create(__instance).Field<Character>("m_character").Value;

            if (character && character.TryGetComponent<DvergrCompanionComponent>(out var companion) && !companion.CanApplyStatusEffect(statusEffect))
            {
                __result = null;
                return false;
            }

            return true;
        }
    }
}