using HarmonyLib;
using HexDvergrAllies.Core.UI;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(TextsDialog), "UpdateTextsList")]
    internal static class TextsDialogDvergrCompendiumPatch
    {
        private static void Postfix(TextsDialog __instance)
        {
            DvergrCompendiumManager.AddEntry(__instance);
        }
    }
}
