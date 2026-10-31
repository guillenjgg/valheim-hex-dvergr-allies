using System.Collections.Generic;
using HarmonyLib;
using HexDvergrAllies.Core.Companions;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch]
    internal static class BaseAICompanionCombatLeashPatch
    {
        private const float LeashDistance = 15f;
        private const float BoundaryBuffer = 0.5f;
        private const float ReturnDistance = 13.5f;
        private const float LogInterval = 2f;

        private static readonly Dictionary<MonsterAI, float> NextBlockedLogTimes = new Dictionary<MonsterAI, float>();
        private static readonly Dictionary<MonsterAI, float> NextCorrectionLogTimes = new Dictionary<MonsterAI, float>();

        [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.MoveTowards))]
        [HarmonyPrefix]
        private static void MoveTowardsPrefix(BaseAI __instance, ref Vector3 dir)
        {
            MonsterAI monsterAI = __instance as MonsterAI;
            Character character;
            Transform owner;

            if (!TryGetCompanion(monsterAI, out character, out owner))
            {
                return;
            }

            Vector3 offset = character.transform.position - owner.position;
            offset.y = 0f;

            float distance = offset.magnitude;

            if (distance < LeashDistance - BoundaryBuffer)
            {
                return;
            }

            Vector3 outward = offset.normalized;
            Vector3 horizontalDirection = new Vector3(dir.x, 0f, dir.z);
            float outwardMovement = Vector3.Dot(horizontalDirection, outward);

            if (outwardMovement <= 0f)
            {
                return;
            }

            Vector3 originalDirection = dir;

            // Remove only movement directed away from the owner.
            // Sideways and inward movement remain permitted.
            horizontalDirection -= outward * outwardMovement;

            dir = new Vector3(horizontalDirection.x, dir.y, horizontalDirection.z);

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value && ShouldLog(NextBlockedLogTimes, monsterAI))
            {
                Character target = monsterAI.GetTargetCreature();
                float targetDistance = target ? Vector3.Distance(character.transform.position, target.transform.position) : -1f;

                Plugin.Log.LogInfo(
                    $"[Dvergr Companion] Combat leash movement blocked: " +
                    $"Dvergr={character.name}, " +
                    $"OwnerDistance={distance:F1}, " +
                    $"Target={GetTargetName(target)}, " +
                    $"TargetDistance={targetDistance:F1}, " +
                    $"OutwardMovement={outwardMovement:F2}, " +
                    $"OriginalDirection={originalDirection}, " +
                    $"AdjustedDirection={dir}");
            }
        }

        [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
        [HarmonyPostfix]
        private static void UpdateAIPostfix(MonsterAI __instance)
        {
            Character character;
            Transform owner;

            if (!TryGetCompanion(__instance, out character, out owner))
            {
                return;
            }

            Vector3 toOwner = owner.position - character.transform.position;
            toOwner.y = 0f;

            float distance = toOwner.magnitude;

            if (distance <= LeashDistance)
            {
                return;
            }

            Character target = __instance.GetTargetCreature();
            float targetDistance = target ? Vector3.Distance(character.transform.position, target.transform.position) : -1f;
            Vector3 returnDirection = toOwner.normalized;

            // Preserve existing behavior: override vanilla movement
            // without clearing the combat target or disabling attacks.
            __instance.MoveTowards(returnDirection, true);

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value && ShouldLog(NextCorrectionLogTimes, __instance))
            {
                Plugin.Log.LogInfo(
                    $"[Dvergr Companion] Combat leash correction: " +
                    $"Dvergr={character.name}, " +
                    $"OwnerDistance={distance:F1}, " +
                    $"Leash={LeashDistance:F1}, " +
                    $"ReturnTarget={ReturnDistance:F1}, " +
                    $"Target={GetTargetName(target)}, " +
                    $"TargetDistance={targetDistance:F1}, " +
                    $"ReturnDirection={returnDirection}");
            }
        }

        private static bool ShouldLog(Dictionary<MonsterAI, float> nextLogTimes, MonsterAI monsterAI)
        {
            float nextLogTime;

            if (nextLogTimes.TryGetValue(monsterAI, out nextLogTime) && Time.time < nextLogTime)
            {
                return false;
            }

            nextLogTimes[monsterAI] = Time.time + LogInterval;

            return true;
        }

        private static string GetTargetName(Character target)
        {
            return target ? target.name : "None";
        }

        private static bool TryGetCompanion(MonsterAI monsterAI, out Character character, out Transform owner)
        {
            character = null;
            owner = null;

            if (monsterAI == null)
            {
                return false;
            }

            character = monsterAI.GetComponent<Character>();

            if (character == null || !DvergrCompanionManager.IsHired(character))
            {
                return false;
            }

            ZNetView netView = character.GetComponent<ZNetView>();

            if (netView == null || !netView.IsValid() || !netView.IsOwner())
            {
                return false;
            }

            GameObject followTarget = monsterAI.GetFollowTarget();

            if (followTarget == null)
            {
                return false;
            }

            owner = followTarget.transform;

            return true;
        }
    }
}
