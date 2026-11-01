using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.Trading;

namespace HexDvergrAllies.Core.UI
{
    internal static class DvergrCompanionShortcutManager
    {
        private const string RenameDialogTitle = "Rename Companion";
        private const int MaxCompanionNameLength = 24;


        internal static void Update(Player player)
        {
            if (player != Player.m_localPlayer || TextInput.IsVisible())
            {
                return;
            }

            if (HexDvergrAlliesConfiguration.CompanionUIShortcut.Value.IsDown())
            {
                DvergrCompanionUIController.Toggle();
                return;
            }

            if (DvergrCompanionUIController.IsVisible)
            {
                return;
            }

            var modePressed = HexDvergrAlliesConfiguration.CompanionModeShortcut.Value.IsDown();
            var renamePressed = HexDvergrAlliesConfiguration.CompanionRenameShortcut.Value.IsDown();
            var interactionPressed = HexDvergrAlliesConfiguration.CompanionInteractionShortcut.Value.IsDown();

            if (!modePressed && !renamePressed && !interactionPressed)
            {
                return;
            }

            var character = GetHoveredCompanion(player);

            if (!character)
            {
                return;
            }

            if (modePressed)
            {
                DvergrCompanionManager.RequestToggleMode(player, character);
            }
            else if (renamePressed)
            {
                RenameCompanion(player, character);
            }
            else if (interactionPressed && DvergrCompanionManager.IsHiredByPlayer(character, player))
            {
                DvergrPactDialogController.ShowBreakPactConfirmation(player, character);
            }
        }


        private static Character GetHoveredCompanion(Player player)
        {
            var hoveringObject = player.GetHoverObject();

            if (!hoveringObject)
            {
                return null;
            }

            var trader = hoveringObject.GetComponentInParent<Trader>();

            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
            {
                return null;
            }

            var character = trader.GetComponent<Character>();

            if (!character)
            {
                return null;
            }

            if (!DvergrInteractionHelper.IsWithinRange(player, character))
            {
                return null;
            }

            return character;
        }

        private static void RenameCompanion(Player player, Character character)
        {
            if (!DvergrCompanionManager.IsHiredByPlayer(character, player))
            {
                return;
            }

            var receiver = character.GetComponent<DvergrCompanionNameReceiver>();

            if (!receiver)
            {
                receiver = character.gameObject.AddComponent<DvergrCompanionNameReceiver>();
            }

            receiver.Initialize(character);
            TextInput.instance.RequestText(receiver, RenameDialogTitle, MaxCompanionNameLength);
        }
    }
}