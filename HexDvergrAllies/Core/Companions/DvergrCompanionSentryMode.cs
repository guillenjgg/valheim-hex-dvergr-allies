using UnityEngine;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionSentryMode
    {
        private const string SentryPositionKey = "HexDvergr_SentryPosition";

        internal const float CombatRadius = 20f;
        internal const float ReturnTolerance = 2f;

        internal static bool IsActive(Character character)
        {
            return DvergrCompanionManager.IsHired(character) &&
                   DvergrCompanionManager.GetMode(character) == DvergrCompanionManager.CompanionMode.Sentry;
        }

        internal static Vector3 GetAnchor(Character character)
        {
            var nview = character.GetComponent<ZNetView>();

            return nview.GetZDO().GetVec3(SentryPositionKey, character.transform.position);
        }

        internal static void Activate(Character character)
        {
            var nview = character.GetComponent<ZNetView>();

            nview.GetZDO().Set(SentryPositionKey, character.transform.position);
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

            if (monsterAI.GetTargetCreature())
            {
                return;
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