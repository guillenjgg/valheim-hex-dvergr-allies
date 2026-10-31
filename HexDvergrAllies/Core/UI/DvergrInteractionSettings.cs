using BepInEx.Configuration;
using HexDvergrAllies.Core.Trading;
using UnityEngine;

namespace HexDvergrAllies.Core.UI
{
    internal static class DvergrInteractionSettings
    {
        internal static readonly ConfigEntry<KeyboardShortcut> InteractionShortcut = HexDvergrAlliesConfiguration.CompanionInteractionShortcut;
        internal static readonly ConfigEntry<KeyboardShortcut> RenameShortcut = HexDvergrAlliesConfiguration.CompanionRenameShortcut;
        internal static readonly ConfigEntry<KeyboardShortcut> ModeShortcut = HexDvergrAlliesConfiguration.CompanionModeShortcut;

        internal static bool IsWithinRange(Player player, Character character)
        {
            return Vector3.Distance(player.transform.position, character.transform.position) <= DvergrTraderManager.InteractionDistance;
        }
    }
}