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

            var modePressed = DvergrInteractionSettings.ModeShortcut.Value.IsDown();
            var renamePressed = DvergrInteractionSettings.RenameShortcut.Value.IsDown();
            var interactionPressed = DvergrInteractionSettings.InteractionShortcut.Value.IsDown();

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
                ToggleMode(player, character);
            }
            else if (renamePressed)
            {
                RenameCompanion(player, character);
            }
            else if (interactionPressed)
            {
                BreakPact(player, character);
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

            if (!DvergrInteractionSettings.IsWithinRange(player, character))
            {
                return null;
            }

            return character;
        }

        private static void ToggleMode(Player player, Character character)
        {
            DvergrCompanionManager.RequestToggleMode(player, character);
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

        private static void BreakPact(Player player, Character character)
        {
            if (!DvergrCompanionManager.IsHiredByPlayer(character, player))
            {
                return;
            }

            DvergrPactDialogController.ShowBreakPactConfirmation(player, character);
        }
    }
}