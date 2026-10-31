using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Character), "OnDestroy")]
    internal static class CharacterCompanionCleanupPatch
    {
        private static void Prefix(Character __instance)
        {
            DvergrCompanionBehaviorManager.RemoveCharacter(__instance);
            DvergrCompanionShipManager.RemoveCharacter(__instance);
        }
    }
}