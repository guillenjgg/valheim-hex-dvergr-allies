using System.Collections.Generic;
using UnityEngine;

namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrCompanionManager
    {
        private const string HireRpc = "HexDvergrHire";
        private const string HiredKey = "HexDvergr_IsHired";
        private const string OwnerKey = "HexDvergr_HiredBy";
        private const string HomeKey = "HexDvergr_HomePosition";

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
                Plugin.Log.LogInfo(
                    $"[Dvergr Companion] Hire RPC received: Dvergr={character.name}, Sender={sender}, PlayerZDO={playerId}, IsOwner={nview.IsOwner()}");

                Hire(character, sender, playerId);
            });

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Initialized: Dvergr={character.name}, ZDO={nview.GetZDO().m_uid}, IsOwner={nview.IsOwner()}, IsHired={nview.GetZDO().GetBool(HiredKey, false)}");
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
            Plugin.Log.LogInfo(
                $"[Dvergr Companion] RequestHire entered: Dvergr={character.name}, Player={player.GetPlayerName()}");

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Hire request rejected: Reason=Invalid ZNetView");
                return;
            }

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Sending hire RPC: DvergrZDO={nview.GetZDO().m_uid}, PlayerZDO={player.GetZDOID()}, IsOwner={nview.IsOwner()}");

            nview.InvokeRPC(HireRpc, player.GetZDOID());
        }

        private static void Hire(Character character, long sender, ZDOID playerId)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                Plugin.Log.LogWarning(
                    "[Dvergr Companion] Hire failed: Reason=Invalid ZNetView");
                return;
            }

            if (!nview.IsOwner())
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Hire rejected: Reason=Not ZDO owner, Sender={sender}");
                return;
            }

            var playerObject = ZNetScene.instance.FindInstance(playerId);

            if (!playerObject)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Hire failed: Reason=Player object not found, PlayerZDO={playerId}");
                return;
            }

            var player = playerObject.GetComponent<Player>();

            if (!player)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Hire failed: Reason=Resolved object is not a Player, PlayerZDO={playerId}");
                return;
            }

            var distance = Vector3.Distance(
                player.transform.position,
                character.transform.position);

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Hire validation: Player={player.GetPlayerName()}, Distance={distance:F2}, MaxDistance={DvergrTraderManager.InteractionDistance:F2}");

            if (distance > DvergrTraderManager.InteractionDistance)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Hire rejected: Reason=Too far away, Distance={distance:F2}");
                return;
            }

            var zdo = nview.GetZDO();

            if (zdo.GetBool(HiredKey, false))
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Hire rejected: Reason=Already hired, ExistingOwner={zdo.GetLong(OwnerKey, 0L)}");
                return;
            }

            var monsterAI = character.GetComponent<MonsterAI>();

            if (!monsterAI)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Hire failed: Reason=MonsterAI component missing, Dvergr={character.name}");
                return;
            }

            zdo.Set(HomeKey, character.transform.position);
            zdo.Set(OwnerKey, player.GetPlayerID());
            zdo.Set(HiredKey, true);

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Hire state saved: DvergrZDO={zdo.m_uid}, Hired={zdo.GetBool(HiredKey, false)}, Owner={zdo.GetLong(OwnerKey, 0L)}, Home={character.transform.position}");

            monsterAI.ResetPatrolPoint();
            monsterAI.SetFollowTarget(player.gameObject);

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Hired successfully: Dvergr={character.name}, Player={player.GetPlayerName()}, PlayerID={player.GetPlayerID()}, FollowTarget={GetFollowTargetName(monsterAI)}");
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
                if (character.GetFaction() == Character.Faction.Dverger)
                {
                    Plugin.Log.LogWarning(
                        $"[Dvergr Companion] UpdateFollow skipped: Dvergr={character.name}, Reason=Invalid ZNetView");
                }

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
                if (shouldLog)
                {
                    Plugin.Log.LogInfo(
                        $"[Dvergr Companion] UpdateFollow skipped: Dvergr={character.name}, ZDO={zdo.m_uid}, Reason=Not ZDO owner");
                }

                return;
            }

            var playerId = zdo.GetLong(OwnerKey, 0L);
            var followTarget = monsterAI.GetFollowTarget();

            if (shouldLog)
            {
                Plugin.Log.LogInfo(
                    $"[Dvergr Companion] Follow tick: Dvergr={character.name}, ZDO={zdo.m_uid}, OwnerPlayerID={playerId}, CurrentTarget={GetFollowTargetName(monsterAI)}, Position={character.transform.position}");
            }

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
                    Plugin.Log.LogInfo(
                        $"[Dvergr Companion] Assigning follow target: Dvergr={character.name}, Player={player.GetPlayerName()}, Distance={distance:F2}, PreviousTarget={GetFollowTargetName(monsterAI)}");

                    monsterAI.ResetPatrolPoint();
                    monsterAI.SetFollowTarget(player.gameObject);

                    Plugin.Log.LogInfo(
                        $"[Dvergr Companion] Follow target assigned: Dvergr={character.name}, NewTarget={GetFollowTargetName(monsterAI)}");
                }

                if (shouldLog)
                {
                    Plugin.Log.LogInfo(
                        $"[Dvergr Companion] Follow status: Dvergr={character.name}, Player={player.GetPlayerName()}, Distance={distance:F2}, TargetMatches={monsterAI.GetFollowTarget() == player.gameObject}");
                }

                return;
            }

            if (shouldLog)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Follow owner not found: Dvergr={character.name}, OwnerPlayerID={playerId}, AvailablePlayers={Player.GetAllPlayers().Count}");
            }

            if (followTarget)
            {
                monsterAI.SetFollowTarget(null);

                Plugin.Log.LogInfo(
                    $"[Dvergr Companion] Follow target cleared: Dvergr={character.name}, Reason=Hiring player not found");
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

        private static string GetFollowTargetName(MonsterAI monsterAI)
        {
            var target = monsterAI.GetFollowTarget();

            return target ? target.name : "None";
        }
    }
}