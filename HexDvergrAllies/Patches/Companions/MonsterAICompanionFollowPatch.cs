using HarmonyLib;
using HexDvergrAllies.Core.Companions;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class MonsterAICompanionFollowPatch
    {
        private static float _nextDiagnosticLogTime;

        private static void Prefix(MonsterAI __instance)
        {
            var character = __instance.GetComponent<Character>();

            if (!character)
            {
                return;
            }

            if (DvergrCompanionShipManager.IsPassenger(character))
            {
                return;
            }

            DvergrCompanionFollowManager.UpdateFollow(__instance);
        }

        private static void Postfix(MonsterAI __instance)
        {
            DvergrCompanionSpawnerTargeting.Update(__instance);
            DvergrCompanionSentryMode.Update(__instance);

            if (!HexDvergrAlliesConfiguration.EnableDebugLogs.Value || Time.time < _nextDiagnosticLogTime)
            {
                return;
            }

            var character = __instance.GetComponent<Character>();

            if (!character || !DvergrCompanionState.IsHired(character))
            {
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            _nextDiagnosticLogTime = Time.time + 2f;

            var target = __instance.GetTargetCreature();
            var followTarget = __instance.GetFollowTarget();
            var targetOffset = target ? target.transform.position - character.transform.position : Vector3.zero;
            var targetDistance = target ? targetOffset.magnitude : -1f;
            var horizontalDistance = new Vector2(targetOffset.x, targetOffset.z).magnitude;
            var verticalDifference = Mathf.Abs(targetOffset.y);

            Plugin.Log.LogInfo(
                $"[Dvergr AI Diagnostic] " +
                $"Dvergr={character.name}, " +
                $"Position={character.transform.position}, " +
                $"Target={(target ? target.name : "None")}, " +
                $"TargetDistance={targetDistance:F1}, " +
                $"Alerted={__instance.IsAlerted()}, " +
                $"InAttack={character.InAttack()}, " +
                $"Tamed={character.IsTamed()}, " +
                $"FollowTarget={(followTarget ? followTarget.name : "None")}, " +
                $"HorizontalDistance={horizontalDistance:F1}, " +
                $"VerticalDifference={verticalDifference:F1}");
        }
    }
}