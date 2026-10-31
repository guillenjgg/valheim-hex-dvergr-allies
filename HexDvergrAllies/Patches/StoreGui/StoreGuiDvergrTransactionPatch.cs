using HarmonyLib;
using HexDvergrAllies.Core.Trading;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(StoreGui), "Update")]
    internal static class StoreGuiDvergrTransactionPatch
    {
        private static void Postfix(StoreGui __instance, Trader ___m_trader)
        {
            if (!__instance.m_rootPanel.activeSelf)
            {
                return;
            }

            if (!___m_trader || !DvergrTraderManager.IsDvergrTrader(___m_trader))
            {
                return;
            }

            if (DvergrTradeTransaction.IsPending)
            {
                __instance.m_buyButton.interactable = false;
            }
        }
    }
}