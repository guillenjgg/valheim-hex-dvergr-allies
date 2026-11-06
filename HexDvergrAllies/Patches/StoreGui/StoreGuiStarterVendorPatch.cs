using HarmonyLib;
using HexDvergrAllies.Core.Features.Progression;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch]
    internal static class StoreGuiStarterVendorPatch
    {
        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Show))]
        [HarmonyPrefix]
        private static void ShowPrefix(Trader __0)
        {
            DvergrStarterVendorService.OnShow(__0);
        }

        [HarmonyPatch(typeof(StoreGui), "SellItem")]
        [HarmonyPrefix]
        private static bool SellPrefix(Trader ___m_trader)
        {
            return DvergrStarterVendorService.CanSell(___m_trader);
        }

        [HarmonyPatch(typeof(StoreGui), "UpdateSellButton")]
        [HarmonyPostfix]
        private static void SellButtonPostfix(StoreGui __instance, Trader ___m_trader)
        {
            DvergrStarterVendorService.UpdateSellButton(__instance, ___m_trader);
        }
    }
}
