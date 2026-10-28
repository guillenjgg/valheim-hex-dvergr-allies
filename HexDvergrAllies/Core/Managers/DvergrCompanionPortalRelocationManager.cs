using UnityEngine;

namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrCompanionPortalRelocationManager
    {
        private const string RelocateRpc = "HexDvergrRelocate";
        private const string HiredKey = "HexDvergr_IsHired";
        private const string OwnerKey = "HexDvergr_HiredBy";
        private const float MaximumSourceDistance = 30f;

        internal static void Register()
        {
            ZRoutedRpc.instance.Register<ZDOID, ZDOID, Vector3, Vector3>(
                RelocateRpc, RPC_Relocate);

            Plugin.Log.LogInfo("[Dvergr Portal] Relocation RPC registered.");
        }

        internal static void RequestRelocation(
            Player player,
            ZDOID companionId,
            Vector3 sourcePosition)
        {
            if (!player || !player.IsOwner())
            {
                return;
            }

            var destination = player.transform.position + player.transform.right * 2f;

            ZRoutedRpc.instance.InvokeRoutedRPC(
                ZRoutedRpc.instance.GetServerPeerID(),
                RelocateRpc,
                player.GetZDOID(),
                companionId,
                sourcePosition,
                destination);

            Plugin.Log.LogInfo(
                $"[Dvergr Portal] Relocation requested: Companion={companionId}, Destination={destination}");
        }

        private static void RPC_Relocate(
            long sender,
            ZDOID playerId,
            ZDOID companionId,
            Vector3 sourcePosition,
            Vector3 destination)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            var playerObject = ZNetScene.instance.FindInstance(playerId);
            var player = playerObject ? playerObject.GetComponent<Player>() : null;

            if (!player)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Portal] Relocation rejected: Player={playerId}, Reason=Player not found");
                return;
            }

            var playerView = player.GetComponent<ZNetView>();

            if (!playerView || !playerView.IsValid() ||
                playerView.GetZDO().GetOwner() != sender)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Portal] Relocation rejected: Companion={companionId}, Reason=Invalid sender");
                return;
            }

            var zdo = ZDOMan.instance.GetZDO(companionId);

            if (zdo == null)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Portal] Relocation rejected: Companion={companionId}, Reason=ZDO not found");
                return;
            }

            if (!zdo.GetBool(HiredKey, false) ||
                zdo.GetLong(OwnerKey, 0L) != player.GetPlayerID())
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Portal] Relocation rejected: Companion={companionId}, Reason=Not hired by player");
                return;
            }

            if (Vector3.Distance(zdo.GetPosition(), sourcePosition) > MaximumSourceDistance)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Portal] Relocation rejected: Companion={companionId}, Reason=Source position mismatch");
                return;
            }

            if (Vector3.Distance(player.transform.position, destination) > 10f)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Portal] Relocation rejected: Companion={companionId}, Reason=Destination mismatch");
                return;
            }

            var loadedObject = ZNetScene.instance.FindInstance(companionId);

            if (loadedObject)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Portal] Relocation deferred: Companion={companionId}, Reason=Still loaded on server");
                return;
            }

            var previousPosition = zdo.GetPosition();

            zdo.SetOwner(ZDOMan.GetSessionID());
            zdo.SetPosition(destination);
            ZDOMan.instance.ForceSendZDO(companionId);

            Plugin.Log.LogInfo(
                $"[Dvergr Portal] Relocated: Companion={companionId}, From={previousPosition}, To={destination}");
        }
    }
}
