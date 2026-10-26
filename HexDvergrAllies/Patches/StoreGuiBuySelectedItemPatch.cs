using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(StoreGui), "BuySelectedItem")]
    internal static class StoreGuiBuySelectedItemPatch
    {
        private static bool Prefix(Trader ___m_trader)
        {
            if (!___m_trader || !DvergrTraderManager.IsDvergrTrader(___m_trader))
            {
                return true;
            }

            var traderZdoId = ___m_trader.GetComponent<ZNetView>().GetZDO().m_uid;

            DvergrTradeRpcManager.RequestPurchase(traderZdoId);
            return false;
        }
    }
}
