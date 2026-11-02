using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.Trading;

namespace HexDvergrAllies.Core.UI
{
    internal static class DvergrHoverTextManager
    {
        internal static bool Init(Character character, out string hoverText)
        {
            if (DvergrCompanionManager.IsHired(character))
            {
                hoverText = "";
                return false;
            }

            if (ShowTraderText(character, out hoverText))
            {
                return false;
            }

            hoverText = null;
            return true;
        }

        private static bool ShowTraderText(Character character, out string hoverText)
        {
            hoverText = null;

            var trader = character.GetComponent<Trader>();

            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
            {
                return false;
            }

            var player = Player.m_localPlayer;

            if (!player || !DvergrInteractionHelper.IsWithinRange(player, character))
            {
                hoverText = "";
                return true;
            }

            hoverText = Localization.instance.Localize(trader.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact");

            return true;
        }
    }
}