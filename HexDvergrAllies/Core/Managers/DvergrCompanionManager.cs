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

        private const float FollowLogInterval = 5f;
        private const float FollowOwnerGracePeriod = 10f;

        private static readonly Dictionary<ZDOID, float> _lastFollowLogTimes =
            new Dictionary<ZDOID, float>();

        private static readonly Dictionary<ZDOID, float> _followOwnerMissingSince =
            new Dictionary<ZDOID, float>();

        internal static void Initialize(Character character)
        {
            if (character.GetFaction() != Character.Faction.Dverger)
            {
                return;
            }

            ZNetView nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Initialize failed: Dvergr={character.name}, Reason=Invalid ZNetView");
                return;
            }

            nview.Register<ZDOID>(HireRpc, (sender, playerId) =>
            {
                Hire(character, sender, playerId);
            });

            nview.Register<ZDOID>(UnhireRpc, (sender, playerId) =>
            {
                Unhire(character, sender, playerId);
            });

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Initialized: Dvergr={character.name}, ZDO={nview.GetZDO().m_uid}, IsOwner={nview.IsOwner()}, IsHired={IsHired(character)}");
        }

        internal static bool IsHired(Character character)
        {
            if (!character)
            {
                return false;
            }

            ZNetView nview = character.GetComponent<ZNetView>();

            return nview &&
                   nview.IsValid() &&
                   nview.GetZDO().GetBool(HiredKey, false);
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

            return zdo.GetBool("HexDvergr_IsHired", false) &&
                   zdo.GetLong("HexDvergr_HiredBy", 0L) == player.GetPlayerID();
        }

        internal static void RequestHire(Player player, Character character)
        {
            ZNetView nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                Plugin.Log.LogWarning(
                    "[Dvergr Companion] Hire request rejected: Reason=Invalid ZNetView");
                return;
            }

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Sending hire RPC: DvergrZDO={nview.GetZDO().m_uid}, PlayerZDO={player.GetZDOID()}");

            nview.InvokeRPC(HireRpc, player.GetZDOID());
        }

        internal static void RequestUnhire(Player player, Character character)
        {
            ZNetView nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                Plugin.Log.LogWarning(
                    "[Dvergr Companion] Unhire request rejected: Reason=Invalid ZNetView");
                return;
            }

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Sending unhire RPC: DvergrZDO={nview.GetZDO().m_uid}, PlayerZDO={player.GetZDOID()}");

            nview.InvokeRPC(UnhireRpc, player.GetZDOID());
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
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Hire rejected: PlayerZDO={playerId}, Reason=Player missing or out of range");
                return;
            }

            ZDO zdo = nview.GetZDO();

            if (zdo.GetBool(HiredKey, false))
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Hire rejected: Reason=Already hired, Owner={zdo.GetLong(OwnerKey, 0L)}");
                return;
            }

            MonsterAI monsterAI = character.GetComponent<MonsterAI>();

            if (!monsterAI)
            {
                Plugin.Log.LogWarning(
                    "[Dvergr Companion] Hire rejected: Reason=MonsterAI missing");
                return;
            }

            DvergrCompanionBehaviorManager.CaptureState(character, monsterAI);

            if (!zdo.GetBool(HomeSavedKey, false))
            {
                zdo.Set(HomeKey, character.transform.position);
                zdo.Set(HomeSavedKey, true);

                Plugin.Log.LogInfo(
                    $"[Dvergr Companion] Home saved: Dvergr={character.name}, Position={character.transform.position}");
            }

            zdo.Set(OwnerKey, player.GetPlayerID());
            zdo.Set(HiredKey, true);

            monsterAI.ResetPatrolPoint();
            monsterAI.SetFollowTarget(player.gameObject);

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Hired successfully: Dvergr={character.name}, Player={player.GetPlayerName()}, PlayerID={player.GetPlayerID()}");
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
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Unhire rejected: Dvergr={character.name}, Reason=Not hired");
                return;
            }

            Player player = ResolvePlayer(playerId);

            if (!player || !IsWithinInteractionDistance(player, character))
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Unhire rejected: PlayerZDO={playerId}, Reason=Player missing or out of range");
                return;
            }

            long ownerPlayerId = zdo.GetLong(OwnerKey, 0L);

            if (ownerPlayerId != player.GetPlayerID())
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Unhire rejected: Player={player.GetPlayerName()}, Reason=Not hiring player");
                return;
            }

            MonsterAI monsterAI = character.GetComponent<MonsterAI>();

            if (!monsterAI)
            {
                Plugin.Log.LogWarning(
                    "[Dvergr Companion] Unhire rejected: Reason=MonsterAI missing");
                return;
            }

            if (!zdo.GetBool(HomeSavedKey, false))
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Unhire rejected: Dvergr={character.name}, Reason=Original home missing");
                return;
            }

            Vector3 homePosition = zdo.GetVec3(HomeKey, character.transform.position);

            monsterAI.SetFollowTarget(null);
            monsterAI.ResetPatrolPoint();

            DvergrCompanionBehaviorManager.ResetBehavior(character, monsterAI);

            zdo.Set(HiredKey, false);
            zdo.Set(OwnerKey, 0L);

            _lastFollowLogTimes.Remove(zdo.m_uid);
            _followOwnerMissingSince.Remove(zdo.m_uid);

            ReturnHome(character, zdo, homePosition);

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Unhired successfully: Dvergr={character.name}, Player={player.GetPlayerName()}, Home={homePosition}");
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

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Returned home: Dvergr={character.name}, ZDO={zdo.m_uid}, From={previousPosition}, To={homePosition}, Distance={distance:F2}");
        }

        private static Player ResolvePlayer(ZDOID playerId)
        {
            if (ZNetScene.instance == null)
            {
                return null;
            }

            GameObject playerObject = ZNetScene.instance.FindInstance(playerId);

            return playerObject ? playerObject.GetComponent<Player>() : null;
        }

        private static bool IsWithinInteractionDistance(Player player, Character character)
        {
            float distance = Vector3.Distance(
                player.transform.position,
                character.transform.position);

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
                    Plugin.Log.LogInfo(
                        $"[Dvergr Companion] Follow owner resolved: Dvergr={character.name}, OwnerPlayerID={playerId}, Waited={Time.time - missingSince:F2}s");

                    _followOwnerMissingSince.Remove(zdo.m_uid);
                }

                if (followTarget != player.gameObject)
                {
                    monsterAI.ResetPatrolPoint();
                    monsterAI.SetFollowTarget(player.gameObject);

                    Plugin.Log.LogInfo(
                        $"[Dvergr Companion] Follow target assigned: Dvergr={character.name}, Player={player.GetPlayerName()}");
                }

                if (ShouldLogFollow(zdo.m_uid))
                {
                    float distance = Vector3.Distance(
                        monsterAI.transform.position,
                        player.transform.position);

                    Plugin.Log.LogInfo(
                        $"[Dvergr Companion] Follow status: Dvergr={character.name}, Player={player.GetPlayerName()}, Distance={distance:F2}");
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

            if (ShouldLogFollow(zdo.m_uid))
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Follow owner not found: Dvergr={character.name}, OwnerPlayerID={playerId}, MissingFor={missingDuration:F2}s, LocalPlayerReady={Player.m_localPlayer != null}");
            }

            if (followTarget)
            {
                monsterAI.SetFollowTarget(null);
            }
        }

        private static bool ShouldLogFollow(ZDOID zdoId)
        {
            float lastLogTime;

            if (_lastFollowLogTimes.TryGetValue(zdoId, out lastLogTime) &&
                Time.time - lastLogTime < FollowLogInterval)
            {
                return false;
            }

            _lastFollowLogTimes[zdoId] = Time.time;
            return true;
        }
    }
}
