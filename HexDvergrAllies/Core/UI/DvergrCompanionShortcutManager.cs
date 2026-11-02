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
            }
        }
    }
}