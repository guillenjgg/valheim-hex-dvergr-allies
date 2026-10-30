
using System.Collections.Generic;
using UnityEngine;

namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrCompanionManager
    {
        private const string HireRpc = "HexDvergrHire";
        private const string UnhireRpc = "HexDvergrUnhire";
        private const string HiredKey = "HexDvergr_IsHired";
        private const string OwnerKey = "HexDvergr_HiredBy";
        private const string HomeKey = "HexDvergr_HomePosition";
        private const string HomeSavedKey = "HexDvergr_HomeSaved";
        private const string RenameRpc = "HexDvergrRename";
        private const string NameKey = "HexDvergr_Name";
        private const int MaxNameLength = 24;

        private const float FollowLogInterval = 5f;
        private const float FollowOwnerGracePeriod = 10f;

        private static readonly Dictionary<long, ZDOID> _hiredCompanions = new Dictionary<long, ZDOID>();

        private static readonly Dictionary<ZDOID, float> _lastFollowLogTimes = new Dictionary<ZDOID, float>();

        private static readonly Dictionary<ZDOID, float> _followOwnerMissingSince = new Dictionary<ZDOID, float>();

        internal static void Initialize(Character character)
        {
            if (character.GetFaction() != Character.Faction.Dverger)
            {
                return;
            }

            ZNetView nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                Plugin.Log.LogWarning($"[Dvergr Companion] Initialize failed: Dvergr={character.name}, Reason=Invalid ZNetView");
                return;
            }

            nview.Register<ZDOID>(HireRpc, (sender, playerId) => { Hire(character, sender, playerId); });

            nview.Register<ZDOID>(UnhireRpc, (sender, playerId) => { Unhire(character, sender, playerId); });

            nview.Register<ZDOID, string>(RenameRpc, (sender, playerId, name) => Rename(character, sender, playerId, name));

            ZDO zdo = nview.GetZDO();

            if (zdo.GetBool(HiredKey, false))
            {
                long ownerPlayerId = zdo.GetLong(OwnerKey, 0L);

                if (ownerPlayerId != 0L)
                {
                    ZDOID existingCompanion;

                    if (!_hiredCompanions.TryGetValue(ownerPlayerId, out existingCompanion))
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

            Plugin.Log.LogInfo($"[Dvergr Companion] Initialized: Dvergr={character.name}, ZDO={zdo.m_uid}, IsOwner={nview.IsOwner()}, IsHired={IsHired(character)}");
        }

        internal static bool IsHired(Character character)
        {
            if (!character)
            {
                return false;
            }

            ZNetView nview = character.GetComponent<ZNetView>();

            return nview && nview.IsValid() && nview.GetZDO().GetBool(HiredKey, false);
        }

        internal static bool IsHiredByPlayer(Character character, Player player)
        {
            if (!character || !player)
            {
                return false;
            }

            ZNetView nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                return false;
            }

            ZDO zdo = nview.GetZDO();

            return zdo.GetBool(HiredKey, false) && zdo.GetLong(OwnerKey, 0L) == player.GetPlayerID();
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

        internal static void RequestHire(Player player, Character character)
        {
            if (!player || !character)
            {
                return;
            }

            ZNetView nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                Plugin.Log.LogWarning("[Dvergr Companion] Hire request rejected: Reason=Invalid ZNetView");
                return;
            }

            ZDOID existingCompanion;

            if (TryGetHiredCompanion(player.GetPlayerID(), out existingCompanion))
            {
                Plugin.Log.LogWarning($"[Dvergr Companion] Hire request rejected: Player={player.GetPlayerName()}, ExistingCompanion={existingCompanion}, Reason=Already has companion");

                player.Message(MessageHud.MessageType.Center, "You already have a Dvergr companion.");

                return;
            }

            Plugin.Log.LogInfo($"[Dvergr Companion] Sending hire RPC: DvergrZDO={nview.GetZDO().m_uid}, PlayerZDO={player.GetZDOID()}");

            nview.InvokeRPC(HireRpc, player.GetZDOID());
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

            nview.InvokeRPC(UnhireRpc, player.GetZDOID());
        }

        internal static void ResetSessionState()
        {
            int hiredCount = _hiredCompanions.Count;
            int followLogCount = _lastFollowLogTimes.Count;
            int missingOwnerCount = _followOwnerMissingSince.Count;

            _hiredCompanions.Clear();
            _lastFollowLogTimes.Clear();
            _followOwnerMissingSince.Clear();

            Plugin.Log.LogInfo($"[Dvergr Companion] Session state reset: CachedHires={hiredCount}, FollowLogs={followLogCount}, MissingOwners={missingOwnerCount}");
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

            nview.InvokeRPC(RenameRpc, player.GetZDOID(), name);
        }

        private static void Rename(Character character, long sender, ZDOID playerId, string name)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var player = ResolvePlayer(playerId);

            if (!player || !IsHiredByPlayer(character, player) || !IsWithinInteractionDistance(player, character))
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

        private static bool TryGetHiredCompanion(long playerId, out ZDOID companionId)
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

        private static void Hire(Character character, long sender, ZDOID playerId)
        {
            ZNetView nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            Player player = ResolvePlayer(playerId);

            if (!player || !IsWithinInteractionDistance(player, character))
            {
                Plugin.Log.LogWarning($"[Dvergr Companion] Hire rejected: PlayerZDO={playerId}, Reason=Player missing or out of range");
                return;
            }

            ZDO zdo = nview.GetZDO();

            if (zdo.GetBool(HiredKey, false))
            {
                Plugin.Log.LogWarning($"[Dvergr Companion] Hire rejected: Reason=Already hired, Owner={zdo.GetLong(OwnerKey, 0L)}");
                return;
            }

            long ownerPlayerId = player.GetPlayerID();
            ZDOID existingCompanion;

            if (TryGetHiredCompanion(ownerPlayerId, out existingCompanion))
            {
                Plugin.Log.LogWarning($"[Dvergr Companion] Hire rejected: Player={player.GetPlayerName()}, PlayerID={ownerPlayerId}, ExistingCompanion={existingCompanion}, Reason=Already has companion");
                return;
            }

            MonsterAI monsterAI = character.GetComponent<MonsterAI>();

            if (!monsterAI)
            {
                Plugin.Log.LogWarning("[Dvergr Companion] Hire rejected: Reason=MonsterAI missing");
                return;
            }

            DvergrCompanionBehaviorManager.CaptureState(character, monsterAI);

            if (!zdo.GetBool(HomeSavedKey, false))
            {
                zdo.Set(HomeKey, character.transform.position);
                zdo.Set(HomeSavedKey, true);

                Plugin.Log.LogInfo($"[Dvergr Companion] Home saved: Dvergr={character.name}, Position={character.transform.position}");
            }

            zdo.Set(OwnerKey, ownerPlayerId);
            zdo.Set(HiredKey, true);

            _hiredCompanions[ownerPlayerId] = zdo.m_uid;

            Plugin.Log.LogInfo($"[Dvergr Companion] Hire cached: PlayerID={ownerPlayerId}, DvergrZDO={zdo.m_uid}");

            monsterAI.ResetPatrolPoint();
            monsterAI.SetFollowTarget(player.gameObject);

            Plugin.Log.LogInfo($"[Dvergr Companion] Hired successfully: Dvergr={character.name}, Player={player.GetPlayerName()}, PlayerID={ownerPlayerId}");
        }

        private static void Unhire(Character character, long sender, ZDOID playerId)
        {
            ZNetView nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            ZDO zdo = nview.GetZDO();

            if (!zdo.GetBool(HiredKey, false))
            {
                Plugin.Log.LogWarning($"[Dvergr Companion] Unhire rejected: Dvergr={character.name}, Reason=Not hired");
                return;
            }

            Player player = ResolvePlayer(playerId);

            if (!player || !IsWithinInteractionDistance(player, character))
            {
                Plugin.Log.LogWarning($"[Dvergr Companion] Unhire rejected: PlayerZDO={playerId}, Reason=Player missing or out of range");
                return;
            }

            long ownerPlayerId = zdo.GetLong(OwnerKey, 0L);

            if (ownerPlayerId != player.GetPlayerID())
            {
                Plugin.Log.LogWarning($"[Dvergr Companion] Unhire rejected: Player={player.GetPlayerName()}, Reason=Not hiring player");
                return;
            }

            MonsterAI monsterAI = character.GetComponent<MonsterAI>();

            if (!monsterAI)
            {
                Plugin.Log.LogWarning("[Dvergr Companion] Unhire rejected: Reason=MonsterAI missing");
                return;
            }

            if (!zdo.GetBool(HomeSavedKey, false))
            {
                Plugin.Log.LogWarning($"[Dvergr Companion] Unhire rejected: Dvergr={character.name}, Reason=Original home missing");
                return;
            }

            Vector3 homePosition = zdo.GetVec3(HomeKey, character.transform.position);

            monsterAI.SetFollowTarget(null);
            monsterAI.ResetPatrolPoint();

            DvergrCompanionBehaviorManager.ResetBehavior(character, monsterAI);

            zdo.Set(HiredKey, false);
            zdo.Set(OwnerKey, 0L);

            ZDOID cachedCompanion;

            if (_hiredCompanions.TryGetValue(ownerPlayerId, out cachedCompanion) && cachedCompanion == zdo.m_uid)
            {
                _hiredCompanions.Remove(ownerPlayerId);

                Plugin.Log.LogInfo($"[Dvergr Companion] Hire cache removed: PlayerID={ownerPlayerId}, DvergrZDO={zdo.m_uid}");
            }

            _lastFollowLogTimes.Remove(zdo.m_uid);
            _followOwnerMissingSince.Remove(zdo.m_uid);

            ReturnHome(character, zdo, homePosition);

            Plugin.Log.LogInfo($"[Dvergr Companion] Unhired successfully: Dvergr={character.name}, Player={player.GetPlayerName()}, Home={homePosition}");
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

        private static Player ResolvePlayer(ZDOID playerId)
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

        private static bool IsWithinInteractionDistance(Player player, Character character)
        {
            float distance = Vector3.Distance(player.transform.position, character.transform.position);

            return distance <= DvergrTraderManager.InteractionDistance;
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

            if (!zdo.GetBool(HiredKey, false))
            {
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
                    Plugin.Log.LogInfo($"[Dvergr Companion] Follow owner resolved: Dvergr={character.name}, OwnerPlayerID={playerId}, Waited={Time.time - missingSince:F2}s");

                    _followOwnerMissingSince.Remove(zdo.m_uid);
                }

                if (followTarget != player.gameObject)
                {
                    monsterAI.ResetPatrolPoint();
                    monsterAI.SetFollowTarget(player.gameObject);

                    Plugin.Log.LogInfo($"[Dvergr Companion] Follow target assigned: Dvergr={character.name}, Player={player.GetPlayerName()}");
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

        private static bool ShouldLogFollow(ZDOID zdoId)
        {
            float lastLogTime;

            if (_lastFollowLogTimes.TryGetValue(zdoId, out lastLogTime) && Time.time - lastLogTime < FollowLogInterval)
            {
                return false;
            }

            _lastFollowLogTimes[zdoId] = Time.time;
            return true;
        }
    }
}
