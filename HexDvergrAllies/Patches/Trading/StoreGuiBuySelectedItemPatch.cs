using HarmonyLib;
using HexDvergrAllies.Core.Managers;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(StoreGui), "BuySelectedItem")]
    internal static class StoreGuiBuySelectedItemPatch
    {
        private static bool Prefix(
            StoreGui __instance,
            Trader ___m_trader,
            Trader.TradeItem ___m_selectedItem)
        {
            if (!___m_trader ||
                !DvergrTraderManager.IsDvergrTrader(___m_trader))
            {
                return true;
            }

            if (___m_selectedItem == null)
            {
                return false;
            }

            if (DvergrTraderManager.IsForgePact(___m_selectedItem))
            {
                var player = Player.m_localPlayer;
                var character = ___m_trader.GetComponent<Character>();

                if (!DvergrTraderManager.CanAffordForgePact(player, character))
                {
                    Plugin.Log.LogWarning(
                        "[Dvergr Companion] Forge Pact rejected: Not eligible or insufficient MeadTasty.");

                    return false;
                }

                var distance = Vector3.Distance(
                    player.transform.position,
                    character.transform.position);

                if (distance > DvergrTraderManager.InteractionDistance)
                {
                    return false;
                }

                var meadPrefab = ObjectDB.instance.GetItemPrefab(
                    DvergrTraderManager.ForgePactPrefab);

                var itemName = meadPrefab.GetComponent<ItemDrop>()
                    .m_itemData.m_shared.m_name;

                player.GetInventory().RemoveItem(
                    itemName,
                    DvergrTraderManager.ForgePactCost);

                Plugin.Log.LogInfo(
                    $"[Dvergr Companion] Forge Pact purchased: Player={player.GetPlayerName()}, Dvergr={character.name}, MeadTasty={DvergrTraderManager.ForgePactCost}");

                DvergrCompanionManager.RequestHire(player, character);

                __instance.Hide();

                return false;
            }

            if (___m_selectedItem.m_prefab.gameObject.name == "Softtissue")
            {
                if (!__instance.m_coinPrefab)
                {
                    var coinPrefab = ObjectDB.instance.GetItemPrefab("Coins");

                    if (!coinPrefab)
                    {
                        Plugin.Log.LogError(
                            "[Dvergr Trader] Coins prefab not found.");

                        return false;
                    }

                    __instance.m_coinPrefab = coinPrefab.GetComponent<ItemDrop>();
                }

                return true;
            }

            var traderZdoId = ___m_trader
                .GetComponent<ZNetView>()
                .GetZDO()
                .m_uid;

            DvergrTradeRpcManager.RequestPurchase(traderZdoId);

            return false;
        }
    }
}