//using System.Collections.Generic;
//using HarmonyLib;
//using HexDvergrAllies.Core.Companions;
//using UnityEngine;

//namespace HexDvergrAllies.Patches
//{
//    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
//    internal static class MonsterAICompanionDiagnosticsPatch
//    {
//        private const float LogInterval = 3f;
//        private const float MinimumLogDistance = 12f;

//        private static readonly Dictionary<MonsterAI, float> NextLogTimes = new Dictionary<MonsterAI, float>();

//        [HarmonyPostfix]
//        private static void Postfix(MonsterAI __instance)
//        {
//            var character = __instance.GetComponent<Character>();

//            if (character == null || !DvergrCompanionManager.IsHired(character))
//            {
//                return;
//            }

//            var followTarget = __instance.GetFollowTarget();

//            if (followTarget == null)
//            {
//                return;
//            }

//            var distance = Vector3.Distance(character.transform.position, followTarget.transform.position);

//            if (distance < MinimumLogDistance)
//            {
//                return;
//            }

//            if (NextLogTimes.TryGetValue(__instance, out var nextLogTime) && Time.time < nextLogTime)
//            {
//                return;
//            }

//            NextLogTimes[__instance] = Time.time + LogInterval;

//            var combatTarget = __instance.GetTargetCreature();
//            var staticTarget = __instance.GetStaticTarget();

//            Plugin.Log.LogInfo($"[Dvergr AI Diagnostics] " + $"Dvergr={character.name}, " + $"Distance={distance:F1}, " + $"Alerted={__instance.IsAlerted()}, " + $"CombatTarget={(combatTarget != null ? combatTarget.name : "None")}, " + $"StaticTarget={(staticTarget != null ? staticTarget.name : "None")}, " + $"FollowTarget={followTarget.name}");
//        }
//    }
//}
