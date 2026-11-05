using HexDvergrAllies.Core.Companions;
using HarmonyLib;

namespace HexDvergrAllies.Core.UI
{
    internal static class DvergrCompanionShortcutManager
    {
        private static readonly FastInvokeHandler TakeInputHandler = MethodInvoker.GetHandler(AccessTools.Method(typeof(Player), "TakeInput"));
        private static ZDOID _pendingModeCompanion;
        private static DvergrCompanionMode? _pendingMode;
        private static float _modeMessageDeadline;

        internal static void Update(Player player)
        {
            if (!player || player != Player.m_localPlayer)
            {
                return;
            }

            if (_pendingMode.HasValue)
            {
                if (DvergrCompanionManager.TryGetHiredCompanionZDO(player, out var pendingZdo) &&
                    pendingZdo.m_uid == _pendingModeCompanion && DvergrCompanionState.GetMode(pendingZdo) == _pendingMode.Value)
                {
                    player.Message(MessageHud.MessageType.Center, _pendingMode.Value == DvergrCompanionMode.Sentry
                        ? "Sentry mode activated." : "Companion mode activated.");
                    _pendingMode = null;
                }
                else if (UnityEngine.Time.time >= _modeMessageDeadline)
                {
                    _pendingMode = null;
                }
            }

            if (TextInput.IsVisible())
            {
                return;
            }

            if (HexDvergrAlliesConfiguration.CompanionUIShortcut.Value.IsDown())
            {
                DvergrCompanionUIController.Toggle();
            }

            if (!(bool)TakeInputHandler(player, null))
            {
                return;
            }

            if (HexDvergrAlliesConfiguration.CompanionSentryShortcut.Value.IsDown() &&
                DvergrCompanionManager.TryGetHiredCompanionZDO(player, out var companionZdo))
            {
                _pendingModeCompanion = companionZdo.m_uid;
                _pendingMode = DvergrCompanionState.GetMode(companionZdo) == DvergrCompanionMode.Sentry
                    ? DvergrCompanionMode.Companion : DvergrCompanionMode.Sentry;
                _modeMessageDeadline = UnityEngine.Time.time + 5f;
                DvergrCompanionModeManager.RequestRemoteToggleMode(player);
            }

            if (HexDvergrAlliesConfiguration.CompanionRecallShortcut.Value.IsDown())
            {
                _pendingMode = null;
                DvergrCompanionRecallManager.RequestRemoteRecall(player);
            }
        }
    }
}
