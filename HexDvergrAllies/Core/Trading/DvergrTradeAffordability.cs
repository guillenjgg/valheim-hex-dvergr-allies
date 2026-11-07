using UnityEngine;

namespace HexDvergrAllies.Core.Trading
{
    internal static class DvergrTradeAffordability
    {
        internal static void Apply(Trader trader, Trader.TradeItem selectedItem, ref bool result)
        {
            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
            {
                return;
            }

            if (selectedItem == null)
            {
                result = false;
                return;
            }

            if (DvergrTraderManager.IsForgePact(selectedItem))
            {
                result = DvergrTraderManager.CanAffordForgePact(Player.m_localPlayer, trader.GetComponent<Character>());
                return;
            }

            if (DvergrMistlandsStoreManager.IsFallenBrother(selectedItem))
            {
                result = DvergrMistlandsStoreManager.CanAffordFallenBrother(Player.m_localPlayer);
                return;
            }

            if (DvergrTraderManager.IsAshlandsFlametal(selectedItem))
            {
                result = DvergrTraderManager.CanAffordAshlandsFlametal(Player.m_localPlayer);
                return;
            }

            if (DvergrMistlandsStoreManager.IsCoinPurchase(selectedItem))
            {
                return;
            }

            result = CanAffordExtractor();
        }

        internal static bool CanAffordExtractor()
        {
            if (!DvergrTradeRpcManager.HasCurrentPrice || !Player.m_localPlayer)
            {
                return false;
            }

            Inventory inventory = Player.m_localPlayer.GetInventory();
            string trophy = DvergrTraderManager.GetPaymentTrophy(inventory);
            GameObject sapPrefab = ObjectDB.instance.GetItemPrefab("Sap");

            if (!sapPrefab)
            {
                return false;
            }

            ItemDrop sapItem = sapPrefab.GetComponent<ItemDrop>();

            if (!sapItem)
            {
                return false;
            }

            string sapName = sapItem.m_itemData.m_shared.m_name;

            return trophy != null && inventory.CountItems(sapName) >= DvergrTradeRpcManager.CurrentSapCost;
        }
    }
}
