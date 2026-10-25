namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrTraderManager
    {
        internal static bool IsDvergrTrader(Trader trader)
        {
            return trader.GetComponent<MonsterAI>()?.GetComponent<Character>()?.GetFaction() == Character.Faction.Dverger;
        }

        internal static void Initialize(Humanoid dvergr)
        {
            if (dvergr.GetComponent<Trader>())
                return;

            var trader = dvergr.gameObject.AddComponent<Trader>();
            trader.m_name = "$enemy_dvergr";
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