
namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrTraderManager
    {
        internal const float InteractionDistance = 2f;

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

            var biome = Heightmap.FindBiome(dvergr.transform.position);

            if (biome == Heightmap.Biome.None)
            {
                return;
            }

            var trader = dvergr.gameObject.AddComponent<Trader>();
            trader.m_name = dvergr.m_name;
            trader.m_standRange = 0f;
            trader.m_greetRange = 0f;
            trader.m_byeRange = 0f;
            trader.m_randomTalkInterval = 3600f;
            trader.m_randomBuy.Add("A fine trade.");

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
            return trader.GetComponent<Humanoid>()?.GetFaction() == Character.Faction.Dverger;
        }

        internal static string GetPaymentTrophy(Inventory inventory)
        {
            foreach (var prefabName in MistlandsTrophyPrefabs)
            {
                var prefab = ObjectDB.instance.GetItemPrefab(prefabName);
                var itemName = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;

                if (inventory.CountItems(itemName) > 0)
                {
                    return itemName;
                }
            }

            return null;
        }

        private static void AddMistlandsItems(Trader trader)
        {
            var needlePrefab = ObjectDB.instance.GetItemPrefab("DvergrNeedle");

            if (needlePrefab == null)
            {
                Plugin.Log.LogWarning("[Dvergr Trader] DvergrNeedle prefab not found.");
                return;
            }

            trader.m_items.Add(new Trader.TradeItem
            {
                m_prefab = needlePrefab.GetComponent<ItemDrop>(),
                m_stack = 1,
                m_price = 0,
                m_tooltip = ""
            });

            var softTissuePrefab = ObjectDB.instance.GetItemPrefab("Softtissue");

            if (softTissuePrefab == null)
            {
                Plugin.Log.LogWarning("[Dvergr Trader] SoftTissue prefab not found.");
                return;
            }

            trader.m_items.Add(new Trader.TradeItem
            {
                m_prefab = softTissuePrefab.GetComponent<ItemDrop>(),
                m_stack = 20,
                m_price = 2000,
                m_tooltip = ""
            });
        }

        private static void AddAshlandsItems(Trader trader)
        {
            // Ashlands inventory will be added later.
        }
    }
}
