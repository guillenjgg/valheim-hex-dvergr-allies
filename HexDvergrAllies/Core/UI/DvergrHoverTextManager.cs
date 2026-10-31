using BepInEx.Configuration;
using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.Trading;
using UnityEngine;

namespace HexDvergrAllies.Core.UI
{
    internal static class DvergrHoverTextManager
    {
        private const string CompanionModeText = "Companion Mode";
        private const string SentryModeText = "Sentry Mode";
        private const string EnableCompanionModeText = "Enable Companion Mode";
        private const string EnableSentryModeText = "Enable Sentry Mode";
        private const string BreakPactText = "Break Pact";
        private const string RenameText = "Rename";
        private const string TraderInteractionText = "\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact";

        private static readonly ConfigEntry<KeyboardShortcut> InteractionShortcut = HexDvergrAlliesConfiguration.CompanionInteractionShortcut;
        private static readonly ConfigEntry<KeyboardShortcut> RenameShortcut = HexDvergrAlliesConfiguration.CompanionRenameShortcut;
        private static readonly ConfigEntry<KeyboardShortcut> ModeShortcut = HexDvergrAlliesConfiguration.CompanionModeShortcut;

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

            if (!player || Vector3.Distance(player.transform.position, character.transform.position) > DvergrTraderManager.InteractionDistance)
            {
                hoverText = "";
                return true;
            }

            var companionName = DvergrCompanionManager.GetCompanionName(character);
            var mode = DvergrCompanionManager.GetMode(character);
            var isSentryMode = mode == DvergrCompanionManager.CompanionMode.Sentry;

            hoverText = companionName + "\n" + (isSentryMode ? SentryModeText : CompanionModeText);

            if (DvergrCompanionManager.IsHiredByPlayer(character, player))
            {
                var modeAction = isSentryMode ? EnableCompanionModeText : EnableSentryModeText;

                hoverText += "\n[<color=yellow><b>" + InteractionShortcut.Value + "</b></color>] " + BreakPactText;
                hoverText += "\n[<color=yellow><b>" + RenameShortcut.Value + "</b></color>] " + RenameText;
                hoverText += "\n[<color=yellow><b>" + ModeShortcut.Value + "</b></color>] " + modeAction;
            }

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

            if (!player || Vector3.Distance(player.transform.position, character.transform.position) > DvergrTraderManager.InteractionDistance)
            {
                hoverText = "";
                return true;
            }

            hoverText = Localization.instance.Localize(trader.m_name + TraderInteractionText);

            return true;
        }
    }
}