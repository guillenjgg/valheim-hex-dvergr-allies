using HarmonyLib;
using HexDvergrAllies.Core.Trading;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(StoreGui), "CanAfford")]
    internal static class StoreGuiCanAffordPatch
    {
        private static void Postfix(Trader ___m_trader, Trader.TradeItem ___m_selectedItem, ref bool __result)
        {
            if (!___m_trader || !DvergrTraderManager.IsDvergrTrader(___m_trader))
            {
                return;
            }

            if (___m_selectedItem == null)
            {
                __result = false;
                return;
            }

            if (DvergrTraderManager.IsForgePact(___m_selectedItem))
            {
                __result = DvergrTraderManager.CanAffordForgePact(Player.m_localPlayer, ___m_trader.GetComponent<Character>());
                return;
            }

            if (DvergrTraderManager.IsAshlandsFlametal(___m_selectedItem))
            {
                __result = DvergrTraderManager.CanAffordAshlandsFlametal(Player.m_localPlayer);
                return;
            }

            if (___m_selectedItem.m_prefab.gameObject.name == "Softtissue")
            {
                return;
            }

            __result = CanAffordExtractor();
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
