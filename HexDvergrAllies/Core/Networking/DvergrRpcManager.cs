using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Core.Networking
{
    internal static class DvergrRpcManager
    {
        internal const string HireRpc = "HexDvergrHire";
        internal const string HireResultRpc = "HexDvergrHireResult";
        internal const string UnhireRpc = "HexDvergrUnhire";
        internal const string RenameRpc = "HexDvergrRename";
        internal const string SetModeRpc = "HexDvergrSetMode";

        internal const string RemoteUnhireRpc = "HexDvergrRemoteUnhire";
        internal const string RemoteUnhireUnloadedRpc = "HexDvergrRemoteUnhireUnloaded";
        internal const string ProcessRemoteUnhireRpc = "HexDvergrProcessRemoteUnhire";
        internal const string RemoteUnhireCompletedRpc = "HexDvergrRemoteUnhireCompleted";
        internal const string RemoteSetModeRpc = "HexDvergrRemoteSetMode";
        internal const string RemoteRecallRpc = "HexDvergrRemoteRecall";
        internal const string RemoteRenameRpc = "HexDvergrRemoteRename";

        internal static void Register(ZNetView nview, Character character)
        {
            nview.Register<ZDOID, long>(HireRpc, (sender, playerId, requestId) => DvergrForgePactManager.Hire(character, sender, playerId, requestId));
            nview.Register<ZDOID, long, bool>(HireResultRpc, (sender, playerId, requestId, success) => DvergrForgePactManager.OnHireResult(character, sender, playerId, requestId, success));
            nview.Register<ZDOID>(UnhireRpc, (sender, playerId) => DvergrCompanionManager.Unhire(character, sender, playerId));
            nview.Register<ZDOID, string>(RenameRpc, (sender, playerId, name) => DvergrCompanionManager.Rename(character, sender, playerId, name));
            nview.Register<ZDOID>(SetModeRpc, (sender, playerId) => DvergrCompanionManager.SetMode(character, sender, playerId));
        }
    }
}