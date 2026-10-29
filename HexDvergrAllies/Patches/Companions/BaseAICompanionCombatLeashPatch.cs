using HarmonyLib;
using HexDvergrAllies.Core.Managers;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch]
    internal static class BaseAICompanionCombatLeashPatch
    {
        private const float LeashDistance = 15f;
        private const float BoundaryBuffer = 0.5f;
        private const float ReturnDistance = 13.5f;

        [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.MoveTowards))]
        [HarmonyPrefix]
        private static void MoveTowardsPrefix(
            BaseAI __instance,
            ref Vector3 dir)
        {
            MonsterAI monsterAI = __instance as MonsterAI;

            Character character;
            Transform owner;

            if (!TryGetCompanion(
                monsterAI,
                out character,
                out owner))
            {
                return;
            }

            Vector3 offset =
                character.transform.position - owner.position;

            offset.y = 0f;

            float distance = offset.magnitude;

            if (distance < LeashDistance - BoundaryBuffer)
            {
                return;
            }

            Vector3 outward = offset.normalized;

            Vector3 horizontalDirection =
                new Vector3(dir.x, 0f, dir.z);

            float outwardMovement =
                Vector3.Dot(horizontalDirection, outward);

            if (outwardMovement <= 0f)
            {
                return;
            }

            // Remove only the component pointing away from the owner.
            // Sideways and inward movement remain permitted.
            horizontalDirection -= outward * outwardMovement;

            dir = new Vector3(
                horizontalDirection.x,
                dir.y,
                horizontalDirection.z);
        }

        [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
        [HarmonyPostfix]
        private static void UpdateAIPostfix(
            MonsterAI __instance)
        {
            Character character;
            Transform owner;

            if (!TryGetCompanion(
                __instance,
                out character,
                out owner))
            {
                return;
            }

            Vector3 toOwner =
                owner.position - character.transform.position;

            toOwner.y = 0f;

            float distance = toOwner.magnitude;

            if (distance <= LeashDistance)
            {
                return;
            }

            // Override the movement selected by vanilla combat AI.
            // Do not clear the enemy target or disable attacking.
            Vector3 returnDirection = toOwner.normalized;

            __instance.MoveTowards(returnDirection, true);

            // MoveTowards can stop movement while turning.
            // The character will resume moving when facing the owner.
            //if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            //{
            //    LogReturn(__instance, distance);
            //}
        }

        private static readonly System.Collections.Generic.Dictionary<
            MonsterAI, float> NextLogTimes =
                new System.Collections.Generic.Dictionary<MonsterAI, float>();

        private static void LogReturn(
            MonsterAI monsterAI,
            float distance)
        {
            float nextLogTime;

            if (NextLogTimes.TryGetValue(
                monsterAI,
                out nextLogTime) &&
                Time.time < nextLogTime)
            {
                return;
            }

            NextLogTimes[monsterAI] = Time.time + 2f;

            Plugin.Log.LogInfo(
                "[Dvergr Companion] Combat leash correction: " +
                $"Dvergr={monsterAI.name}, " +
                $"Distance={distance:F1}, " +
                $"Leash={LeashDistance:F1}, " +
                $"ReturnTarget={ReturnDistance:F1}");
        }

        private static bool TryGetCompanion(
            MonsterAI monsterAI,
            out Character character,
            out Transform owner)
        {
            character = null;
            owner = null;

            if (monsterAI == null)
            {
                return false;
            }

            character = monsterAI.GetComponent<Character>();

            if (character == null ||
                !DvergrCompanionManager.IsHired(character))
            {
                return false;
            }

            ZNetView netView = character.GetComponent<ZNetView>();

            if (netView == null ||
                !netView.IsValid() ||
                !netView.IsOwner())
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
