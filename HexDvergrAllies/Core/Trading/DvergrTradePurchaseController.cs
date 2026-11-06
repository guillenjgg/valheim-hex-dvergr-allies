using HexDvergrAllies.Core;
using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Core.Trading
{
    internal static class DvergrTradePurchaseController
    {
        internal static bool OnBuySelectedItem(StoreGui store, Trader trader, Trader.TradeItem selectedItem)
        {
            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
            {
                return true;
            }

            if (!Core.Features.Progression.DvergrStarterVendorService.CanBuy(trader, selectedItem))
            {
                return false;
            }

            if (selectedItem == null)
            {
                return false;
            }

            if (DvergrTraderManager.IsForgePact(selectedItem))
            {
                DvergrForgePactManager.RequestForgePact(Player.m_localPlayer, trader.GetComponent<Character>());
                return false;
            }

            if (DvergrMistlandsStoreManager.IsCoinPurchase(selectedItem))
            {
                if (!store.m_coinPrefab)
                {
                    var coinPrefab = ObjectDB.instance.GetItemPrefab(PrefabHelper.Coins);

                    if (!coinPrefab)
                    {
                        Plugin.Log.LogError("[Dvergr Trader] Coins prefab not found.");
                        return false;
                    }

                    store.m_coinPrefab = coinPrefab.GetComponent<ItemDrop>();
                }

                return true;
            }

            var traderNview = trader.GetComponent<ZNetView>();

            if (!traderNview || !traderNview.IsValid())
            {
                return false;
            }

            var purchaseType = DvergrMistlandsStoreManager.IsFallenBrother(selectedItem) ? DvergrPurchaseType.FallenBrother : DvergrTraderManager.IsAshlandsFlametal(selectedItem) ? DvergrPurchaseType.Flametal : DvergrPurchaseType.Extractor;

            DvergrTradeRpcManager.RequestPurchase(traderNview.GetZDO().m_uid, purchaseType);

            return false;
        }
    }
}
