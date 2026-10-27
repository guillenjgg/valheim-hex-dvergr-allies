
using HarmonyLib;
using HexDvergrAllies.Core.Managers;

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

            if (___m_selectedItem.m_prefab.gameObject.name == "Softtissue")
            {
                return;
            }

            __result = CanAffordExtractor();
        }

        internal static bool CanAffordExtractor()
        {
            if (!DvergrTradeRpcManager.HasCurrentPrice)
            {
                return false;
            }

            var inventory = Player.m_localPlayer.GetInventory();
            var trophy = DvergrTraderManager.GetPaymentTrophy(inventory);
            var sapPrefab = ObjectDB.instance.GetItemPrefab("Sap");
            var sapName = sapPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;

            return trophy != null &&
                inventory.CountItems(sapName) >= DvergrTradeRpcManager.CurrentSapCost;
        }
    }
}
