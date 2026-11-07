using HexDvergrAllies.Core;
using System.Collections.Generic;
using HarmonyLib;
using HexDvergrAllies.Core.Companions;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch]
    internal static class BaseAICompanionCombatLeashPatch
    {
        private const float CompanionLeashDistance = 15f;
        private const float BoundaryBuffer = 0.5f;
        private const float LogInterval = 2f;

        private static readonly Dictionary<MonsterAI, float> NextBlockedLogTimes = new Dictionary<MonsterAI, float>();

        [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.MoveTowards))]
        [HarmonyPrefix]
        private static void MoveTowardsPrefix(BaseAI __instance, ref Vector3 dir)
        {
            var monsterAI = __instance as MonsterAI;
            Character character;
            Transform owner;

            if (!TryGetCompanion(monsterAI, out character, out owner))
            {
                return;
            }

            var isSentry = DvergrCompanionSentryMode.IsActive(character);

            if (!isSentry && monsterAI.GetTargetCreature() == null && monsterAI.GetStaticTarget() == null)
            {
                return;
            }

            var leashDistance = isSentry ? DvergrCompanionSentryMode.CombatRadius : CompanionLeashDistance;
            var referencePosition = isSentry ? DvergrCompanionSentryMode.GetAnchor(character) : owner.position;
            var offset = character.transform.position - referencePosition;
            offset.y = 0f;

            var distance = offset.magnitude;

            if (distance < leashDistance - BoundaryBuffer)
            {
                return;
            }

            var outward = offset.normalized;
            var horizontalDirection = new Vector3(dir.x, 0f, dir.z);
            var outwardMovement = Vector3.Dot(horizontalDirection, outward);

            if (outwardMovement <= 0f)
            {
                return;
            }

            var originalDirection = dir;

            horizontalDirection -= outward * outwardMovement;

            dir = new Vector3(horizontalDirection.x, dir.y, horizontalDirection.z);

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value && ShouldLog(NextBlockedLogTimes, monsterAI))
            {
                var target = monsterAI.GetTargetCreature();
                var targetDistance = target ? Vector3.Distance(character.transform.position, target.transform.position) : -1f;

                HexLog.LogInfo(
                    $"[Dvergr Companion] Combat leash movement blocked: " +
                    $"Dvergr={character.name}, " +
                    $"OwnerDistance={distance:F1}, " +
                    $"LeashDistance={leashDistance:F1}, " +
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

            var isSentry = DvergrCompanionSentryMode.IsActive(character);

            if (!isSentry && __instance.GetTargetCreature() == null && __instance.GetStaticTarget() == null)
            {
                return;
            }

            var leashDistance = isSentry ? DvergrCompanionSentryMode.CombatRadius : CompanionLeashDistance;
            var referencePosition = isSentry ? DvergrCompanionSentryMode.GetAnchor(character) : owner.position;

            var toOwner = referencePosition - character.transform.position;
            toOwner.y = 0f;

            var distance = toOwner.magnitude;

            if (distance <= leashDistance)
            {
                return;
            }

            var returnDirection = toOwner.normalized;

            __instance.MoveTowards(returnDirection, true);
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

            if (!monsterAI)
            {
                return false;
            }

            character = monsterAI.GetComponent<Character>();

            if (!character || !DvergrCompanionState.IsHired(character))
            {
                return false;
            }

            var netView = character.GetComponent<ZNetView>();

            if (!netView || !netView.IsValid() || !netView.IsOwner())
            {
                return false;
            }

            if (DvergrCompanionSentryMode.IsActive(character))
            {
                owner = character.transform;
                return true;
            }

            var followTarget = monsterAI.GetFollowTarget();

            if (!followTarget)
            {
                return false;
            }

            owner = followTarget.transform;

            return true;
        }
    }
}