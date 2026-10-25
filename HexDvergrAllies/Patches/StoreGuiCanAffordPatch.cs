
using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(StoreGui), "CanAfford")]
    internal static class StoreGuiCanAffordPatch
    {
        private static void Postfix(Trader ___m_trader, ref bool __result)
        {
            if (!___m_trader || !DvergrTraderManager.IsDvergrTrader(___m_trader))
            {
                return;
            }

            __result = false;

            if (!DvergrTradeRpcManager.HasCurrentPrice)
            {
                return;
            }

            var inventory = Player.m_localPlayer.GetInventory();
            var trophy = DvergrTraderManager.GetPaymentTrophy(inventory);
            var sapPrefab = ObjectDB.instance.GetItemPrefab("Sap");
            var sapName = sapPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;

            __result = trophy != null &&
                inventory.CountItems(sapName) >= DvergrTradeRpcManager.CurrentSapCost;
        }
    }
}
