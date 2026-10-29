
using HexDvergrAllies.Core.Managers;
using UnityEngine;

namespace HexDvergrAllies.Core.UI
{
    internal static class DvergrPactDialogController
    {
        private static bool _confirmationPending;

        internal static bool ShowBreakPactConfirmation(Player player, Character character)
        {
            if (!player || !character)
            {
                return false;
            }

            if (_confirmationPending)
            {
                Plugin.Log.LogInfo("[Dvergr Companion] Break Pact confirmation already pending.");

                return false;
            }

            if (!UnifiedPopup.IsAvailable())
            {
                Plugin.Log.LogWarning("[Dvergr Companion] Break Pact confirmation unavailable: UnifiedPopup is not available.");

                return false;
            }

            if (UnifiedPopup.IsVisible())
            {
                Plugin.Log.LogWarning("[Dvergr Companion] Break Pact confirmation unavailable: Another popup is visible.");

                return false;
            }

            ZNetView nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                return false;
            }

            if (!DvergrCompanionManager.IsHiredByPlayer(character, player))
            {
                return false;
            }

            ZDOID companionId = nview.GetZDO().m_uid;

            YesNoPopup popup = new YesNoPopup("Break Your Pact?", "Release this Dvergr from your service?\n\n" + "Your companion will return to its original home.", () => { _confirmationPending = false; UnifiedPopup.Pop(); if (!player || ZNetScene.instance == null) { return; } GameObject companionObject = ZNetScene.instance.FindInstance(companionId); if (!companionObject) { Plugin.Log.LogWarning("[Dvergr Companion] Break Pact aborted: Companion no longer loaded."); return; } Character currentCharacter = companionObject.GetComponent<Character>(); if (!currentCharacter || !DvergrCompanionManager.IsHiredByPlayer(currentCharacter, player)) { Plugin.Log.LogWarning("[Dvergr Companion] Break Pact aborted: Companion no longer belongs to player."); return; } Plugin.Log.LogInfo($"[Dvergr Companion] Break Pact confirmed: ZDO={companionId}"); DvergrCompanionManager.RequestUnhire(player, currentCharacter); }, () => { _confirmationPending = false; UnifiedPopup.Pop(); Plugin.Log.LogInfo("[Dvergr Companion] Break Pact canceled by player."); }, false, false);

            _confirmationPending = true;

            UnifiedPopup.Push(popup);
            UnifiedPopup.SetFocus();

            Plugin.Log.LogInfo($"[Dvergr Companion] Break Pact confirmation opened: Dvergr={character.name}, ZDO={companionId}");

            return true;
        }
    }
}
