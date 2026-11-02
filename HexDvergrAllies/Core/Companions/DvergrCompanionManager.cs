using HarmonyLib;
using System;
using HexDvergrAllies.Core.Monoscripts;
using HexDvergrAllies.Core.UI;
using System.Collections.Generic;
using UnityEngine;
using CompanionMode = HexDvergrAllies.Core.Companions.DvergrCompanionMode;
using HexDvergrAllies.Core.Networking;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionManager
    {
        private const string HiredKey = DvergrCompanionState.HiredKey;
        private const string OwnerKey = DvergrCompanionState.OwnerKey;
        private const string HomeKey = DvergrCompanionState.HomeKey;
        private const string HomeSavedKey = DvergrCompanionState.HasHomeKeyBeenSaved;
        private const string NameKey = DvergrCompanionState.NameKey;
        private const string ModeKey = DvergrCompanionState.ModeKey;
        private const string PendingModeTeleportKey = DvergrCompanionState.PendingModeTeleportKey;
        private const string PendingUnhireKey = DvergrCompanionState.PendingUnhireKey;
        private const string RecallPositionKey = DvergrCompanionState.RecallPositionKey;

        private const int MaxNameLength = 24;
        private const float FollowLogInterval = 5f;
        private const float FollowOwnerGracePeriod = 10f;

        private static readonly Func<ZRoutedRpc, long> GetServerPeerID = AccessTools.MethodDelegate<Func<ZRoutedRpc, long>>(AccessTools.Method(typeof(ZRoutedRpc), "GetServerPeerID"));

        private static readonly Dictionary<long, ZDOID> _hiredCompanions = new Dictionary<long, ZDOID>();

        private static readonly Dictionary<ZDOID, float> _lastFollowLogTimes = new Dictionary<ZDOID, float>();

        private static readonly Dictionary<ZDOID, float> _followOwnerMissingSince = new Dictionary<ZDOID, float>();

        #region Internal
        internal static void Initialize(Character character)
        {
            if (character.GetFaction() != Character.Faction.Dverger)
            {
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                Plugin.Log.LogWarning($"[Dvergr Companion] Initialize failed: Dvergr={character.name}, Reason=Invalid ZNetView");
                return;
            }

            DvergrRpcManager.Register(nview, character);

            var zdo = nview.GetZDO();

            if (DvergrCompanionState.IsUnhirePending(zdo) && nview.IsOwner())
            {
                ProcessPendingUnhire(character);
            }

            if (!DvergrCompanionState.IsHired(zdo) && nview.IsOwner())
            {
                DvergrCompanionBehaviorManager.ResetBehavior(character, character.GetComponent<MonsterAI>());
            }

            if (DvergrCompanionState.IsHired(zdo))
            {
                InitializeCompanion(character);

                var ownerPlayerId = DvergrCompanionState.GetOwnerPlayerId(zdo);

                if (ownerPlayerId != 0L)
                {
                    if (!_hiredCompanions.TryGetValue(ownerPlayerId, out var existingCompanion))
                    {
                        _hiredCompanions[ownerPlayerId] = zdo.m_uid;

                        Plugin.Log.LogInfo($"[Dvergr Companion] Existing hire cached: PlayerID={ownerPlayerId}, DvergrZDO={zdo.m_uid}");
                    }
                    else if (existingCompanion != zdo.m_uid)
                    {
                        Plugin.Log.LogWarning($"[Dvergr Companion] Multiple existing hires detected: PlayerID={ownerPlayerId}, Cached={existingCompanion}, Additional={zdo.m_uid}");
                    }
                }
            }

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo($"[Dvergr Companion] Initialized: Dvergr={character.name}, ZDO={zdo.m_uid}, IsOwner={nview.IsOwner()}, IsHired={DvergrCompanionState.IsHired(zdo)}");
            }
        }

        internal static CompanionMode GetMode(Character character)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                return CompanionMode.Companion;
            }

            return DvergrCompanionState.GetMode(nview.GetZDO());
        }

        internal static void RequestToggleMode(Player player, Character character)
        {
            if (!IsHiredByPlayer(character, player))
            {
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                return;
            }

            nview.InvokeRPC(DvergrRpcManager.SetModeRpc, player.GetZDOID());
        }

        internal static bool IsHiredByPlayer(Character character, Player player)
        {
            if (!character || !player)
            {
                return false;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                return false;
            }

            var zdo = nview.GetZDO();

            return DvergrCompanionState.IsHired(zdo) && DvergrCompanionState.GetOwnerPlayerId(zdo) == player.GetPlayerID();
        }

        internal static bool HasHiredCompanion(Player player)
        {
            if (!player)
            {
                return false;
            }

            ZDOID companionId;

            return TryGetHiredCompanion(player.GetPlayerID(), out companionId);
        }

        internal static bool TryGetHiredCompanionId(Player player, out ZDOID companionId)
        {
            companionId = ZDOID.None;

            return player && TryGetHiredCompanion(player.GetPlayerID(), out companionId);
        }

        internal static void RequestUnhire(Player player, Character character)
        {
            if (!player || !character)
            {
                return;
            }

            ZNetView nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                Plugin.Log.LogWarning("[Dvergr Companion] Unhire request rejected: Reason=Invalid ZNetView");
                return;
            }

            Plugin.Log.LogInfo($"[Dvergr Companion] Sending unhire RPC: DvergrZDO={nview.GetZDO().m_uid}, PlayerZDO={player.GetZDOID()}");

            nview.InvokeRPC(DvergrRpcManager.UnhireRpc, player.GetZDOID());
        }

        internal static void ResetSessionState()
        {
            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo($"[Dvergr Companion] Session state reset: CachedHires={_hiredCompanions.Count}, FollowLogs={_lastFollowLogTimes.Count}, MissingOwners={_followOwnerMissingSince.Count}");
            }

            _hiredCompanions.Clear();
            _lastFollowLogTimes.Clear();
            _followOwnerMissingSince.Clear();
        }

        internal static string GetCompanionName(Character character)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                return Localization.instance.Localize(character.m_name);
            }

            var name = nview.GetZDO().GetString(NameKey, "");

            return string.IsNullOrEmpty(name) ? Localization.instance.Localize(character.m_name) : name;
        }

        internal static void RequestRename(Player player, Character character, string name)
        {
            if (!player || !character || !IsHiredByPlayer(character, player))
            {
                return;
            }

            name = name.Trim();

            if (name.Length == 0 || name.Length > MaxNameLength || name.Contains("<") || name.Contains(">"))
            {
                player.Message(MessageHud.MessageType.Center, "Invalid companion name.");
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                return;
            }

            nview.InvokeRPC(DvergrRpcManager.RenameRpc, player.GetZDOID(), name);
        }

        internal static void UpdateFollow(MonsterAI monsterAI)
        {
            Character character = monsterAI.GetComponent<Character>();

            if (!character)
            {
                return;
            }

            ZNetView nview = monsterAI.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            ZDO zdo = nview.GetZDO();

            if (zdo.GetBool(PendingUnhireKey, false))
            {
                ProcessPendingUnhire(character);
                return;
            }

            if (!zdo.GetBool(HiredKey, false))
            {
                _followOwnerMissingSince.Remove(zdo.m_uid);
                return;
            }

            if (GetMode(character) == CompanionMode.Companion && zdo.GetBool(PendingModeTeleportKey, false))
            {
                foreach (var player in Player.GetAllPlayers())
                {
                    if (!player || player.GetPlayerID() != zdo.GetLong(OwnerKey, 0L))
                    {
                        continue;
                    }

                    var destination = player.transform.position + player.transform.right * 2f;
                    character.transform.position = destination;
                    zdo.SetPosition(destination);
                    zdo.Set(PendingModeTeleportKey, false);
                    break;
                }
            }

            if (GetMode(character) == CompanionMode.Sentry)
            {
                if (monsterAI.GetFollowTarget())
                {
                    monsterAI.SetFollowTarget(null);
                }

                _followOwnerMissingSince.Remove(zdo.m_uid);
                return;
            }

            long playerId = zdo.GetLong(OwnerKey, 0L);
            GameObject followTarget = monsterAI.GetFollowTarget();

            foreach (Player player in Player.GetAllPlayers())
            {
                if (!player || player.GetPlayerID() != playerId)
                {
                    continue;
                }

                float missingSince;

                if (_followOwnerMissingSince.TryGetValue(zdo.m_uid, out missingSince))
                {
                    if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                    {
                        Plugin.Log.LogInfo($"[Dvergr Companion] Follow owner resolved: Dvergr={character.name}, OwnerPlayerID={playerId}, Waited={Time.time - missingSince:F2}s");
                    }

                    _followOwnerMissingSince.Remove(zdo.m_uid);
                }

                if (followTarget != player.gameObject)
                {
                    monsterAI.ResetPatrolPoint();
                    monsterAI.SetFollowTarget(player.gameObject);

                    if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                    {
                        Plugin.Log.LogInfo($"[Dvergr Companion] Follow target assigned: Dvergr={character.name}, Player={player.GetPlayerName()}");
                    }
                }

                if (ShouldLogFollow(zdo.m_uid))
                {
                    float distance = Vector3.Distance(monsterAI.transform.position, player.transform.position);

                    Plugin.Log.LogInfo($"[Dvergr Companion] Follow status: Dvergr={character.name}, Player={player.GetPlayerName()}, Distance={distance:F2}");
                }

                return;
            }

            float firstMissingTime;

            if (!_followOwnerMissingSince.TryGetValue(zdo.m_uid, out firstMissingTime))
            {
                firstMissingTime = Time.time;
                _followOwnerMissingSince[zdo.m_uid] = firstMissingTime;
            }

            float missingDuration = Time.time - firstMissingTime;

            if (missingDuration < FollowOwnerGracePeriod)
            {
                return;
            }

            Player localPlayer = Player.m_localPlayer;
            bool isLocalOwner = localPlayer && localPlayer.GetPlayerID() == playerId;

            if (isLocalOwner)
            {
                if (ShouldLogFollow(zdo.m_uid))
                {
                    Plugin.Log.LogWarning($"[Dvergr Companion] Local follow owner not found: Dvergr={character.name}, ZDO={zdo.m_uid}, OwnerPlayerID={playerId}, LocalPlayerID={localPlayer.GetPlayerID()}, MissingFor={missingDuration:F2}s");
                }
            }
            else if (ShouldLogFollow(zdo.m_uid))
            {
                Plugin.Log.LogInfo($"[Dvergr Companion] Remote follow owner unavailable: Dvergr={character.name}, ZDO={zdo.m_uid}, OwnerPlayerID={playerId}, LocalPlayerID={(localPlayer ? localPlayer.GetPlayerID() : 0L)}, MissingFor={missingDuration:F2}s");
            }

            if (followTarget)
            {
                monsterAI.SetFollowTarget(null);
            }
        }

        internal static void OnCompanionDeath(Character character)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var zdo = nview.GetZDO();

            if (!zdo.GetBool(HiredKey, false))
            {
                return;
            }

            var ownerPlayerId = zdo.GetLong(OwnerKey, 0L);

            zdo.Set(HiredKey, false);
            zdo.Set(OwnerKey, 0L);

            if (_hiredCompanions.TryGetValue(ownerPlayerId, out var companionId) && companionId == zdo.m_uid)
            {
                _hiredCompanions.Remove(ownerPlayerId);
            }

            _lastFollowLogTimes.Remove(zdo.m_uid);
            _followOwnerMissingSince.Remove(zdo.m_uid);

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo($"[Dvergr Companion] Companion died: PlayerID={ownerPlayerId}, DvergrZDO={zdo.m_uid}, Hire released");
            }
        }

        internal static bool TryGetHiredCompanionZDO(Player player, out ZDO companionZdo)
        {
            companionZdo = null;

            if (!TryGetHiredCompanionId(player, out var companionId))
            {
                return false;
            }

            companionZdo = ZDOMan.instance.GetZDO(companionId);
            return companionZdo != null;
        }

        internal static void RequestRemoteToggleMode(Player player)
        {
            if (!player || !TryGetHiredCompanionId(player, out var companionId))
            {
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerID(ZRoutedRpc.instance), DvergrRpcManager.RemoteSetModeRpc, player.GetZDOID(), companionId);
        }

        internal static void RequestRemoteRecall(Player player)
        {
            if (!player || !TryGetHiredCompanionId(player, out var companionId))
            {
                return;
            }

            var destination = player.transform.position + player.transform.right * 2f;

            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerID(ZRoutedRpc.instance), DvergrRpcManager.RemoteRecallRpc, player.GetZDOID(), companionId, destination);
        }

        internal static void RequestRemoteRename(Player player, string name)
        {
            if (!player || !TryGetHiredCompanionId(player, out var companionId))
            {
                return;
            }

            name = name.Trim();

            if (name.Length == 0 || name.Length > MaxNameLength || name.Contains("<") || name.Contains(">"))
            {
                player.Message(MessageHud.MessageType.Center, "Invalid companion name.");
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerID(ZRoutedRpc.instance), DvergrRpcManager.RemoteRenameRpc, player.GetZDOID(), companionId, name);
        }

        internal static void RequestRemoteUnhire(Player player)
        {
            if (!player || !TryGetHiredCompanionId(player, out var companionId))
            {
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerID(ZRoutedRpc.instance), DvergrRpcManager.RemoteUnhireRpc, player.GetZDOID(), companionId);
        }

        internal static Player ResolvePlayer(ZDOID playerId)
        {
            if (ZNetScene.instance == null)
            {
                return null;
            }

            GameObject playerObject = ZNetScene.instance.FindInstance(playerId);

            return playerObject
                ? playerObject.GetComponent<Player>()
                : null;
        }

        internal static bool TryGetHiredCompanion(long playerId, out ZDOID companionId)
        {
            if (!_hiredCompanions.TryGetValue(playerId, out companionId))
            {
                return false;
            }

            if (ZDOMan.instance == null)
            {
                return true;
            }

            ZDO companionZdo = ZDOMan.instance.GetZDO(companionId);

            if (companionZdo == null)
            {
                // Do not clear the cache merely because the
                // companion's zone is currently unloaded.
                return true;
            }

            bool isHired = companionZdo.GetBool(HiredKey, false);
            long ownerPlayerId = companionZdo.GetLong(OwnerKey, 0L);

            if (isHired && ownerPlayerId == playerId)
            {
                return true;
            }

            _hiredCompanions.Remove(playerId);

            Plugin.Log.LogInfo($"[Dvergr Companion] Stale hire cache removed: PlayerID={playerId}, DvergrZDO={companionId}");

            return false;
        }

        internal static void InitializeCompanion(Character character)
        {
            var companion = character.GetComponent<DvergrCompanionComponent>();

            if (!companion)
            {
                companion = character.gameObject.AddComponent<DvergrCompanionComponent>();
            }

            companion.Initialize(character);
        }

        internal static void CacheHiredCompanion(long ownerPlayerId, ZDOID companionId)
        {
            _hiredCompanions[ownerPlayerId] = companionId;
        }

        internal static void Rename(Character character, long sender, ZDOID playerId, string name)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var player = ResolvePlayer(playerId);

            if (!player || !IsHiredByPlayer(character, player) || !DvergrInteractionHelper.IsWithinRange(player, character))
            {
                return;
            }

            name = name.Trim();

            if (name.Length == 0 || name.Length > MaxNameLength || name.Contains("<") || name.Contains(">"))
            {
                return;
            }

            nview.GetZDO().Set(NameKey, name);

            Plugin.Log.LogInfo($"[Dvergr Companion] Renamed: Dvergr={character.name}, Name={name}, Player={player.GetPlayerName()}");
        }

        internal static void SetMode(Character character, long sender, ZDOID playerId)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var player = ResolvePlayer(playerId);

            if (!player || !IsHiredByPlayer(character, player))
            {
                return;
            }

            var playerZdo = player.GetComponent<ZNetView>().GetZDO();

            if (playerZdo.GetOwner() != sender)
            {
                return;
            }

            var mode = GetMode(character) == CompanionMode.Companion ? CompanionMode.Sentry : CompanionMode.Companion;
            var zdo = nview.GetZDO();

            if (mode == CompanionMode.Sentry)
            {
                DvergrCompanionSentryMode.Activate(character);
            }
            else
            {
                zdo.Set(RecallPositionKey, player.transform.position + player.transform.right * 2f);
                zdo.Set(PendingModeTeleportKey, true);
            }

            zdo.Set(ModeKey, (int)mode);

            Plugin.Log.LogInfo($"[Dvergr Companion] Mode changed: Dvergr={character.name}, Player={player.GetPlayerName()}, Mode={mode}");
        }

        internal static void Unhire(Character character, long sender, ZDOID playerId)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var zdo = nview.GetZDO();

            if (!zdo.GetBool(HiredKey, false))
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogInfo($"[Dvergr Companion] Unhire request ignored: Dvergr={character.name}, Reason=Not hired");
                }

                return;
            }

            var playerZdo = ZDOMan.instance.GetZDO(playerId);

            if (playerZdo == null || playerZdo.GetOwner() != sender)
            {
                return;
            }

            var player = ResolvePlayer(playerId);

            if (!player)
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogInfo($"[Dvergr Companion] Unhire request ignored: Dvergr={character.name}, PlayerZDO={playerId}, Reason=Player missing or out of range");
                }

                return;
            }

            var ownerPlayerId = zdo.GetLong(OwnerKey, 0L);

            if (ownerPlayerId != player.GetPlayerID())
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogInfo($"[Dvergr Companion] Unhire request ignored: Dvergr={character.name}, Player={player.GetPlayerName()}, PlayerID={player.GetPlayerID()}, OwnerPlayerID={ownerPlayerId}, Reason=Not owner");
                }

                return;
            }

            CleanupUnhire(character, zdo, ownerPlayerId);
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

            if (verifiedPlayerId == 0L || !companionZdo.GetBool(HiredKey, false) || companionZdo.GetLong(OwnerKey, 0L) != verifiedPlayerId)
            {
                return;
            }

            if (Vector3.Distance(playerZdo.GetPosition(), destination) > 10f)
            {
                return;
            }

            companionZdo.Set(ModeKey, (int)CompanionMode.Companion);
            companionZdo.Set(RecallPositionKey, destination);
            companionZdo.Set(PendingModeTeleportKey, true);
        }

        internal static void RPC_RemoteSetMode(long sender, ZDOID playerId, ZDOID companionId)
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

            if (verifiedPlayerId == 0L || !companionZdo.GetBool(HiredKey, false) || companionZdo.GetLong(OwnerKey, 0L) != verifiedPlayerId)
            {
                return;
            }

            var mode = companionZdo.GetInt(ModeKey, 0) == (int)CompanionMode.Companion ? CompanionMode.Sentry : CompanionMode.Companion;

            if (mode == CompanionMode.Sentry)
            {
                companionZdo.Set("HexDvergr_SentryPosition", companionZdo.GetPosition());
            }
            else
            {
                companionZdo.Set(RecallPositionKey, playerZdo.GetPosition() + Vector3.right * 2f);
                companionZdo.Set(PendingModeTeleportKey, true);
            }

            companionZdo.Set(ModeKey, (int)mode);
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

            if (verifiedPlayerId == 0L || !companionZdo.GetBool(HiredKey, false) || companionZdo.GetLong(OwnerKey, 0L) != verifiedPlayerId)
            {
                return;
            }

            if (!companionZdo.GetBool(HomeSavedKey, false))
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogWarning($"[Dvergr Companion] Remote unhire rejected: ZDO={companionId}, Reason=Original home missing");
                }

                return;
            }

            if (companionZdo.GetBool(PendingUnhireKey, false))
            {
                return;
            }

            companionZdo.Set(PendingUnhireKey, true);

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

        internal static void RPC_RemoteRename(long sender, ZDOID playerId, ZDOID companionId, string name)
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

            if (verifiedPlayerId == 0L || !companionZdo.GetBool(HiredKey, false) || companionZdo.GetLong(OwnerKey, 0L) != verifiedPlayerId)
            {
                return;
            }

            name = name.Trim();

            if (name.Length == 0 || name.Length > MaxNameLength || name.Contains("<") || name.Contains(">"))
            {
                return;
            }

            companionZdo.Set(NameKey, name);
        }

        internal static void RPC_ProcessRemoteUnhire(long sender, ZDOID companionId)
        {
            if (sender != GetServerPeerID(ZRoutedRpc.instance))
            {
                return;
            }

            var instance = ZNetScene.instance.FindInstance(companionId);

            if (!instance)
            {
                var zdo = ZDOMan.instance.GetZDO(companionId);

                if (zdo == null || !zdo.IsOwner() || !zdo.GetBool(PendingUnhireKey, false))
                {
                    return;
                }

                var ownerPlayerId = zdo.GetLong(OwnerKey, 0L);

                if (!zdo.GetBool(HiredKey, false) || ownerPlayerId == 0L)
                {
                    return;
                }

                ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerID(ZRoutedRpc.instance), DvergrRpcManager.RemoteUnhireUnloadedRpc, companionId);

                return;
            }

            var character = instance.GetComponent<Character>();
            var nview = instance.GetComponent<ZNetView>();

            if (!character || !nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            nview.GetZDO().Set(PendingUnhireKey, true);
            ProcessPendingUnhire(character);

            if (!nview.GetZDO().GetBool(HiredKey, false))
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerID(ZRoutedRpc.instance), DvergrRpcManager.RemoteUnhireCompletedRpc, companionId);
            }
        }

        internal static void RPC_RemoteUnhireUnloaded(long sender, ZDOID companionId)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            var zdo = ZDOMan.instance.GetZDO(companionId);

            if (zdo == null || zdo.GetOwner() != sender || !zdo.GetBool(PendingUnhireKey, false))
            {
                return;
            }

            var ownerPlayerId = zdo.GetLong(OwnerKey, 0L);

            if (!zdo.GetBool(HiredKey, false) || ownerPlayerId == 0L)
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

            if (zdo == null || zdo.GetOwner() != sender || !zdo.GetBool(PendingUnhireKey, false))
            {
                return;
            }

            var ownerPlayerId = zdo.GetLong(OwnerKey, 0L);

            if (!zdo.GetBool(HiredKey, false) || ownerPlayerId == 0L)
            {
                return;
            }

            zdo.Set(ModeKey, (int)CompanionMode.Companion);
            zdo.Set(HiredKey, false);
            zdo.Set(OwnerKey, 0L);
            zdo.Set(PendingModeTeleportKey, false);
            zdo.Set(PendingUnhireKey, false);
            zdo.SetPosition(zdo.GetVec3(HomeKey, zdo.GetPosition()));

            if (_hiredCompanions.TryGetValue(ownerPlayerId, out var cachedCompanion) && cachedCompanion == zdo.m_uid)
            {
                _hiredCompanions.Remove(ownerPlayerId);
            }

            _lastFollowLogTimes.Remove(zdo.m_uid);
            _followOwnerMissingSince.Remove(zdo.m_uid);
        }
        #endregion

        #region Private
        private static void CleanupUnhire(Character character, ZDO zdo, long ownerPlayerId)
        {
            var monsterAI = character.GetComponent<MonsterAI>();

            if (!monsterAI)
            {
                if(HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogWarning($"[Dvergr Companion] Unhire rejected: Dvergr={character.name}, Reason=MonsterAI missing");
                }
                
                return;
            }

            if (!zdo.GetBool(HomeSavedKey, false))
            {
                if(HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogWarning($"[Dvergr Companion] Unhire rejected: Dvergr={character.name}, Reason=Home not saved");
                }
                
                return;
            }

            var homePosition = zdo.GetVec3(HomeKey, character.transform.position);

            monsterAI.SetFollowTarget(null);
            monsterAI.ResetPatrolPoint();

            DvergrCompanionBehaviorManager.ResetBehavior(character, monsterAI);

            zdo.Set(ModeKey, (int)CompanionMode.Companion);
            zdo.Set(PendingModeTeleportKey, false);
            zdo.Set(HiredKey, false);
            zdo.Set(OwnerKey, 0L);

            RemoveCompanion(character);

            if (_hiredCompanions.TryGetValue(ownerPlayerId, out var cachedCompanion) && cachedCompanion == zdo.m_uid)
            {
                _hiredCompanions.Remove(ownerPlayerId);

                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogInfo($"[Dvergr Companion] Hire cache removed: PlayerID={ownerPlayerId}, DvergrZDO={zdo.m_uid}");
                }
            }

            _lastFollowLogTimes.Remove(zdo.m_uid);
            _followOwnerMissingSince.Remove(zdo.m_uid);

            ReturnHome(character, zdo, homePosition);

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo($"[Dvergr Companion] Unhire cleanup completed: Dvergr={character.name}, OwnerPlayerID={ownerPlayerId}, Home={homePosition}");
            }
        }

        private static void CleanupUnloadedUnhire(ZDO zdo, long ownerPlayerId)
        {
            if (!zdo.GetBool(HomeSavedKey, false))
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogWarning($"[Dvergr Companion] Remote unhire rejected: ZDO={zdo.m_uid}, Reason=Original home missing");
                }

                return;
            }

            var homePosition = zdo.GetVec3(HomeKey, zdo.GetPosition());

            zdo.Set(ModeKey, (int)CompanionMode.Companion);
            zdo.Set(HiredKey, false);
            zdo.Set(OwnerKey, 0L);
            zdo.Set(PendingModeTeleportKey, false);
            zdo.Set(PendingUnhireKey, false);
            zdo.SetPosition(homePosition);

            if (_hiredCompanions.TryGetValue(ownerPlayerId, out var cachedCompanion) && cachedCompanion == zdo.m_uid)
            {
                _hiredCompanions.Remove(ownerPlayerId);
            }

            _lastFollowLogTimes.Remove(zdo.m_uid);
            _followOwnerMissingSince.Remove(zdo.m_uid);

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo($"[Dvergr Companion] Remote unhire completed: ZDO={zdo.m_uid}, PlayerID={ownerPlayerId}, Home={homePosition}");
            }
        }

        private static void ProcessPendingUnhire(Character character)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var zdo = nview.GetZDO();

            if (!zdo.GetBool(PendingUnhireKey, false))
            {
                return;
            }

            var ownerPlayerId = zdo.GetLong(OwnerKey, 0L);

            if (!zdo.GetBool(HiredKey, false) || ownerPlayerId == 0L)
            {
                zdo.Set(PendingUnhireKey, false);
                return;
            }

            CleanupUnhire(character, zdo, ownerPlayerId);

            if (!zdo.GetBool(HiredKey, false))
            {
                zdo.Set(PendingModeTeleportKey, false);
                zdo.Set(PendingUnhireKey, false);
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

            Plugin.Log.LogInfo($"[Dvergr Companion] Returned home: Dvergr={character.name}, ZDO={zdo.m_uid}, From={previousPosition}, To={homePosition}, Distance={distance:F2}");
        }

        private static bool ShouldLogFollow(ZDOID zdoId)
        {
            if (!HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                return false;
            }

            if (_lastFollowLogTimes.TryGetValue(zdoId, out var lastLogTime) && Time.time - lastLogTime < FollowLogInterval)
            {
                return false;
            }

            _lastFollowLogTimes[zdoId] = Time.time;
            return true;
        }

        private static void RemoveCompanion(Character character)
        {
            if (character.TryGetComponent<DvergrCompanionComponent>(out var companion))
            {
                UnityEngine.Object.Destroy(companion);
            }
        }
        #endregion
    }
}