using HexDvergrAllies.Core;
using HexDvergrAllies.Core.Networking;
using HexDvergrAllies.Core.Monoscripts;
using UnityEngine;
using CompanionMode = HexDvergrAllies.Core.Companions.DvergrCompanionMode;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionUnhireManager
    {
        internal static void RequestRemoteUnhire(Player player)
        {
            if (!player || !DvergrCompanionManager.TryGetHiredCompanionId(player, out var companionId))
            {
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance), DvergrRpcManager.RemoteUnhireRpc, player.GetZDOID(), companionId);
        }

        internal static void RequestUnhire(Player player, Character character)
        {
            if (!player || !character)
            {
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    HexLog.LogWarning($"[Dvergr Companion] Unhire request rejected: Invalid ZNetView for Character={character.name}");
                }

                return;
            }

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                HexLog.LogInfo($"[Dvergr Companion] Sending unhire RPC: DvergrZDO={nview.GetZDO().m_uid}, PlayerZDO={player.GetZDOID()}");
            }

            nview.InvokeRPC(DvergrRpcManager.UnhireRpc, player.GetZDOID());
        }

        internal static void Unhire(Character character, long sender, ZDOID playerId)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var zdo = nview.GetZDO();

            if (!zdo.GetBool(DvergrCompanionState.HiredKey, false))
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    HexLog.LogInfo($"[Dvergr Companion] Unhire request ignored: Dvergr={character.name}, Reason=Not hired");
                }

                return;
            }

            var playerZdo = ZDOMan.instance.GetZDO(playerId);

            if (playerZdo == null || playerZdo.GetOwner() != sender)
            {
                return;
            }

            var player = DvergrCompanionManager.ResolvePlayer(playerId);

            if (!player)
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    HexLog.LogInfo($"[Dvergr Companion] Unhire request ignored: Dvergr={character.name}, PlayerZDO={playerId}, Reason=Player missing or out of range");
                }

                return;
            }

            var ownerPlayerId = zdo.GetLong(DvergrCompanionState.OwnerKey, 0L);

            if (ownerPlayerId != player.GetPlayerID())
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    HexLog.LogInfo($"[Dvergr Companion] Unhire request ignored: Dvergr={character.name}, Player={player.GetPlayerName()}, PlayerID={player.GetPlayerID()}, OwnerPlayerID={ownerPlayerId}, Reason=Not owner");
                }

                return;
            }

            CleanupUnhire(character, zdo, ownerPlayerId);
        }

        internal static void RPC_RemoteUnhire(long sender, ZDOID playerId, ZDOID companionId)
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

            if (!companionZdo.GetBool(DvergrCompanionState.HasHomeKeyBeenSaved, false))
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    HexLog.LogWarning($"[Dvergr Companion] Remote unhire rejected: ZDO={companionId}, Reason=Original home missing");
                }

                return;
            }

            if (companionZdo.GetBool(DvergrCompanionState.PendingUnhireKey, false))
            {
                return;
            }

            companionZdo.Set(DvergrCompanionState.PendingUnhireKey, true);

            if (companionZdo.HasOwner())
            {
                if (!companionZdo.IsOwner())
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC(companionZdo.GetOwner(), DvergrRpcManager.ProcessRemoteUnhireRpc, companionId);
                    return;
                }

                var instance = ZNetScene.instance.FindInstance(companionId);

                if (instance)
                {
                    ProcessPendingUnhire(instance.GetComponent<Character>());
                    return;
                }
            }

            CleanupUnloadedUnhire(companionZdo, verifiedPlayerId);
        }

        internal static void RPC_ProcessRemoteUnhire(long sender, ZDOID companionId)
        {
            if (sender != DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance))
            {
                return;
            }

            var instance = ZNetScene.instance.FindInstance(companionId);

            if (!instance)
            {
                var zdo = ZDOMan.instance.GetZDO(companionId);

                if (zdo == null || !zdo.IsOwner() || !zdo.GetBool(DvergrCompanionState.PendingUnhireKey, false))
                {
                    return;
                }

                var ownerPlayerId = zdo.GetLong(DvergrCompanionState.OwnerKey, 0L);

                if (!zdo.GetBool(DvergrCompanionState.HiredKey, false) || ownerPlayerId == 0L)
                {
                    return;
                }

                ZRoutedRpc.instance.InvokeRoutedRPC(DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance), DvergrRpcManager.RemoteUnhireUnloadedRpc, companionId);

                return;
            }

            var character = instance.GetComponent<Character>();
            var nview = instance.GetComponent<ZNetView>();

            if (!character || !nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            nview.GetZDO().Set(DvergrCompanionState.PendingUnhireKey, true);
            ProcessPendingUnhire(character);

            if (!nview.GetZDO().GetBool(DvergrCompanionState.HiredKey, false))
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance), DvergrRpcManager.RemoteUnhireCompletedRpc, companionId);
            }
        }

        internal static void RPC_RemoteUnhireUnloaded(long sender, ZDOID companionId)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            var zdo = ZDOMan.instance.GetZDO(companionId);

            if (zdo == null || zdo.GetOwner() != sender || !zdo.GetBool(DvergrCompanionState.PendingUnhireKey, false))
            {
                return;
            }

            var ownerPlayerId = zdo.GetLong(DvergrCompanionState.OwnerKey, 0L);

            if (!zdo.GetBool(DvergrCompanionState.HiredKey, false) || ownerPlayerId == 0L)
            {
                return;
            }

            CleanupUnloadedUnhire(zdo, ownerPlayerId);
        }

        internal static void RPC_RemoteUnhireCompleted(long sender, ZDOID companionId)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            var zdo = ZDOMan.instance.GetZDO(companionId);

            if (zdo == null || zdo.GetOwner() != sender || !zdo.GetBool(DvergrCompanionState.PendingUnhireKey, false))
            {
                return;
            }

            var ownerPlayerId = zdo.GetLong(DvergrCompanionState.OwnerKey, 0L);

            if (!zdo.GetBool(DvergrCompanionState.HiredKey, false) || ownerPlayerId == 0L)
            {
                return;
            }

            zdo.Set(DvergrCompanionState.ModeKey, (int)CompanionMode.Companion);
            zdo.Set(DvergrCompanionState.HiredKey, false);
            zdo.Set(DvergrCompanionState.OwnerKey, 0L);
            zdo.Set(DvergrCompanionState.PendingModeTeleportKey, false);
            zdo.Set(DvergrCompanionState.PendingUnhireKey, false);
            zdo.SetPosition(zdo.GetVec3(DvergrCompanionState.HomeKey, zdo.GetPosition()));
            DvergrCompanionManager.RemoveHireCache(ownerPlayerId, zdo.m_uid);

        }

        internal static void CleanupUnhire(Character character, ZDO zdo, long ownerPlayerId)
        {
            var monsterAI = character.GetComponent<MonsterAI>();

            if (!monsterAI)
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    HexLog.LogWarning($"[Dvergr Companion] Unhire rejected: Dvergr={character.name}, Reason=MonsterAI missing");
                }

                return;
            }

            if (!zdo.GetBool(DvergrCompanionState.HasHomeKeyBeenSaved, false))
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    HexLog.LogWarning($"[Dvergr Companion] Unhire rejected: Dvergr={character.name}, Reason=Home not saved");
                }

                return;
            }

            var homePosition = zdo.GetVec3(DvergrCompanionState.HomeKey, character.transform.position);

            monsterAI.SetFollowTarget(null);
            monsterAI.ResetPatrolPoint();

            DvergrCompanionBehaviorManager.ResetBehavior(character, monsterAI);

            zdo.Set(DvergrCompanionState.ModeKey, (int)CompanionMode.Companion);
            zdo.Set(DvergrCompanionState.PendingModeTeleportKey, false);
            zdo.Set(DvergrCompanionState.HiredKey, false);
            zdo.Set(DvergrCompanionState.OwnerKey, 0L);

            RemoveCompanion(character);

            DvergrCompanionManager.RemoveHireCache(ownerPlayerId, zdo.m_uid, true);

            ReturnHome(character, zdo, homePosition);

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                HexLog.LogInfo($"[Dvergr Companion] Unhire cleanup completed: Dvergr={character.name}, OwnerPlayerID={ownerPlayerId}, Home={homePosition}");
            }
        }

        private static void CleanupUnloadedUnhire(ZDO zdo, long ownerPlayerId)
        {
            if (!zdo.GetBool(DvergrCompanionState.HasHomeKeyBeenSaved, false))
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    HexLog.LogWarning($"[Dvergr Companion] Remote unhire rejected: ZDO={zdo.m_uid}, Reason=Original home missing");
                }

                return;
            }

            var homePosition = zdo.GetVec3(DvergrCompanionState.HomeKey, zdo.GetPosition());

            zdo.Set(DvergrCompanionState.ModeKey, (int)CompanionMode.Companion);
            zdo.Set(DvergrCompanionState.HiredKey, false);
            zdo.Set(DvergrCompanionState.OwnerKey, 0L);
            zdo.Set(DvergrCompanionState.PendingModeTeleportKey, false);
            zdo.Set(DvergrCompanionState.PendingUnhireKey, false);
            zdo.SetPosition(homePosition);
            DvergrCompanionManager.RemoveHireCache(ownerPlayerId, zdo.m_uid);

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                HexLog.LogInfo($"[Dvergr Companion] Remote unhire completed: ZDO={zdo.m_uid}, PlayerID={ownerPlayerId}, Home={homePosition}");
            }
        }

        internal static void ProcessPendingUnhire(Character character)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var zdo = nview.GetZDO();

            if (!zdo.GetBool(DvergrCompanionState.PendingUnhireKey, false))
            {
                return;
            }

            var ownerPlayerId = zdo.GetLong(DvergrCompanionState.OwnerKey, 0L);

            if (!zdo.GetBool(DvergrCompanionState.HiredKey, false) || ownerPlayerId == 0L)
            {
                zdo.Set(DvergrCompanionState.PendingUnhireKey, false);
                return;
            }

            CleanupUnhire(character, zdo, ownerPlayerId);

            if (!zdo.GetBool(DvergrCompanionState.HiredKey, false))
            {
                zdo.Set(DvergrCompanionState.PendingModeTeleportKey, false);
                zdo.Set(DvergrCompanionState.PendingUnhireKey, false);
            }
        }

        private static void ReturnHome(Character character, ZDO zdo, Vector3 homePosition)
        {
            Vector3 previousPosition = character.transform.position;
            float distance = Vector3.Distance(previousPosition, homePosition);

            Rigidbody rigidbody = character.GetComponent<Rigidbody>();

            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.position = homePosition;
            }

            character.transform.position = homePosition;
            zdo.SetPosition(homePosition);

            HexLog.LogInfo($"[Dvergr Companion] Returned home: Dvergr={character.name}, ZDO={zdo.m_uid}, From={previousPosition}, To={homePosition}, Distance={distance:F2}");
        }

        private static void RemoveCompanion(Character character)
        {
            if (character.TryGetComponent<DvergrCompanionComponent>(out var companion))
            {
                UnityEngine.Object.Destroy(companion);
            }
        }
    }
}
