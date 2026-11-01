using HarmonyLib;
using HexDvergrAllies.Core.UI;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    internal static class PlayerCompanionShortcutPatch
    {
        private static void Postfix(Player __instance)
        {
            DvergrCompanionShortcutManager.Update(__instance);
        }
    }
}