using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class DvergrCompanionLedgeFollowPatch
    {
        private const float MinimumHeightDifference = 3f;
        private const float MinimumHorizontalDistance = 2f;
        private const float MaximumHorizontalDistance = 12f;

        [HarmonyPostfix]
        private static void Postfix(MonsterAI __instance)
        {
            var character = __instance.GetComponent<Character>();

            if (!character || !DvergrCompanionManager.IsHired(character))
            {
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var followTarget = __instance.GetFollowTarget();

            if (!followTarget)
            {
                return;
            }

            if (__instance.GetTargetCreature() != null ||
                __instance.GetStaticTarget() != null)
            {
                return;
            }

            var characterPosition = character.transform.position;
            var targetPosition = followTarget.transform.position;

            var heightDifference = characterPosition.y - targetPosition.y;

            if (heightDifference < MinimumHeightDifference)
            {
                return;
            }

            var direction = targetPosition - characterPosition;
            direction.y = 0f;

            var horizontalDistance = direction.magnitude;

            if (horizontalDistance < MinimumHorizontalDistance ||
                horizontalDistance > MaximumHorizontalDistance)
            {
                return;
            }

            character.SetMoveDir(direction.normalized);
            character.SetRun(true);
        }
    }
}
