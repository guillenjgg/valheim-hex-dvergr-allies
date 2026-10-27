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

        private static readonly Dictionary<ZDOID, float> _lastFollowLogTimes =
            new Dictionary<ZDOID, float>();

        internal static void Initialize(Character character)
        {
            if (character.GetFaction() != Character.Faction.Dverger)
            {
                return;
            }

            var nview = character.GetComponent<ZNetView>();

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

            var nview = character.GetComponent<ZNetView>();

            return nview &&
                   nview.IsValid() &&
                   nview.GetZDO().GetBool(HiredKey, false);
        }

        internal static void RequestHire(Player player, Character character)
        {
            var nview = character.GetComponent<ZNetView>();

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
            var nview = character.GetComponent<ZNetView>();

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
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var player = ResolvePlayer(playerId);

            if (!player || !IsWithinInteractionDistance(player, character))
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Hire rejected: PlayerZDO={playerId}, Reason=Player missing or out of range");
                return;
            }

            var zdo = nview.GetZDO();

            if (zdo.GetBool(HiredKey, false))
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Hire rejected: Reason=Already hired, Owner={zdo.GetLong(OwnerKey, 0L)}");
                return;
            }

            var monsterAI = character.GetComponent<MonsterAI>();

            if (!monsterAI)
            {
                Plugin.Log.LogWarning(
                    "[Dvergr Companion] Hire rejected: Reason=MonsterAI missing");
                return;
            }

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
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var zdo = nview.GetZDO();

            if (!zdo.GetBool(HiredKey, false))
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Unhire rejected: Dvergr={character.name}, Reason=Not hired");
                return;
            }

            var player = ResolvePlayer(playerId);

            if (!player || !IsWithinInteractionDistance(player, character))
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Unhire rejected: PlayerZDO={playerId}, Reason=Player missing or out of range");
                return;
            }

            var ownerPlayerId = zdo.GetLong(OwnerKey, 0L);

            if (ownerPlayerId != player.GetPlayerID())
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Unhire rejected: Player={player.GetPlayerName()}, Reason=Not hiring player");
                return;
            }

            var monsterAI = character.GetComponent<MonsterAI>();

            if (!monsterAI)
            {
                Plugin.Log.LogWarning(
                    "[Dvergr Companion] Unhire rejected: Reason=MonsterAI missing");
                return;
            }

            monsterAI.SetFollowTarget(null);
            monsterAI.ResetPatrolPoint();

            DvergrCompanionBehaviorManager.ResetBehavior(character, monsterAI);

            zdo.Set(HiredKey, false);
            zdo.Set(OwnerKey, 0L);

            _lastFollowLogTimes.Remove(zdo.m_uid);

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Unhired successfully: Dvergr={character.name}, Player={player.GetPlayerName()}, HomeSaved={zdo.GetBool(HomeSavedKey, false)}, CurrentPosition={character.transform.position}");
        }

        private static Player ResolvePlayer(ZDOID playerId)
        {
            if (ZNetScene.instance == null)
            {
                return null;
            }

            var playerObject = ZNetScene.instance.FindInstance(playerId);

            return playerObject ? playerObject.GetComponent<Player>() : null;
        }

        private static bool IsWithinInteractionDistance(Player player, Character character)
        {
            var distance = Vector3.Distance(
                player.transform.position,
                character.transform.position);

            return distance <= DvergrTraderManager.InteractionDistance;
        }

        internal static void UpdateFollow(MonsterAI monsterAI)
        {
            var character = monsterAI.GetComponent<Character>();

            if (!character)
            {
                return;
            }

            var nview = monsterAI.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                return;
            }

            var zdo = nview.GetZDO();

            if (!zdo.GetBool(HiredKey, false))
            {
                return;
            }

            var shouldLog = ShouldLogFollow(zdo.m_uid);

            if (!nview.IsOwner())
            {
                return;
            }

            var playerId = zdo.GetLong(OwnerKey, 0L);
            var followTarget = monsterAI.GetFollowTarget();

            foreach (var player in Player.GetAllPlayers())
            {
                if (player.GetPlayerID() != playerId)
                {
                    continue;
                }

                var distance = Vector3.Distance(
                    monsterAI.transform.position,
                    player.transform.position);

                if (followTarget != player.gameObject)
                {
                    monsterAI.ResetPatrolPoint();
                    monsterAI.SetFollowTarget(player.gameObject);

                    Plugin.Log.LogInfo(
                        $"[Dvergr Companion] Follow target assigned: Dvergr={character.name}, Player={player.GetPlayerName()}");
                }

                if (shouldLog)
                {
                    Plugin.Log.LogInfo(
                        $"[Dvergr Companion] Follow status: Dvergr={character.name}, Player={player.GetPlayerName()}, Distance={distance:F2}");
                }

                return;
            }

            if (shouldLog)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Follow owner not found: Dvergr={character.name}, OwnerPlayerID={playerId}");
            }

            if (followTarget)
            {
                monsterAI.SetFollowTarget(null);
            }
        }

        private static bool ShouldLogFollow(ZDOID zdoId)
        {
            if (_lastFollowLogTimes.TryGetValue(zdoId, out var lastLogTime) &&
                Time.time - lastLogTime < FollowLogInterval)
            {
                return false;
            }

            _lastFollowLogTimes[zdoId] = Time.time;
            return true;
        }
    }
}