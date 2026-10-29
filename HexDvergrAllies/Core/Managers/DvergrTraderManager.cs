using UnityEngine;

namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrTraderManager
    {
        internal const float InteractionDistance = 2f;
        internal const string ForgePactPrefab = "MeadTasty";
        internal const int ForgePactCost = 2;

        private static readonly string[] MistlandsTrophyPrefabs =
        {
            "TrophySeeker",
            "TrophyGjall",
            "TrophySeekerBrute"
        };

        internal static void Initialize(Humanoid dvergr)
        {
            if (dvergr.GetComponent<Trader>())
            {
                return;
            }

            Heightmap.Biome biome = Heightmap.FindBiome(
                dvergr.transform.position);

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
            trader.m_randomBuy.Add("A fine trade.");

            AddForgePact(trader);

            switch (biome)
            {
                case Heightmap.Biome.Mistlands:
                    AddMistlandsItems(trader);
                    break;

                case Heightmap.Biome.AshLands:
                    AddAshlandsItems(trader);
                    break;
            }
        }

        internal static bool IsDvergrTrader(Trader trader)
        {
            return trader &&
                   trader.GetComponent<Humanoid>()?.GetFaction() ==
                   Character.Faction.Dverger;
        }

        internal static bool IsForgePact(Trader.TradeItem item)
        {
            return item != null &&
                   item.m_prefab &&
                   item.m_prefab.gameObject.name == ForgePactPrefab;
        }

        internal static string GetForgePactPaymentName()
        {
            GameObject prefab = ObjectDB.instance.GetItemPrefab(ForgePactPrefab);

            if (!prefab)
            {
                return null;
            }

            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();

            return itemDrop
                ? itemDrop.m_itemData.m_shared.m_name
                : null;
        }

        internal static bool CanAffordForgePact(Player player, Character character)
        {
            if (!player || !character)
            {
                return false;
            }

            if (DvergrCompanionManager.IsHired(character) ||
                DvergrCompanionManager.HasHiredCompanion(player))
            {
                return false;
            }

            string paymentName = GetForgePactPaymentName();

            if (string.IsNullOrEmpty(paymentName))
            {
                return false;
            }

            return player.GetInventory().CountItems(paymentName) >= ForgePactCost;
        }

        internal static string GetPaymentTrophy(Inventory inventory)
        {
            foreach (string prefabName in MistlandsTrophyPrefabs)
            {
                GameObject prefab = ObjectDB.instance.GetItemPrefab(
                    prefabName);

                if (!prefab)
                {
                    continue;
                }

                string itemName = prefab.GetComponent<ItemDrop>()
                    .m_itemData.m_shared.m_name;

                if (inventory.CountItems(itemName) > 0)
                {
                    return itemName;
                }
            }

            return null;
        }

        private static void AddForgePact(Trader trader)
        {
            GameObject prefab = ObjectDB.instance.GetItemPrefab(
                ForgePactPrefab);

            if (!prefab)
            {
                Plugin.Log.LogWarning(
                    "[Dvergr Trader] MeadTasty prefab not found.");

                return;
            }

            trader.m_items.Add(new Trader.TradeItem
            {
                m_prefab = prefab.GetComponent<ItemDrop>(),
                m_stack = 1,
                m_price = 0,
                m_tooltip = "Forge Pact: Hire this Dvergr for 2 Tasty Mead.",
                m_buyPlayerEffects = new EffectList()
            });
        }

        private static void AddMistlandsItems(Trader trader)
        {
            GameObject needlePrefab = ObjectDB.instance.GetItemPrefab(
                "DvergrNeedle");

            if (needlePrefab)
            {
                trader.m_items.Add(new Trader.TradeItem
                {
                    m_prefab = needlePrefab.GetComponent<ItemDrop>(),
                    m_stack = 1,
                    m_price = 0,
                    m_tooltip = "",
                    m_buyPlayerEffects = new EffectList()
                });
            }
            else
            {
                Plugin.Log.LogWarning(
                    "[Dvergr Trader] DvergrNeedle prefab not found.");
            }

            GameObject softTissuePrefab = ObjectDB.instance.GetItemPrefab(
                "Softtissue");

            if (softTissuePrefab)
            {
                trader.m_items.Add(new Trader.TradeItem
                {
                    m_prefab = softTissuePrefab.GetComponent<ItemDrop>(),
                    m_stack = 20,
                    m_price = 2000,
                    m_tooltip = "",
                    m_buyPlayerEffects = new EffectList()
                });
            }
            else
            {
                Plugin.Log.LogWarning(
                    "[Dvergr Trader] Softtissue prefab not found.");
            }
        }

        private static void AddAshlandsItems(Trader trader)
        {
            // Ashlands inventory will be added later.
        }
    }
}