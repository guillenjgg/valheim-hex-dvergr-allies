using HarmonyLib;
using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Character), "OnDeath")]
    internal static class CharacterCompanionDeathPatch
    {
        private static void Prefix(Character __instance)
        {
            Core.Features.Progression.DvergrStarterEncounterManager.OnCharacterDeath(__instance);
            DvergrCompanionManager.OnCompanionDeath(__instance);
        }
    }
}
