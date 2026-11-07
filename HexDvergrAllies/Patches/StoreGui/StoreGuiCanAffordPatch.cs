using HarmonyLib;
using HexDvergrAllies.Core.Trading;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(StoreGui), "CanAfford")]
    internal static class StoreGuiCanAffordPatch
    {
        private static void Postfix(Trader ___m_trader, Trader.TradeItem __0, ref bool __result)
        {
            DvergrTradeAffordability.Apply(___m_trader, __0, ref __result);
        }
    }
}
