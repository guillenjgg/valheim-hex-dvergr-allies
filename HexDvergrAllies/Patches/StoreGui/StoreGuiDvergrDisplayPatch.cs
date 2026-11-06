using HarmonyLib;
using HexDvergrAllies.Core.UI;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch]
    internal static class StoreGuiDvergrDisplayPatch
    {
        [HarmonyPatch(typeof(StoreGui), "FillList")]
        [HarmonyPostfix]
        private static void FillListPostfix(StoreGui __instance)
        {
            DvergrTraderDisplayManager.OnFillList(__instance);
        }

        [HarmonyPatch(typeof(StoreGui), "Update")]
        [HarmonyPostfix]
        private static void UpdatePostfix(StoreGui __instance)
        {
            DvergrTraderDisplayManager.OnUpdate(__instance);
        }
    }
}
