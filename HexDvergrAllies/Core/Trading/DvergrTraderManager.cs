using HexDvergrAllies.Core;
using HexDvergrAllies.Core.HexLocalization;
using HexDvergrAllies.Core.Companions;
using UnityEngine;

namespace HexDvergrAllies.Core.Trading
{
    internal static class DvergrTraderManager
    {
        internal const float InteractionDistance = 2f;

        internal static void Initialize(Humanoid dvergr)
        {
            if (dvergr.GetComponent<Trader>())
            {
                return;
            }

            Heightmap.Biome biome = Heightmap.FindBiome(dvergr.transform.position);

            if (biome == Heightmap.Biome.None)
            {
                return;
            }

            Trader trader = dvergr.gameObject.AddComponent<Trader>();

            trader.m_name = dvergr.m_name;
            trader.m_standRange = 0f;
            trader.m_greetRange = 0f;
            trader.m_byeRange = 0f;
            trader.m_randomTalkInterval = 3600f;
            trader.m_randomBuy.Add("$hex_dvergr_fine_trade");

            AddForgePact(trader);

            if (Features.Progression.DvergrStarterCharacterState.IsStarter(dvergr))
            {
                return;
            }

            switch (biome)
            {
                case Heightmap.Biome.Mistlands:
                    {
                        AddMistlandsItems(trader);
                        break;
                    }

                case Heightmap.Biome.AshLands:
                    {
                        AddAshlandsItems(trader);
                        break;
                    }
            }
        }

        internal static bool IsDvergrTrader(Trader trader)
        {
            return trader && trader.GetComponent<Humanoid>()?.GetFaction() == Character.Faction.Dverger;
        }

        internal static bool IsForgePact(Trader.TradeItem item)
        {
            return item != null && item.m_prefab && item.m_prefab.gameObject.name == PrefabHelper.MeadTasty;
        }

        internal static bool IsAshlandsFlametal(Trader.TradeItem item)
        {
            return item != null && item.m_prefab && item.m_prefab.gameObject.name == PrefabHelper.FlametalNew;
        }

        internal static string GetForgePactPaymentName()
        {
            GameObject prefab = ObjectDB.instance.GetItemPrefab(PrefabHelper.MeadTasty);

            if (!prefab)
            {
                return null;
            }

            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();

            return itemDrop ? itemDrop.m_itemData.m_shared.m_name : null;
        }

        internal static string GetAshlandsPaymentName()
        {
            GameObject prefab = ObjectDB.instance.GetItemPrefab(PrefabHelper.MoltenCore);

            if (!prefab)
            {
                return null;
            }

            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();

            return itemDrop ? itemDrop.m_itemData.m_shared.m_name : null;
        }

        internal static bool CanAffordForgePact(Player player, Character character)
        {
            if (!player || !character)
            {
                return false;
            }

            if (Features.Progression.DvergrStarterCharacterState.IsStarter(character))
            {
                return Features.Progression.DvergrStarterVendorService.IsAvailable(player, character);
            }

            if (DvergrCompanionState.IsHired(character) || DvergrCompanionManager.HasHiredCompanion(player))
            {
                return false;
            }

            if (!Features.Progression.DvergrStarterCharacterState.CanHire(player, character))
            {
                return false;
            }

            if (Features.Progression.DvergrStarterCharacterState.HasFreePact(player, character))
            {
                return true;
            }

            string paymentName = GetForgePactPaymentName();

            if (string.IsNullOrEmpty(paymentName))
            {
                return false;
            }

            return player.GetInventory().CountItems(paymentName) >= DvergrPricingConfig.ForgePactCost;
        }

        internal static bool CanAffordAshlandsFlametal(Player player)
        {
            if (!player)
            {
                return false;
            }

            string paymentName = GetAshlandsPaymentName();

            if (string.IsNullOrEmpty(paymentName))
            {
                return false;
            }

            return player.GetInventory().CountItems(paymentName) >= DvergrPricingConfig.AshlandsMoltenCoreCost;
        }

        internal static string GetPaymentTrophy(Inventory inventory)
        {
            foreach (string prefabName in PrefabHelper.MistlandsTrophyPrefabs)
            {
                GameObject prefab = ObjectDB.instance.GetItemPrefab(prefabName);

                if (!prefab)
                {
                    continue;
                }

                string itemName = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;

                if (inventory.CountItems(itemName) > 0)
                {
                    return itemName;
                }
            }

            return null;
        }

        private static void AddForgePact(Trader trader)
        {
            GameObject prefab = ObjectDB.instance.GetItemPrefab(PrefabHelper.MeadTasty);

            if (!prefab)
            {
                HexLog.LogWarning("[Dvergr Trader] MeadTasty prefab not found.");
                return;
            }

            trader.m_items.Add(new Trader.TradeItem { m_prefab = prefab.GetComponent<ItemDrop>(), m_stack = 1, m_price = 0, m_tooltip = Features.Progression.DvergrStarterVendorService.PactTooltip(trader.GetComponent<Character>()), m_buyPlayerEffects = new EffectList() });
        }

        private static void AddMistlandsItems(Trader trader)
        {
            DvergrMistlandsStoreManager.AddOffers(trader);
            GameObject needlePrefab = ObjectDB.instance.GetItemPrefab(PrefabHelper.DvergrNeedle);

            if (needlePrefab)
            {
                trader.m_items.Add(new Trader.TradeItem { m_prefab = needlePrefab.GetComponent<ItemDrop>(), m_stack = 1, m_price = 0, m_tooltip = "", m_buyPlayerEffects = new EffectList() });
            }
            else
            {
                HexLog.LogWarning("[Dvergr Trader] DvergrNeedle prefab not found.");
            }

            GameObject softTissuePrefab = ObjectDB.instance.GetItemPrefab(PrefabHelper.Softtissue);

            if (softTissuePrefab)
            {
                trader.m_items.Add(new Trader.TradeItem { m_prefab = softTissuePrefab.GetComponent<ItemDrop>(), m_stack = 20, m_price = 2000, m_tooltip = "", m_buyPlayerEffects = new EffectList() });
            }
            else
            {
                HexLog.LogWarning("[Dvergr Trader] Softtissue prefab not found.");
            }
        }

        private static void AddAshlandsItems(Trader trader)
        {
            GameObject flametalPrefab = ObjectDB.instance.GetItemPrefab(PrefabHelper.FlametalNew);

            if (!flametalPrefab)
            {
                HexLog.LogWarning("[Dvergr Trader] FlametalNew prefab not found.");
                return;
            }

            ItemDrop itemDrop = flametalPrefab.GetComponent<ItemDrop>();

            if (!itemDrop)
            {
                HexLog.LogWarning("[Dvergr Trader] FlametalNew ItemDrop component not found.");
                return;
            }

            trader.m_items.Add(new Trader.TradeItem { m_prefab = itemDrop, m_stack = DvergrPricingConfig.AshlandsFlametalQuantity, m_price = 0, m_tooltip = HexLocalizationManager.Localize("$hex_dvergr_flametal_tooltip", DvergrPricingConfig.AshlandsMoltenCoreCost, DvergrPricingConfig.AshlandsFlametalQuantity), m_buyPlayerEffects = new EffectList() });

            HexLog.LogInfo($"[Dvergr Trader] Ashlands inventory initialized: FlametalNew={DvergrPricingConfig.AshlandsFlametalQuantity}, MoltenCore={DvergrPricingConfig.AshlandsMoltenCoreCost}");
        }
    }
}
