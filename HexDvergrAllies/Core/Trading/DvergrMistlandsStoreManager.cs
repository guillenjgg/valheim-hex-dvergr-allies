using HexDvergrAllies.Core.HexLocalization;
using System.Collections.Generic;
using UnityEngine;

namespace HexDvergrAllies.Core.Trading
{
    internal static class DvergrMistlandsStoreManager
    {
        internal static bool IsFallenBrother(Trader.TradeItem item)
        {
            return item != null && item.m_prefab && item.m_prefab.gameObject.name == PrefabHelper.TrophyDvergr;
        }

        internal static bool IsCoinPurchase(Trader.TradeItem item)
        {
            return item != null && item.m_prefab && (item.m_prefab.gameObject.name == PrefabHelper.Softtissue || item.m_prefab.gameObject.name == PrefabHelper.JuteBlue);
        }

        internal static void AddOffers(Trader trader)
        {
            AddOffer(trader, PrefabHelper.JuteBlue, DvergrPricingConfig.MistlandsJuteQuantity, DvergrPricingConfig.MistlandsJuteCoinCost, "");
            AddOffer(trader, PrefabHelper.TrophyDvergr, 1, 0, HexLocalizationManager.Localize("$hex_dvergr_fallen_brother_tooltip", DvergrPricingConfig.FallenBrotherTrophyCost));
        }

        private static void AddOffer(Trader trader, string prefabName, int quantity, int coins, string tooltip)
        {
            var prefab = ObjectDB.instance.GetItemPrefab(prefabName);
            var item = prefab ? prefab.GetComponent<ItemDrop>() : null;

            if (!item)
            {
                Plugin.Log.LogWarning($"[Dvergr Trader] Required Mistlands stock unavailable: {prefabName}.");
                return;
            }

            trader.m_items.Add(new Trader.TradeItem { m_prefab = item, m_stack = quantity, m_price = coins, m_tooltip = tooltip, m_buyPlayerEffects = new EffectList() });
        }

        private static List<string> AllowedPaymentNames()
        {
            var names = new List<string>();

            foreach (var prefabName in PrefabHelper.MistlandsTrophyPrefabs)
            {
                var prefab = ObjectDB.instance.GetItemPrefab(prefabName);
                var item = prefab ? prefab.GetComponent<ItemDrop>() : null;

                if (item && !names.Contains(item.m_itemData.m_shared.m_name))
                {
                    names.Add(item.m_itemData.m_shared.m_name);
                }
            }

            return names;
        }

        internal static bool CanAffordFallenBrother(Player player)
        {
            if (!player || !ObjectDB.instance)
            {
                return false;
            }

            var total = 0;

            foreach (var name in AllowedPaymentNames())
            {
                total += player.GetInventory().CountItems(name);
            }

            return total >= DvergrPricingConfig.FallenBrotherTrophyCost;
        }

        internal static bool PurchaseFallenBrother(Player player)
        {
            var prefab = ObjectDB.instance.GetItemPrefab(PrefabHelper.TrophyDvergr);
            var item = prefab ? prefab.GetComponent<ItemDrop>() : null;

            if (!item || !CanAffordFallenBrother(player))
            {
                return false;
            }

            var inventory = player.GetInventory();

            if (!inventory.CanAddItem(prefab, 1) || !inventory.AddItem(prefab, 1))
            {
                return false;
            }

            var remaining = DvergrPricingConfig.FallenBrotherTrophyCost;

            // Mixed payments are allowed, but only from the existing trophy list.
            // Inventory validation, grant and removal run synchronously together.
            foreach (var name in AllowedPaymentNames())
            {
                var count = Mathf.Min(remaining, inventory.CountItems(name));

                if (count > 0)
                {
                    inventory.RemoveItem(name, count);
                    remaining -= count;
                }

                if (remaining == 0)
                {
                    break;
                }
            }

            player.ShowPickupMessage(item.m_itemData, 1);
            Plugin.Log.LogInfo($"[Dvergr Trade] Fallen brother trophy purchased for {DvergrPricingConfig.FallenBrotherTrophyCost} allowed enemy trophies.");
            return true;
        }
    }
}
