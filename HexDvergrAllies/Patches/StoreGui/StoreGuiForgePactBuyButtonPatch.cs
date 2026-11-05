using HarmonyLib;
using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.Trading;

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

            var player = Player.m_localPlayer;
            var character = ___m_trader.GetComponent<Character>();
            var alreadyHired = DvergrCompanionManager.HasHiredCompanion(player);
            var canHire = DvergrTraderManager.CanAffordForgePact(player, character);

            __instance.m_buyButton.interactable = canHire;
            __instance.m_buyButton.GetComponent<UITooltip>().m_text = alreadyHired
                ? Localization.instance.Localize("$hex_dvergr_one_companion_only")
                : canHire ? "" : Localization.instance.Localize("$msg_missingrequirement");
        }
    }
}
