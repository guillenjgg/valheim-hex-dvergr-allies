using HarmonyLib;
using HexDvergrAllies.Core.UI;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Player), "TakeInput")]
    internal static class PlayerCompanionInputPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (DvergrCompanionUIController.IsVisible)
            {
                __result = false;
            }
        }
    }
}