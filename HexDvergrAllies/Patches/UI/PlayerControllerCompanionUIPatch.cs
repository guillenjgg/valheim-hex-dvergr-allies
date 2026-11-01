using HarmonyLib;
using HexDvergrAllies.Core.UI;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(PlayerController), "TakeInput")]
    internal static class PlayerControllerCompanionUIPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (DvergrCompanionUIController.IsVisible)
            {
                __result = false;
            }
        }
    }
}
