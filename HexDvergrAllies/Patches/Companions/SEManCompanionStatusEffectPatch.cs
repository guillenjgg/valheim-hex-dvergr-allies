using HarmonyLib;
using HexDvergrAllies.Core.Monoscripts;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(SEMan), "Internal_AddStatusEffect")]
    internal static class SEManCompanionStatusEffectPatch
    {
        private static bool Prefix(SEMan __instance, int nameHash)
        {
            var character = Traverse.Create(__instance).Field<Character>("m_character").Value;

            if (character && character.TryGetComponent<DvergrCompanionComponent>(out var companion))
            {
                return companion.CanApplyStatusEffect(nameHash);
            }

            return true;
        }
    }
}