using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionSpawnerTargeting
    {
        private const float DetectionRadius = 20f;
        private const float ScanInterval = 1f;

        private static readonly AccessTools.FieldRef<MonsterAI, StaticTarget> StaticTargetField = AccessTools.FieldRefAccess<MonsterAI, StaticTarget>("m_targetStatic");
        private static readonly AccessTools.FieldRef<MonsterAI, Character> CombatTargetField = AccessTools.FieldRefAccess<MonsterAI, Character>("m_targetCreature");

        private static readonly Dictionary<MonsterAI, float> NextScanTimes = new Dictionary<MonsterAI, float>();

        internal static void Update(MonsterAI monsterAI)
        {
            var character = monsterAI.GetComponent<Character>();

            if (!character || !DvergrCompanionState.IsHired(character) ||
                !character.name.StartsWith(PrefabHelper.AshlandsDvergrPrefix))
            {
                return;
            }

            var netView = character.GetComponent<ZNetView>();

            if (!netView || !netView.IsValid() || !netView.IsOwner())
            {
                return;
            }

            if (NextScanTimes.TryGetValue(monsterAI, out var nextScanTime) && Time.time < nextScanTime)
            {
                return;
            }

            NextScanTimes[monsterAI] = Time.time + ScanInterval;

            var origin = DvergrCompanionSentryMode.IsActive(character)
                ? DvergrCompanionSentryMode.GetAnchor(character)
                : character.transform.position;

            StaticTarget closestTarget = null;
            var closestDistance = DetectionRadius * DetectionRadius;

            foreach (var spawner in Object.FindObjectsByType<SpawnArea>(FindObjectsSortMode.None))
            {
                if (!spawner.name.StartsWith(PrefabHelper.CharredStoneSpawner))
                {
                    continue;
                }

                var offset = spawner.transform.position - origin;
                offset.y = 0f;

                var distance = offset.sqrMagnitude;

                if (distance >= closestDistance)
                {
                    continue;
                }

                var staticTarget = spawner.GetComponent<StaticTarget>();

                if (!staticTarget)
                {
                    staticTarget = spawner.gameObject.AddComponent<StaticTarget>();
                }

                closestTarget = staticTarget;
                closestDistance = distance;
            }

            if (!closestTarget)
            {
                return;
            }

            CombatTargetField(monsterAI) = null;
            StaticTargetField(monsterAI) = closestTarget;
        }

        internal static void RemoveCharacter(MonsterAI monsterAI)
        {
            NextScanTimes.Remove(monsterAI);
        }
    }
}
