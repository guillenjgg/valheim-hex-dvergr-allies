//using System;
//using System.Collections.Generic;
//using HarmonyLib;
//using HexDvergrAllies.Core.Companions;
//using UnityEngine;

//namespace HexDvergrAllies.Patches.Debug
//{
//    [HarmonyPatch]
//    internal static class MonsterAICompanionDungeonDebugPatch
//    {
//        private static readonly Dictionary<MonsterAI, float> NextLogTimes = new Dictionary<MonsterAI, float>();
//        private static readonly Func<BaseAI, Character, bool> CanSee = AccessTools.MethodDelegate<Func<BaseAI, Character, bool>>(AccessTools.Method(typeof(BaseAI), "CanSeeTarget", new[] { typeof(Character) }));
//        private static readonly Func<BaseAI, Character, bool> CanHear = AccessTools.MethodDelegate<Func<BaseAI, Character, bool>>(AccessTools.Method(typeof(BaseAI), "CanHearTarget", new[] { typeof(Character) }));
//        private static readonly Func<BaseAI, Vector3, bool> HavePath = AccessTools.MethodDelegate<Func<BaseAI, Vector3, bool>>(AccessTools.Method(typeof(BaseAI), "HavePath"));
//        private static readonly Func<BaseAI, ItemDrop.ItemData, bool> CanUseAttack = AccessTools.MethodDelegate<Func<BaseAI, ItemDrop.ItemData, bool>>(AccessTools.Method(typeof(BaseAI), "CanUseAttack", new[] { typeof(ItemDrop.ItemData) }));
//        private static readonly AccessTools.FieldRef<BaseAI, float> ViewRange = AccessTools.FieldRefAccess<BaseAI, float>("m_viewRange");
//        private static readonly AccessTools.FieldRef<BaseAI, float> ViewAngle = AccessTools.FieldRefAccess<BaseAI, float>("m_viewAngle");
//        private static readonly AccessTools.FieldRef<BaseAI, Pathfinding.AgentType> PathAgent = AccessTools.FieldRefAccess<BaseAI, Pathfinding.AgentType>("m_pathAgentType");
//        private static readonly AccessTools.FieldRef<Character, Transform> Eye = AccessTools.FieldRefAccess<Character, Transform>("m_eye");
//        private static readonly AccessTools.FieldRef<MonsterAI, float> UnseenTime = AccessTools.FieldRefAccess<MonsterAI, float>("m_timeSinceSensedTargetCreature");
//        private static readonly AccessTools.FieldRef<MonsterAI, float> UnreachableTimer = AccessTools.FieldRefAccess<MonsterAI, float>("m_unableToAttackTargetTimer");

//        [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
//        [HarmonyPostfix]
//        private static void TraceDungeonCombat(MonsterAI __instance)
//        {
//            if (!HexDvergrAlliesConfiguration.EnableDebugLogs.Value) return;
//            var character = __instance.GetComponent<Character>();
//            if (!character || !character.InInterior() || !DvergrCompanionState.IsHired(character)) return;
//            var view = character.GetComponent<ZNetView>();
//            if (!view || !view.IsValid() || !view.IsOwner()) return;
//            if (NextLogTimes.TryGetValue(__instance, out var nextLog) && Time.time < nextLog) return;
//            NextLogTimes[__instance] = Time.time + 2f;

//            var target = __instance.GetTargetCreature();
//            Character candidate = null;
//            var nearest = 30f;
//            foreach (var other in Character.GetAllCharacters())
//            {
//                if (!other || other == character || other.IsDead() || !other.InInterior() || !BaseAI.IsEnemy(character, other)) continue;
//                var distance = Vector3.Distance(character.transform.position, other.transform.position);
//                if (distance >= nearest) continue;
//                candidate = other;
//                nearest = distance;
//            }

//            Plugin.Log.LogInfo($"[Dvergr Dungeon Diagnostic] Time={Time.time:F2}, Dvergr={character.GetZDOID()}, Position={character.transform.position}, Mode={DvergrCompanionModeManager.GetMode(character)}, Target={(target ? target.name : "None")}, Candidate={(candidate ? candidate.name : "None")}, Alerted={__instance.IsAlerted()}, InAttack={character.InAttack()}, PathAgent={PathAgent(__instance)}, ViewRange={ViewRange(__instance):F1}, ViewAngle={ViewAngle(__instance):F1}, UnseenTime={UnseenTime(__instance):F2}, UnreachableTimer={UnreachableTimer(__instance):F2}");
//            if (target) LogCandidate(__instance, character, target, "CurrentTarget");
//            if (candidate && candidate != target) LogCandidate(__instance, character, candidate, "NearestEnemy");
//        }

//        private static void LogCandidate(MonsterAI ai, Character character, Character candidate, string role)
//        {
//            var eye = Eye(character);
//            var targetEye = Eye(candidate);
//            var start = eye ? eye.position : character.GetCenterPoint();
//            var end = !candidate.IsCrouching() && targetEye ? targetEye.position : candidate.GetCenterPoint();
//            var mask = AccessTools.StaticFieldRefAccess<int>(typeof(BaseAI), "m_viewBlockMask");
//            var eyeBlocked = Physics.Linecast(start, end, out var eyeHit, mask);
//            var centerBlocked = Physics.Linecast(character.GetCenterPoint(), candidate.GetCenterPoint(), out var centerHit, mask);
//            var weapon = (character as Humanoid)?.GetCurrentWeapon();
//            if (role == "CurrentTarget" && weapon == null && character is Humanoid humanoid)
//            {
//                var effectiveDistance = Vector3.Distance(character.transform.position, candidate.transform.position) - candidate.GetRadius();
//                Plugin.Log.LogInfo($"[Dvergr Dungeon Weapon Selection] Time={Time.time:F2}, Dvergr={character.GetZDOID()}, Target={candidate.GetZDOID()}, TargetRadius={candidate.GetRadius():F2}, EffectiveDistance={effectiveDistance:F2}, TargetAttached={candidate.IsAttached()}, InAttack={character.InAttack()}, Staggering={character.IsStaggering()}, MinorAction={character.InMinorAction()}, InventoryCount={humanoid.GetInventory().GetAllItems().Count}");
//                foreach (var item in humanoid.GetInventory().GetAllItems())
//                {
//                    if (!item.IsWeapon()) continue;
//                    var elapsed = Time.time - item.m_lastAttackTime;
//                    Plugin.Log.LogInfo($"[Dvergr Dungeon Weapon Candidate] Time={Time.time:F2}, Dvergr={character.GetZDOID()}, Weapon={item.m_shared.m_name}, CanUseAttack={CanUseAttack(ai, item)}, TargetType={item.m_shared.m_aiTargetType}, EffectiveDistance={effectiveDistance:F2}, MinRange={item.m_shared.m_aiAttackRangeMin:F2}, MaxRange={item.m_shared.m_aiAttackRange:F2}, BelowMin={effectiveDistance < item.m_shared.m_aiAttackRangeMin}, Elapsed={elapsed:F2}, VanillaInterval={item.m_shared.m_aiAttackInterval:F2}, Walking={item.m_shared.m_aiWhenWalking}, Swimming={item.m_shared.m_aiWhenSwiming}, Flying={item.m_shared.m_aiWhenFlying}");
//                }
//            }
//            Plugin.Log.LogInfo($"[Dvergr Dungeon Target] Time={Time.time:F2}, Role={role}, Dvergr={character.GetZDOID()}, Enemy={candidate.name}, EnemyId={candidate.GetZDOID()}, Distance={Vector3.Distance(character.transform.position, candidate.transform.position):F2}, CanSee={CanSee(ai, candidate)}, CanHear={CanHear(ai, candidate)}, HavePath={HavePath(ai, candidate.transform.position)}, Angle={Vector3.Angle(candidate.transform.position - character.transform.position, character.transform.forward):F1}, Stealth={candidate.GetStealthFactor():F2}, Noise={candidate.GetNoiseRange():F1}, EyeStart={start}, EyeEnd={end}, EyeBlocker={DescribeHit(eyeBlocked, eyeHit)}, CenterBlocker={DescribeHit(centerBlocked, centerHit)}, Weapon={weapon?.m_shared.m_name ?? "None"}, AttackRange={weapon?.m_shared.m_aiAttackRange}, AttackRangeMin={weapon?.m_shared.m_aiAttackRangeMin}");
//        }

//        [HarmonyPatch(typeof(DvergrCompanionDungeonTargeting), "SelectTarget")]
//        [HarmonyPrefix]
//        private static void CapturePreviousTarget(MonsterAI ai, out Character __state)
//        {
//            __state = HexDvergrAlliesConfiguration.EnableDebugLogs.Value ? ai.GetTargetCreature() : null;
//        }

//        [HarmonyPatch(typeof(DvergrCompanionDungeonTargeting), "SelectTarget")]
//        [HarmonyPostfix]
//        private static void TraceTargetDecision(MonsterAI ai, Character __state, Character __result)
//        {
//            if (!__state || __state == __result || !HexDvergrAlliesConfiguration.EnableDebugLogs.Value) return;
//            if (!__result && ai.GetTargetCreature() == __state) return;
//            var character = ai.GetComponent<Character>();
//            if (!character || !character.InInterior() || !DvergrCompanionState.IsHired(character)) return;
//            Plugin.Log.LogInfo($"[Dvergr Dungeon Decision] Time={Time.time:F2}, Dvergr={character.GetZDOID()}, Previous={__state.name}, PreviousId={__state.GetZDOID()}, PreviousDead={__state.IsDead()}, Replacement={(__result ? __result.name : "None")}");
//            if (!__state.IsDead()) LogCandidate(ai, character, __state, "ReleasedTarget");
//        }

//        private static string DescribeHit(bool blocked, RaycastHit hit)
//        {
//            if (!blocked || !hit.collider) return "Clear";
//            var collider = hit.collider;
//            return $"{collider.name}/Parent={(collider.transform.parent ? collider.transform.parent.name : "None")}/Layer={LayerMask.LayerToName(collider.gameObject.layer)}/Distance={hit.distance:F2}";
//        }

//        [HarmonyPatch(typeof(Character), "OnDestroy")]
//        [HarmonyPostfix]
//        private static void OnCharacterDestroyed(Character __instance)
//        {
//            var ai = __instance.GetComponent<MonsterAI>();
//            if (ai) NextLogTimes.Remove(ai);
//        }
//    }
//}
