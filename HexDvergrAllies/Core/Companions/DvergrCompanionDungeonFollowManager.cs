using HexDvergrAllies.Core;
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionDungeonFollowManager
    {
        private sealed class FollowState
        {
            internal float NextCheck;
            internal float BlockedSince;
            internal float BestDistance;
            internal GameObject Owner;
            internal bool WasSeparated;
        }

        private static readonly Dictionary<Character, FollowState> States = new Dictionary<Character, FollowState>();
        private static readonly Func<BaseAI, Vector3, bool> HavePath = AccessTools.MethodDelegate<Func<BaseAI, Vector3, bool>>(AccessTools.Method(typeof(BaseAI), "HavePath"));
        private static readonly Func<BaseAI, Character, bool> CanSee = AccessTools.MethodDelegate<Func<BaseAI, Character, bool>>(AccessTools.Method(typeof(BaseAI), "CanSeeTarget", new[] { typeof(Character) }));

        internal static bool IsDungeonFollow(MonsterAI ai)
        {
            if (!HexDvergrAlliesConfiguration.CompanionEnableDungeonFollowRecovery.Value)
            {
                return false;
            }

            var character = ai.GetComponent<Character>();
            var follow = ai.GetFollowTarget();
            var player = follow ? follow.GetComponent<Player>() : null;

            return character && player && character.InInterior() && player.InInterior() && DvergrCompanionManager.IsHiredByPlayer(character, player) && !DvergrCompanionSentryMode.IsActive(character);
        }

        internal static bool Update(MonsterAI ai)
        {
            var character = ai.GetComponent<Character>();

            if (!IsDungeonFollow(ai))
            {
                if (character)
                {
                    States.Remove(character);
                }

                return false;
            }

            var view = character.GetComponent<ZNetView>();

            if (!view || !view.IsValid() || !view.IsOwner())
            {
                return true;
            }

            var follow = ai.GetFollowTarget();
            var player = follow.GetComponent<Player>();

            if (player.IsDead() || player.IsTeleporting() || character.InAttack() || DvergrCompanionShipManager.IsPassenger(character))
            {
                States.Remove(character);

                return true;
            }

            if (!States.TryGetValue(character, out var state) || state.Owner != follow)
            {
                state = new FollowState { Owner = follow, BlockedSince = Time.time, BestDistance = Vector3.Distance(character.transform.position, follow.transform.position) };
                States[character] = state;
            }

            if (Time.time < state.NextCheck)
            {
                return true;
            }

            state.NextCheck = Time.time + 0.5f;
            var distance = Vector3.Distance(character.transform.position, follow.transform.position);
            var heightDifference = Mathf.Abs(character.transform.position.y - follow.transform.position.y);
            var mask = AccessTools.StaticFieldRefAccess<int>(typeof(BaseAI), "m_viewBlockMask");
            var blocked = Physics.Linecast(character.GetCenterPoint(), player.GetCenterPoint(), mask, QueryTriggerInteraction.Ignore);
            var hasPath = HavePath(ai, follow.transform.position);
            var separated = distance > 1.5f && (blocked || !hasPath || heightDifference >= 2.5f);
            var target = ai.GetTargetCreature();
            var fightingNearby = target && Mathf.Abs(target.transform.position.y - player.transform.position.y) < 2.5f && distance < 12f && CanSee(ai, target);

            if (!separated || fightingNearby)
            {
                state.BlockedSince = Time.time;
                state.BestDistance = distance;
                state.WasSeparated = false;

                return true;
            }

            if (!state.WasSeparated || state.BestDistance - distance >= 0.5f)
            {
                state.BlockedSince = Time.time;
                state.BestDistance = distance;
                state.WasSeparated = true;

                return true;
            }

            if (Time.time - state.BlockedSince < HexDvergrAlliesConfiguration.CompanionDungeonFollowRecoveryDelay.Value)
            {
                return true;
            }

            if (!TryGetDestination(character, player, mask, out var destination))
            {
                return true;
            }

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                HexLog.LogInfo($"[Dvergr Dungeon Follow Recovery] Dvergr={character.GetZDOID()}, Distance={distance:F2}, HeightDifference={heightDifference:F2}, Blocked={blocked}, HavePath={hasPath}, From={character.transform.position}, To={destination}");
            }

            var body = character.GetComponent<Rigidbody>();

            if (body)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = destination;
            }

            character.transform.position = destination;
            view.GetZDO().SetPosition(destination);
            ai.ResetPatrolPoint();
            States.Remove(character);

            return true;
        }

        private static bool TryGetDestination(Character character, Player player, int mask, out Vector3 destination)
        {
            destination = Vector3.zero;
            var capsule = character.GetComponent<CapsuleCollider>();

            if (!capsule)
            {
                return false;
            }

            var scale = character.transform.lossyScale;
            var radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            var height = Mathf.Max(capsule.height * Mathf.Abs(scale.y), radius * 2f);
            var centerOffset = character.transform.TransformVector(capsule.center);
            var offsets = new[] { Vector3.zero, player.transform.right * 0.75f, -player.transform.right * 0.75f, -player.transform.forward * 0.75f, player.transform.forward * 0.75f };

            foreach (var offset in offsets)
            {
                var point = player.transform.position + offset;

                if (!Physics.Raycast(point + Vector3.up * 0.5f, Vector3.down, out var ground, 1.1f, mask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                if (ground.normal.y < 0.5f || Mathf.Abs(ground.point.y - player.transform.position.y) > 0.6f)
                {
                    continue;
                }

                point.y = ground.point.y + 0.1f;
                var center = point + centerOffset;
                var halfSegment = Vector3.up * (height * 0.5f - radius);

                if (Physics.CheckCapsule(center - halfSegment, center + halfSegment, radius, mask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                if (Physics.Linecast(player.GetCenterPoint(), center, mask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                destination = point;

                return true;
            }

            return false;
        }

        internal static void RemoveCharacter(Character character)
        {
            States.Remove(character);
        }
    }
}
