using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(StoreGui), "BuySelectedItem")]
    internal static class StoreGuiBuySelectedItemPatch
    {
        private static bool Prefix(Trader ___m_trader, Trader.TradeItem ___m_selectedItem)
        {
            if (!___m_trader || !DvergrTraderManager.IsDvergrTrader(___m_trader))
            {
                return true;
            }

            if (___m_selectedItem.m_prefab.gameObject.name == "Softtissue")
            {
                return true;
            }

            var traderZdoId = ___m_trader.GetComponent<ZNetView>().GetZDO().m_uid;

            DvergrTradeRpcManager.RequestPurchase(traderZdoId);
            return false;
        }

    }
}
