using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(StoreGui), "BuySelectedItem")]
    internal static class StoreGuiBuySelectedItemPatch
    {
        private static bool Prefix(Trader ___m_trader)
        {
            return !___m_trader || !DvergrTraderManager.IsDvergrTrader(___m_trader);
        }
    }
}