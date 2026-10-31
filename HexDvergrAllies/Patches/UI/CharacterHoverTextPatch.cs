using HarmonyLib;
using HexDvergrAllies.Core.UI;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Character), nameof(Character.GetHoverText))]
    internal static class CharacterHoverTextPatch
    {
        private static bool Prefix(Character __instance, ref string __result)
        {
            return DvergrHoverTextManager.Init(__instance, out __result);
        }
    }
}
