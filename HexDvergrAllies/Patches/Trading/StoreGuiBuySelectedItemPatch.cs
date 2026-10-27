using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(StoreGui), "BuySelectedItem")]
    internal static class StoreGuiBuySelectedItemPatch
    {
        private static bool Prefix(StoreGui __instance, Trader ___m_trader, Trader.TradeItem ___m_selectedItem)
        {
            if (!___m_trader || !DvergrTraderManager.IsDvergrTrader(___m_trader))
            {
                return true;
            }

            if (___m_selectedItem == null)
            {
                return false;
            }

            if (___m_selectedItem.m_prefab.gameObject.name == "Softtissue")
            {
                if (!__instance.m_coinPrefab)
                {
                    var coinPrefab = ObjectDB.instance.GetItemPrefab("Coins");

                    if (!coinPrefab)
                    {
                        Plugin.Log.LogError("[Dvergr Trader] Coins prefab not found.");
                        return false;
                    }

                    __instance.m_coinPrefab = coinPrefab.GetComponent<ItemDrop>();
                }

                return true;
            }

            var traderZdoId = ___m_trader.GetComponent<ZNetView>().GetZDO().m_uid;

            DvergrTradeRpcManager.RequestPurchase(traderZdoId);
            return false;
        }
    }
}