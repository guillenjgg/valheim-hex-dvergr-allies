namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrTraderManager
    {
        internal const float InteractionDistance = 2f;

        private static readonly string[] TrophyPrefabs =
        {
            "TrophySeeker",
            "TrophyGjall",
            "TrophySeekerBrute"
        };

        internal static bool IsDvergrTrader(Trader trader)
        {
            return trader.GetComponent<Humanoid>()?.GetFaction() == Character.Faction.Dverger;
        }

        internal static string GetPaymentTrophy(Inventory inventory)
        {
            foreach (var prefabName in TrophyPrefabs)
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

        internal static void Initialize(Humanoid dvergr)
        {
            if (dvergr.GetComponent<Trader>())
            {
                return;
            }

            var trader = dvergr.gameObject.AddComponent<Trader>();
            trader.m_name = dvergr.m_name;
            trader.m_standRange = 0f;
            trader.m_greetRange = 0f;
            trader.m_byeRange = 0f;
            trader.m_randomTalkInterval = 3600f;

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
        }
    }
}
