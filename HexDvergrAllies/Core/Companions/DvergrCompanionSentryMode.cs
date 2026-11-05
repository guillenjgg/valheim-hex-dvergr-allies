using HarmonyLib;
using UnityEngine;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionSentryMode
    {
        internal const string SentryPositionKey = "HexDvergr_SentryPosition";

        internal const float CombatRadius = 20f;
        internal const float ReturnTolerance = 2f;

        private static readonly AccessTools.FieldRef<MonsterAI, Character> CombatTargetField = AccessTools.FieldRefAccess<MonsterAI, Character>("m_targetCreature");
        private static readonly AccessTools.FieldRef<MonsterAI, Vector3> LastKnownTargetPositionField = AccessTools.FieldRefAccess<MonsterAI, Vector3>("m_lastKnownTargetPos");
        private static readonly AccessTools.FieldRef<MonsterAI, bool> BeenAtLastPositionField = AccessTools.FieldRefAccess<MonsterAI, bool>("m_beenAtLastPos");

        internal static bool IsActive(Character character)
        {
            return DvergrCompanionState.IsHired(character) &&
                   DvergrCompanionModeManager.GetMode(character) == DvergrCompanionMode.Sentry;
        }

        internal static Vector3 GetAnchor(Character character)
        {
            var nview = character.GetComponent<ZNetView>();

            return nview.GetZDO().GetVec3(SentryPositionKey, character.transform.position);
        }

        internal static void Activate(Character character, Vector3 position)
        {
            var nview = character.GetComponent<ZNetView>();

            nview.GetZDO().Set(SentryPositionKey, position);
        }

        internal static bool IsWithinCombatRadius(Character character, Character target)
        {
            var offset = target.transform.position - GetAnchor(character);
            offset.y = 0f;

            return offset.sqrMagnitude <= CombatRadius * CombatRadius;
        }

        internal static void Update(MonsterAI monsterAI)
        {
            var character = monsterAI.GetComponent<Character>();

            if (!character || !IsActive(character))
            {
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var target = monsterAI.GetTargetCreature();

            if (target)
            {
                if (IsWithinCombatRadius(character, target))
                {
                    return;
                }

                CombatTargetField(monsterAI) = null;
                LastKnownTargetPositionField(monsterAI) = GetAnchor(character);
                BeenAtLastPositionField(monsterAI) = true;
            }

            var anchor = GetAnchor(character);
            var direction = anchor - character.transform.position;
            direction.y = 0f;

            if (direction.magnitude <= ReturnTolerance)
            {
                return;
            }

            monsterAI.MoveTowards(direction.normalized, true);
        }
    }
}