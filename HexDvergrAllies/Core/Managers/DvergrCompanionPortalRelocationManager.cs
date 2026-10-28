using System;
using HarmonyLib;
using UnityEngine;

namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrCompanionPortalRelocationManager
    {
        private const string RelocateRpc = "HexDvergrRelocate";
        private const string HiredKey = "HexDvergr_IsHired";
        private const string OwnerKey = "HexDvergr_HiredBy";

        private const float MaximumSourceDistance = 30f;
        private const float MaximumDestinationDistance = 10f;
        private const float MinimumTravelDistance = 30f;

        private static readonly Func<ZRoutedRpc, long> GetServerPeerID =
            AccessTools.MethodDelegate<Func<ZRoutedRpc, long>>(
                AccessTools.Method(typeof(ZRoutedRpc), "GetServerPeerID"));

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
                GetServerPeerID(ZRoutedRpc.instance),
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

            var playerZdo = ZDOMan.instance.GetZDO(playerId);

            if (playerZdo == null || playerZdo.GetOwner() != sender)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Portal] Relocation rejected: Companion={companionId}, Reason=Invalid player or sender");
                return;
            }

            var companionZdo = ZDOMan.instance.GetZDO(companionId);

            if (companionZdo == null)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Portal] Relocation rejected: Companion={companionId}, Reason=ZDO not found");
                return;
            }

            if (!companionZdo.GetBool(HiredKey, false))
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Portal] Relocation rejected: Companion={companionId}, Reason=Not hired");
                return;
            }

            var hiredBy = companionZdo.GetLong(OwnerKey, 0L);

            if (hiredBy == 0L)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Portal] Relocation rejected: Companion={companionId}, Reason=Missing hiring player");
                return;
            }

            var sourceDistance = Vector3.Distance(
                companionZdo.GetPosition(), sourcePosition);

            if (sourceDistance > MaximumSourceDistance)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Portal] Relocation rejected: Companion={companionId}, Reason=Source position mismatch, Distance={sourceDistance:F2}");
                return;
            }

            if (Vector3.Distance(sourcePosition, destination) < MinimumTravelDistance)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Portal] Relocation rejected: Companion={companionId}, Reason=Travel distance too short");
                return;
            }

            var playerPosition = playerZdo.GetPosition();
            var playerDistance = Vector3.Distance(playerPosition, destination);

            if (playerDistance > MaximumDestinationDistance)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Portal] Player position not synchronized: Companion={companionId}, ServerPosition={playerPosition}, RequestedDestination={destination}, Distance={playerDistance:F2}");
            }

            var loadedObject = ZNetScene.instance.FindInstance(companionId);

            if (loadedObject)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Portal] Relocation deferred: Companion={companionId}, Reason=Still loaded on server");
                return;
            }

            var previousPosition = companionZdo.GetPosition();

            companionZdo.SetOwner(ZDOMan.GetSessionID());
            companionZdo.SetPosition(destination);
            ZDOMan.instance.ForceSendZDO(companionId);

            Plugin.Log.LogInfo(
                $"[Dvergr Portal] Relocated: Companion={companionId}, From={previousPosition}, To={destination}, HiredBy={hiredBy}");
        }
    }
}
