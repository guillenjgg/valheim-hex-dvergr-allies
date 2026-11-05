//using System;
//using System.Collections.Generic;
//using System.Reflection.Emit;
//using HarmonyLib;
//using HexDvergrAllies.Core.Companions;
//using UnityEngine;

//namespace HexDvergrAllies.Patches.Debug
//{
//    [HarmonyPatch]
//    internal static class MonsterAICompanionCombatDebugPatch
//    {
//        private sealed class Snapshot
//        {
//            internal ItemDrop.ItemData SelectedWeapon;
//            internal string Reason;
//            internal ZDOID TargetId;
//            internal float LastMovementTime;
//            internal float NextLogTime;
//            internal float NextAttackLogTime;
//            internal float NextStatusTime;
//        }

//        private static readonly Dictionary<MonsterAI, Snapshot> Snapshots = new Dictionary<MonsterAI, Snapshot>();
//        private static readonly AccessTools.FieldRef<MonsterAI, float> PauseTimer = AccessTools.FieldRefAccess<MonsterAI, float>("m_pauseTimer");

//        [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
//        [HarmonyTranspiler]
//        private static IEnumerable<CodeInstruction> TraceMovementBranches(IEnumerable<CodeInstruction> instructions)
//        {
//            var codes = new List<CodeInstruction>(instructions);
//            var movement = AccessTools.Method(typeof(BaseAI), "RandomMovementArroundPoint");
//            var circleDistance = AccessTools.Field(typeof(MonsterAI), "m_circleTargetDistance");
//            var randomRange = AccessTools.Field(typeof(BaseAI), "m_randomMoveRange");
//            var trace = AccessTools.Method(typeof(MonsterAICompanionCombatDebugPatch), nameof(RecordMovement));
//            var matches = 0;

//            for (var i = 0; i < codes.Count; i++)
//            {
//                if (!codes[i].Calls(movement))
//                {
//                    continue;
//                }

//                string reason = null;
//                for (var j = Math.Max(0, i - 8); j < i; j++)
//                {
//                    if (codes[j].LoadsField(circleDistance)) reason = "PeriodicCircling";
//                    if (codes[j].LoadsField(randomRange)) reason = "WeaponCooldownCircling";
//                }

//                if (reason == null) continue;
//                var loadAI = new CodeInstruction(OpCodes.Ldarg_0);
//                loadAI.labels.AddRange(codes[i].labels);
//                codes[i].labels.Clear();
//                codes.InsertRange(i, new[] { loadAI, new CodeInstruction(OpCodes.Ldstr, reason), new CodeInstruction(OpCodes.Call, trace) });
//                i += 3;
//                matches++;
//            }

//            if (matches != 2) throw new InvalidOperationException($"Combat diagnostics expected two circling branches, found {matches}.");
//            return codes;
//        }

//        private static bool TryGetSnapshot(MonsterAI ai, out Snapshot snapshot)
//        {
//            snapshot = null;
//            if (!HexDvergrAlliesConfiguration.EnableDebugLogs.Value || !ai) return false;
//            var character = ai.GetComponent<Character>();
//            if (!character || !DvergrCompanionState.IsHired(character)) return false;
//            var view = character.GetComponent<ZNetView>();
//            if (!view || !view.IsValid() || !view.IsOwner()) return false;
//            if (!Snapshots.TryGetValue(ai, out snapshot))
//            {
//                snapshot = new Snapshot();
//                Snapshots.Add(ai, snapshot);
//            }
//            return true;
//        }

//        private static void RecordMovement(MonsterAI ai, string reason)
//        {
//            if (!TryGetSnapshot(ai, out var snapshot)) return;
//            snapshot.LastMovementTime = Time.time;
//            if (snapshot.Reason == reason && Time.time < snapshot.NextLogTime) return;
//            snapshot.Reason = reason;
//            snapshot.NextLogTime = Time.time + 2f;
//            LogState(ai, snapshot, reason);
//        }

//        [HarmonyPatch(typeof(MonsterAI), "SelectBestAttack")]
//        [HarmonyPostfix]
//        private static void CaptureSelectedWeapon(MonsterAI __instance, ItemDrop.ItemData __result)
//        {
//            if (TryGetSnapshot(__instance, out var snapshot)) snapshot.SelectedWeapon = __result;
//        }

//        [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
//        [HarmonyPostfix]
//        private static void UpdateSnapshot(MonsterAI __instance)
//        {
//            if (!TryGetSnapshot(__instance, out var snapshot)) return;
//            var target = __instance.GetTargetCreature();
//            var targetId = target ? target.GetZDOID() : ZDOID.None;
//            if (snapshot.TargetId != targetId)
//            {
//                snapshot.TargetId = targetId;
//                LogState(__instance, snapshot, "TargetChanged");
//            }
//            if (snapshot.Reason != null && Time.time - snapshot.LastMovementTime > 0.25f)
//            {
//                LogState(__instance, snapshot, "CirclingEnded:" + snapshot.Reason);
//                snapshot.Reason = null;
//            }
//            if (target && Time.time >= snapshot.NextStatusTime)
//            {
//                snapshot.NextStatusTime = Time.time + 2f;
//                LogState(__instance, snapshot, "CombatStatus");
//            }
//        }

//        [HarmonyPatch(typeof(MonsterAI), "DoAttack")]
//        [HarmonyPostfix]
//        private static void AttackResult(MonsterAI __instance, bool __result)
//        {
//            if (!TryGetSnapshot(__instance, out var snapshot) || Time.time < snapshot.NextAttackLogTime) return;
//            snapshot.NextAttackLogTime = Time.time + 1f;
//            LogState(__instance, snapshot, __result ? "AttackStarted" : "AttackRejected");
//        }

//        private static void LogState(MonsterAI ai, Snapshot snapshot, string reason)
//        {
//            var character = (Humanoid)ai.GetComponent<Character>();
//            var target = ai.GetTargetCreature();
//            var follow = ai.GetFollowTarget();
//            var weapon = snapshot.SelectedWeapon ?? character.GetCurrentWeapon();
//            var multiplier = Mathf.Max(1f, HexDvergrAlliesConfiguration.CompanionAttackSpeedMultiplier.Value);
//            var weaponInterval = weapon != null ? weapon.m_shared.m_aiAttackInterval / multiplier : -1f;
//            var weaponElapsed = weapon != null ? Time.time - weapon.m_lastAttackTime : -1f;
//            var targetDistance = target ? Vector3.Distance(character.transform.position, target.transform.position) - target.GetRadius() : -1f;
//            var ownerDistance = follow ? Vector3.Distance(character.transform.position, follow.transform.position) : -1f;
//            var sentry = DvergrCompanionSentryMode.IsActive(character);
//            var anchorDistance = sentry ? Vector3.Distance(character.transform.position, DvergrCompanionSentryMode.GetAnchor(character)) : ownerDistance;
//            Plugin.Log.LogInfo($"[Dvergr Healing Diagnostic] Time={Time.time:F2}, Dvergr={character.GetZDOID()}, Health={character.GetHealth():F1}, MaxHealth={character.GetMaxHealth():F1}, HealthPercent={character.GetHealthPercentage():F3}, AutoHealingEnabled={HexDvergrAlliesConfiguration.CompanionEnableAutoHealing.Value}, Threshold={HexDvergrAlliesConfiguration.CompanionAutoHealThreshold.Value:F3}, CooldownRemaining={DvergrCompanionBehaviorManager.GetAutoHealCooldownRemaining(character):F2}");
//            Plugin.Log.LogInfo($"[Dvergr Combat Diagnostic] Time={Time.time:F2}, Reason={reason}, Dvergr={character.GetZDOID()}, Target={(target ? target.name : "None")}, TargetId={(target ? target.GetZDOID() : ZDOID.None)}, TargetDistance={targetDistance:F1}, OwnerDistance={ownerDistance:F1}, AnchorDistance={anchorDistance:F1}, Mode={DvergrCompanionModeManager.GetMode(character)}, Weapon={weapon?.m_shared.m_name ?? "None"}, WeaponElapsed={weaponElapsed:F2}, WeaponInterval={weaponInterval:F2}, AttackElapsed={character.GetTimeSinceLastAttack():F2}, MinAttackInterval={ai.m_minAttackInterval:F2}, InAttack={character.InAttack()}, Loaded={character.IsWeaponLoaded()}, MinorAction={character.InMinorAction()}, PauseTimer={PauseTimer(ai):F2}, CircleInterval={ai.m_circleTargetInterval:F2}, CircleDuration={ai.m_circleTargetDuration:F2}, CircleDistance={ai.m_circleTargetDistance:F1}, CirculateWhileCharging={ai.m_circulateWhileCharging}, AttackRange={weapon?.m_shared.m_aiAttackRange}, AttackRangeMin={weapon?.m_shared.m_aiAttackRangeMin}");
//        }

//        [HarmonyPatch(typeof(Character), "OnDestroy")]
//        [HarmonyPostfix]
//        private static void OnCharacterDestroyed(Character __instance)
//        {
//            var ai = __instance.GetComponent<MonsterAI>();
//            if (ai) Snapshots.Remove(ai);
//        }
//    }
//}
