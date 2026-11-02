using HarmonyLib;
using HexDvergrAllies.Core;
using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.Trading;

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

            if (DvergrTraderManager.IsForgePact(___m_selectedItem))
            {
                DvergrForgePactManager.RequestForgePact(Player.m_localPlayer, ___m_trader.GetComponent<Character>());
                return false;
            }

            if (___m_selectedItem.m_prefab.gameObject.name == PrefabHelper.Softtissue)
            {
                if (!__instance.m_coinPrefab)
                {
                    var coinPrefab = ObjectDB.instance.GetItemPrefab(PrefabHelper.Coins);

                    if (!coinPrefab)
                    {
                        Plugin.Log.LogError("[Dvergr Trader] Coins prefab not found.");
                        return false;
                    }

                    __instance.m_coinPrefab = coinPrefab.GetComponent<ItemDrop>();
                }

                return true;
            }

            var traderNview = ___m_trader.GetComponent<ZNetView>();

            if (!traderNview || !traderNview.IsValid())
            {
                return false;
            }

            var purchaseType = DvergrTraderManager.IsAshlandsFlametal(___m_selectedItem) ? DvergrPurchaseType.Flametal : DvergrPurchaseType.Extractor;

            DvergrTradeRpcManager.RequestPurchase(traderNview.GetZDO().m_uid, purchaseType);

            return false;
        }
    }
}