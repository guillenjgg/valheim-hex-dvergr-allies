using HexDvergrAllies.Core.Networking;
using UnityEngine;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionRecallManager
    {
        internal static void RequestRemoteRecall(Player player)
        {
            if (!player || !DvergrCompanionManager.TryGetHiredCompanionId(player, out var companionId))
            {
                return;
            }

            var destination = player.transform.position + player.transform.right * 2f;

            ZRoutedRpc.instance.InvokeRoutedRPC(DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance), DvergrRpcManager.RemoteRecallRpc, player.GetZDOID(), companionId, destination);
        }

        internal static void RPC_RemoteRecall(long sender, ZDOID playerId, ZDOID companionId, Vector3 destination)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            var playerZdo = ZDOMan.instance.GetZDO(playerId);
            var companionZdo = ZDOMan.instance.GetZDO(companionId);

            if (playerZdo == null || companionZdo == null || playerZdo.GetOwner() != sender)
            {
                return;
            }

            var verifiedPlayerId = playerZdo.GetLong(ZDOVars.s_playerID, 0L);

            if (verifiedPlayerId == 0L || !companionZdo.GetBool(DvergrCompanionState.HiredKey, false) || companionZdo.GetLong(DvergrCompanionState.OwnerKey, 0L) != verifiedPlayerId)
            {
                return;
            }

            if (Vector3.Distance(playerZdo.GetPosition(), destination) > 10f)
            {
                return;
            }

            companionZdo.Set(DvergrCompanionState.ModeKey, (int)DvergrCompanionMode.Companion);
            companionZdo.Set(DvergrCompanionState.RecallPositionKey, destination);
            companionZdo.Set(DvergrCompanionState.PendingModeTeleportKey, true);
        }
    }
}