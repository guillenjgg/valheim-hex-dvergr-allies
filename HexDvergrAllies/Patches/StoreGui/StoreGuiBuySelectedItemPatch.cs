using HarmonyLib;
using HexDvergrAllies.Core.Trading;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(StoreGui), "BuySelectedItem")]
    internal static class StoreGuiBuySelectedItemPatch
    {
        private static bool Prefix(StoreGui __instance, Trader ___m_trader, Trader.TradeItem ___m_selectedItem)
        {
            return DvergrTradePurchaseController.OnBuySelectedItem(__instance, ___m_trader, ___m_selectedItem);
        }
    }
}
