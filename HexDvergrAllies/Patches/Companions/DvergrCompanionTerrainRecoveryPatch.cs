using System.Collections.Generic;
using HarmonyLib;
using HexDvergrAllies.Core.Managers;
using UnityEngine;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class DvergrCompanionTerrainRecoveryPatch
    {
        private const float CheckInterval = 0.5f;
        private const float RecoveryDistance = 12f;
        private const float ImmediateRecoveryDistance = 30f;
        private const float StuckDuration = 3f;
        private const float MinimumProgress = 1f;
        private const float RecoveryOffset = 2f;

        private sealed class RecoveryState
        {
            internal float NextCheckTime;
            internal float LastDistance;
            internal float LastProgressTime;
            internal GameObject FollowTarget;
        }

        private static readonly Dictionary<MonsterAI, RecoveryState> States = new Dictionary<MonsterAI, RecoveryState>();

        [HarmonyPostfix]
        private static void Postfix(MonsterAI __instance)
        {
            var character = __instance.GetComponent<Character>();

            if (character == null || !DvergrCompanionManager.IsHired(character))
            {
                States.Remove(__instance);
                return;
            }

            if (DvergrCompanionShipManager.IsPassenger(character))
            {
                States.Remove(__instance);
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                States.Remove(__instance);
                return;
            }

            var followTarget = __instance.GetFollowTarget();

            if (followTarget == null)
            {
                States.Remove(__instance);
                return;
            }

            var distance = Vector3.Distance(character.transform.position, followTarget.transform.position);

            if (distance >= ImmediateRecoveryDistance)
            {
                Recover(character, followTarget, distance, "Distance limit");
                States.Remove(__instance);
                return;
            }

            if (__instance.GetTargetCreature() != null || __instance.GetStaticTarget() != null)
            {
                States.Remove(__instance);
                return;
            }

            if (distance < RecoveryDistance)
            {
                States.Remove(__instance);
                return;
            }

            if (!States.TryGetValue(__instance, out var state))
            {
                state = new RecoveryState
                {
                    NextCheckTime = Time.time + CheckInterval,
                    LastDistance = distance,
                    LastProgressTime = Time.time,
                    FollowTarget = followTarget
                };

                States.Add(__instance, state);
                return;
            }

            if (state.FollowTarget != followTarget)
            {
                States.Remove(__instance);
                return;
            }

            if (Time.time < state.NextCheckTime)
            {
                return;
            }

            state.NextCheckTime = Time.time + CheckInterval;

            var progress = state.LastDistance - distance;

            if (progress >= MinimumProgress)
            {
                state.LastProgressTime = Time.time;
            }

            state.LastDistance = distance;

            if (Time.time - state.LastProgressTime < StuckDuration)
            {
                return;
            }

            Recover(character, followTarget, distance, "No progress");
            States.Remove(__instance);
        }

        private static void Recover(Character character, GameObject followTarget, float distance, string reason)
        {
            var destination = followTarget.transform.position + followTarget.transform.right * RecoveryOffset;

            Plugin.Log.LogInfo($"[Dvergr Terrain Recovery] Dvergr={character.name}, Reason={reason}, Distance={distance:F1}, From={character.transform.position}, To={destination}");

            character.transform.position = destination;
        }
    }
}
