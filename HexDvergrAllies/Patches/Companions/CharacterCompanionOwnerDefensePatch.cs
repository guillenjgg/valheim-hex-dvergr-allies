using HarmonyLib;
using HexDvergrAllies.Core.Managers;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    internal static class CharacterCompanionOwnerDefensePatch
    {
        private const float MaximumDefenseDistance = 30f;

        private static readonly System.Action<BaseAI, bool> SetAlertedMethod =
            AccessTools.MethodDelegate<System.Action<BaseAI, bool>>(
                AccessTools.Method(
                    typeof(BaseAI),
                    "SetAlerted",
                    new[] { typeof(bool) }));

        private static readonly System.Action<MonsterAI, Character> SetTargetMethod =
            AccessTools.MethodDelegate<System.Action<MonsterAI, Character>>(
                AccessTools.Method(
                    typeof(MonsterAI),
                    "SetTarget",
                    new[] { typeof(Character) }));

        private static void Postfix(Character __instance, HitData hit)
        {
            if (__instance == null || !__instance.IsPlayer() || hit == null)
            {
                return;
            }

            Character attacker = hit.GetAttacker();

            if (attacker == null || attacker == __instance || attacker.IsDead())
            {
                return;
            }

            if (attacker.IsPlayer())
            {
                return;
            }

            MonsterAI[] monsters = Object.FindObjectsByType<MonsterAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (MonsterAI monsterAI in monsters)
            {
                if (monsterAI == null)
                {
                    continue;
                }

                Character companion = monsterAI.GetComponent<Character>();

                if (companion == null ||
                    !DvergrCompanionManager.IsHired(companion) ||
                    companion.IsDead())
                {
                    continue;
                }

                ZNetView netView = companion.GetComponent<ZNetView>();

                if (netView == null || !netView.IsValid() || !netView.IsOwner())
                {
                    continue;
                }

                GameObject followTarget = monsterAI.GetFollowTarget();

                if (followTarget == null ||
                    followTarget != __instance.gameObject)
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    companion.transform.position,
                    __instance.transform.position);

                if (distance > MaximumDefenseDistance)
                {
                    continue;
                }

                Character currentTarget = monsterAI.GetTargetCreature();

                if (currentTarget != null && !currentTarget.IsDead())
                {
                    continue;
                }

                SetTargetMethod(monsterAI, attacker);
                SetAlertedMethod(monsterAI, true);

                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogInfo(
                        $"[Dvergr Companion] Owner defense triggered: " +
                        $"Dvergr={companion.name}, " +
                        $"Owner={__instance.name}, " +
                        $"Attacker={attacker.name}, " +
                        $"Distance={distance:F1}, " +
                        $"CombatTarget={monsterAI.GetTargetCreature()?.name ?? "None"}");
                }
            }
        }
    }
}
