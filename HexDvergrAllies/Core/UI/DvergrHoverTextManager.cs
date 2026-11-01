using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.Trading;

namespace HexDvergrAllies.Core.UI
{
    internal static class DvergrHoverTextManager
    {
        internal static bool Init(Character character, out string hoverText)
        {
            if (ShowCompanionText(character, out hoverText))
            {
                return false;
            }

            if (ShowTraderText(character, out hoverText))
            {
                return false;
            }

            hoverText = null;
            return true;
        }

        private static bool ShowCompanionText(Character character, out string hoverText)
        {
            hoverText = null;

            if (!DvergrCompanionManager.IsHired(character))
            {
                return false;
            }

            var player = Player.m_localPlayer;

            if (!player || !DvergrInteractionHelper.IsWithinRange(player, character))
            {
                hoverText = "";
                return true;
            }

            var companionName = DvergrCompanionManager.GetCompanionName(character);
            var mode = DvergrCompanionManager.GetMode(character);
            var isSentryMode = mode == DvergrCompanionManager.CompanionMode.Sentry;

            hoverText = companionName + "\n" + (isSentryMode ? "$hex_dvergr_sentry_mode" : "$hex_dvergr_companion_mode");

            if (DvergrCompanionManager.IsHiredByPlayer(character, player))
            {
                var modeAction = isSentryMode ? "$hex_dvergr_enable_companion_mode" : "$hex_dvergr_enable_sentry_mode";

                hoverText += "\n[<color=yellow><b>" + HexDvergrAlliesConfiguration.CompanionInteractionShortcut.Value + "</b></color>] $hex_dvergr_break_pact";
                hoverText += "\n[<color=yellow><b>" + HexDvergrAlliesConfiguration.CompanionRenameShortcut.Value + "</b></color>] $hex_dvergr_rename";
                hoverText += "\n[<color=yellow><b>" + HexDvergrAlliesConfiguration.CompanionModeShortcut.Value + "</b></color>] " + modeAction;
            }

            hoverText = Localization.instance.Localize(hoverText);

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