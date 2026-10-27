using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(StoreGui), "Hide")]
    internal static class StoreGuiTradeSessionPatch
    {
        private static void Prefix(Trader ___m_trader)
        {
            if (!___m_trader || !DvergrTraderManager.IsDvergrTrader(___m_trader))
            {
                return;
            }

            DvergrTradeSessionManager.Close();
        }
    }
}