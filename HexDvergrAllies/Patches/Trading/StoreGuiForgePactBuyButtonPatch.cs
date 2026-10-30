using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(StoreGui), "UpdateBuyButton")]
    internal static class StoreGuiForgePactBuyButtonPatch
    {
        private static void Postfix(StoreGui __instance, Trader ___m_trader, Trader.TradeItem ___m_selectedItem)
        {
            if (!___m_trader || !DvergrTraderManager.IsDvergrTrader(___m_trader))
            {
                return;
            }

            if (___m_selectedItem == null || !DvergrTraderManager.IsForgePact(___m_selectedItem))
            {
                return;
            }

            bool canHire = DvergrTraderManager.CanAffordForgePact(Player.m_localPlayer, ___m_trader.GetComponent<Character>());

            __instance.m_buyButton.interactable = canHire;
            __instance.m_buyButton.GetComponent<UITooltip>().m_text = canHire
                ? ""
                : Localization.instance.Localize("$msg_missingrequirement");
        }
    }
}
