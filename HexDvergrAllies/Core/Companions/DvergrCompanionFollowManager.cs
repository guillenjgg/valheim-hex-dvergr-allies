using System.Collections.Generic;
using UnityEngine;
using CompanionMode = HexDvergrAllies.Core.Companions.DvergrCompanionMode;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionFollowManager
    {
        private const float FollowLogInterval = 5f;
        private const float FollowOwnerGracePeriod = 10f;

        private static readonly Dictionary<ZDOID, float> _lastFollowLogTimes = new Dictionary<ZDOID, float>();
        private static readonly Dictionary<ZDOID, float> _followOwnerMissingSince = new Dictionary<ZDOID, float>();

        internal static int FollowLogCount => _lastFollowLogTimes.Count;
        internal static int MissingOwnerCount => _followOwnerMissingSince.Count;

        internal static void UpdateFollow(MonsterAI monsterAI)
        {
            var character = monsterAI.GetComponent<Character>();

            if (!character)
            {
                return;
            }

            var nview = monsterAI.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var zdo = nview.GetZDO();

            if (zdo.GetBool(DvergrCompanionState.PendingUnhireKey, false))
            {
                DvergrCompanionUnhireManager.ProcessPendingUnhire(character);
                return;
            }

            if (!zdo.GetBool(DvergrCompanionState.HiredKey, false))
            {
                _followOwnerMissingSince.Remove(zdo.m_uid);
                return;
            }

            if (DvergrCompanionModeManager.GetMode(character) == CompanionMode.Companion && zdo.GetBool(DvergrCompanionState.PendingModeTeleportKey, false))
            {
                foreach (var player in Player.GetAllPlayers())
                {
                    if (!player || player.GetPlayerID() != zdo.GetLong(DvergrCompanionState.OwnerKey, 0L))
                    {
                        continue;
                    }

                    var destination = player.transform.position + player.transform.right * 2f;
                    character.transform.position = destination;
                    zdo.SetPosition(destination);
                    zdo.Set(DvergrCompanionState.PendingModeTeleportKey, false);
                    break;
                }
            }

            if (DvergrCompanionModeManager.GetMode(character) == CompanionMode.Sentry)
            {
                if (monsterAI.GetFollowTarget())
                {
                    monsterAI.SetFollowTarget(null);
                }

                _followOwnerMissingSince.Remove(zdo.m_uid);
                return;
            }

            var playerId = zdo.GetLong(DvergrCompanionState.OwnerKey, 0L);
            var followTarget = monsterAI.GetFollowTarget();

            foreach (var player in Player.GetAllPlayers())
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
                    var distance = Vector3.Distance(monsterAI.transform.position, player.transform.position);

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

            var missingDuration = Time.time - firstMissingTime;

            if (missingDuration < FollowOwnerGracePeriod)
            {
                return;
            }

            var localPlayer = Player.m_localPlayer;
            var isLocalOwner = localPlayer && localPlayer.GetPlayerID() == playerId;

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

        internal static void ResetSessionState()
        {
            _lastFollowLogTimes.Clear();
            _followOwnerMissingSince.Clear();
        }

        internal static void RemoveFollowState(ZDOID companionId)
        {
            _lastFollowLogTimes.Remove(companionId);
            _followOwnerMissingSince.Remove(companionId);
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
    }
}
