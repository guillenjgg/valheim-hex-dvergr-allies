using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrCompanionBehaviorManager
    {
        private const string OriginalMaxHealthKey = "HexDvergr_OriginalMaxHealth";
        private const string OriginalMaxHealthSavedKey = "HexDvergr_OriginalMaxHealthSaved";
        private const float CompanionCombatLeashDistance = 15f;

        private class CompanionBehaviorState
        {
            internal float OriginalSpeed;
            internal float OriginalWalkSpeed;
            internal float OriginalRunSpeed;
            internal float OriginalMinAttackInterval;
            internal float OriginalMaxHealth;
            internal float OriginalAnimationSpeed;

            internal float AppliedMovementMultiplier = 1f;
            internal float AppliedAttackSpeedMultiplier = 1f;
            internal float AppliedAnimationMultiplier = 1f;
            internal float NextAutoHealTime;
            internal bool AnimationSpeedCaptured;
        }

        private static readonly Dictionary<Character, CompanionBehaviorState> CompanionStates = new Dictionary<Character, CompanionBehaviorState>();
        private static readonly AccessTools.FieldRef<Character, Animator> AnimatorField = AccessTools.FieldRefAccess<Character, Animator>("m_animator");
        private static readonly AccessTools.FieldRef<MonsterAI, Character> CombatTargetField = AccessTools.FieldRefAccess<MonsterAI, Character>("m_targetCreature");
        private static readonly System.Action<BaseAI, bool> SetAlertedMethod = AccessTools.MethodDelegate<System.Action<BaseAI, bool>>(AccessTools.Method(typeof(BaseAI), "SetAlerted", new[] { typeof(bool) }));

        internal static void CaptureState(Character character, MonsterAI monsterAI)
        {
            if (CompanionStates.ContainsKey(character))
            {
                return;
            }

            var state = new CompanionBehaviorState
            {
                OriginalSpeed = character.m_speed,
                OriginalWalkSpeed = character.m_walkSpeed,
                OriginalRunSpeed = character.m_runSpeed,
                OriginalMinAttackInterval = monsterAI.m_minAttackInterval,
                OriginalMaxHealth = character.GetMaxHealth()
            };

            CompanionStates.Add(character, state);

            var netView = character.GetComponent<ZNetView>();

            if (netView != null && netView.IsValid() && netView.IsOwner())
            {
                var zdo = netView.GetZDO();

                if (!zdo.GetBool(OriginalMaxHealthSavedKey, false))
                {
                    zdo.Set(OriginalMaxHealthKey, state.OriginalMaxHealth);
                    zdo.Set(OriginalMaxHealthSavedKey, true);
                }
            }

            Plugin.Log.LogInfo($"[Dvergr Companion] Original stats cached: Dvergr={character.name}, Speed={state.OriginalSpeed}, WalkSpeed={state.OriginalWalkSpeed}, RunSpeed={state.OriginalRunSpeed}, AttackInterval={state.OriginalMinAttackInterval}, MaxHealth={state.OriginalMaxHealth}");
        }

        internal static void UpdateCombatLeash(MonsterAI monsterAI)
        {
            if (monsterAI == null)
            {
                return;
            }

            Character character = monsterAI.GetComponent<Character>();

            if (character == null || !DvergrCompanionManager.IsHired(character))
            {
                return;
            }

            ZNetView netView = character.GetComponent<ZNetView>();

            if (netView == null || !netView.IsValid() || !netView.IsOwner())
            {
                return;
            }

            GameObject followTarget = monsterAI.GetFollowTarget();

            if (followTarget == null)
            {
                return;
            }

            Character combatTarget = CombatTargetField(monsterAI);

            if (combatTarget == null)
            {
                return;
            }

            float companionDistance = Vector3.Distance(
                followTarget.transform.position,
                character.transform.position);

            if (companionDistance <= CompanionCombatLeashDistance)
            {
                return;
            }

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo(
                    $"[Dvergr Companion] Combat leash exceeded: " +
                    $"Dvergr={character.name}, " +
                    $"Target={combatTarget.name}, " +
                    $"CompanionDistanceFromOwner={companionDistance:F1}, " +
                    $"LeashDistance={CompanionCombatLeashDistance:F1}");
            }
        }

        internal static void UpdateBehavior(MonsterAI monsterAI)
        {
            if (monsterAI == null)
            {
                return;
            }

            var character = monsterAI.GetComponent<Character>();

            if (character == null)
            {
                return;
            }

            if (!DvergrCompanionManager.IsHired(character))
            {
                if (CompanionStates.ContainsKey(character))
                {
                    ResetBehavior(character, monsterAI);
                }

                return;
            }

            CaptureState(character, monsterAI);

            var state = CompanionStates[character];

            ApplyMovementSpeed(character, state);
            ApplyAttackSpeed(monsterAI, state);

            var netView = character.GetComponent<ZNetView>();

            if (netView != null && netView.IsValid() && netView.IsOwner())
            {
                ApplyMaximumHealth(character, state);
                ApplyAutoHealing(character, state);
            }
        }

        internal static void UpdateAttackAnimation(Humanoid humanoid)
        {
            if (humanoid == null)
            {
                return;
            }

            var character = (Character)humanoid;
            CompanionBehaviorState state;

            if (!CompanionStates.TryGetValue(character, out state))
            {
                return;
            }

            var netView = character.GetComponent<ZNetView>();

            if (netView == null || !netView.IsValid() || !netView.IsOwner())
            {
                return;
            }

            var animator = AnimatorField(character);

            if (!DvergrCompanionManager.IsHired(character))
            {
                RestoreAttackAnimation(character, state);
                return;
            }

            var syncAnimation = character.GetComponent<ZSyncAnimation>();

            if (syncAnimation == null || animator == null)
            {
                return;
            }

            if (!state.AnimationSpeedCaptured)
            {
                state.OriginalAnimationSpeed = animator.speed;
                state.AnimationSpeedCaptured = true;

                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogInfo($"[Dvergr Companion] Animation initialized: Dvergr={character.name}, OriginalSpeed={state.OriginalAnimationSpeed:F2}");
                }
            }

            var multiplier = HexDvergrAlliesConfiguration.CompanionAttackSpeedMultiplier.Value;

            if (multiplier <= 0f)
            {
                multiplier = 1f;
            }

            var inAttack = humanoid.InAttack();
            var targetMultiplier = inAttack ? multiplier : 1f;

            if (Mathf.Approximately(state.AppliedAnimationMultiplier, targetMultiplier))
            {
                return;
            }

            var previousSpeed = animator.speed;
            var targetSpeed = state.OriginalAnimationSpeed * targetMultiplier;

            syncAnimation.SetSpeed(targetSpeed);

            state.AppliedAnimationMultiplier = targetMultiplier;

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo($"[Dvergr Companion] Animation speed changed: Dvergr={character.name}, InAttack={inAttack}, Multiplier={targetMultiplier:F2}, OriginalSpeed={state.OriginalAnimationSpeed:F2}, PreviousSpeed={previousSpeed:F2}, TargetSpeed={targetSpeed:F2}, ActualSpeed={animator.speed:F2}");
            }
        }

        internal static bool ShouldSuppressAutomaticTargeting(BaseAI baseAI)
        {
            if (baseAI == null)
            {
                return false;
            }

            MonsterAI monsterAI = baseAI as MonsterAI;

            if (monsterAI == null)
            {
                return false;
            }

            Character character = monsterAI.GetComponent<Character>();

            if (!character || !DvergrCompanionManager.IsHired(character))
            {
                return false;
            }

            ZNetView netView = character.GetComponent<ZNetView>();

            return netView != null && netView.IsValid() && netView.IsOwner();
        }

        internal static void ResetBehavior(Character character, MonsterAI monsterAI)
        {
            CompanionBehaviorState state;

            if (CompanionStates.TryGetValue(character, out state))
            {
                RestoreAttackAnimation(character, state);

                character.m_speed = state.OriginalSpeed;
                character.m_walkSpeed = state.OriginalWalkSpeed;
                character.m_runSpeed = state.OriginalRunSpeed;

                if (monsterAI != null)
                {
                    monsterAI.m_minAttackInterval = state.OriginalMinAttackInterval;
                }

                CompanionStates.Remove(character);
            }

            var netView = character.GetComponent<ZNetView>();

            if (netView == null || !netView.IsValid() || !netView.IsOwner())
            {
                return;
            }

            var zdo = netView.GetZDO();

            if (!zdo.GetBool(OriginalMaxHealthSavedKey, false))
            {
                return;
            }

            var originalMaxHealth = zdo.GetFloat(OriginalMaxHealthKey, character.GetMaxHealth());

            SetMaximumHealthPreservingPercentage(character, originalMaxHealth);

            zdo.Set(OriginalMaxHealthSavedKey, false);

            Plugin.Log.LogInfo($"[Dvergr Companion] Original stats restored: Dvergr={character.name}, MaxHealth={originalMaxHealth}");
        }

        internal static void RemoveCharacter(Character character)
        {
            CompanionBehaviorState state;

            if (CompanionStates.TryGetValue(character, out state))
            {
                RestoreAttackAnimation(character, state);
                CompanionStates.Remove(character);
            }
        }

        private static void RestoreAttackAnimation(Character character, CompanionBehaviorState state)
        {
            if (!state.AnimationSpeedCaptured || Mathf.Approximately(state.AppliedAnimationMultiplier, 1f))
            {
                return;
            }

            var netView = character.GetComponent<ZNetView>();

            if (netView == null || !netView.IsValid() || !netView.IsOwner())
            {
                return;
            }

            var syncAnimation = character.GetComponent<ZSyncAnimation>();

            if (syncAnimation == null)
            {
                return;
            }

            syncAnimation.SetSpeed(state.OriginalAnimationSpeed);

            state.AppliedAnimationMultiplier = 1f;
        }

        private static void ApplyMovementSpeed(Character character, CompanionBehaviorState state)
        {
            var multiplier = HexDvergrAlliesConfiguration.CompanionMovementSpeedMultiplier.Value;

            if (state.AppliedMovementMultiplier == multiplier)
            {
                return;
            }

            character.m_speed = state.OriginalSpeed * multiplier;
            character.m_walkSpeed = state.OriginalWalkSpeed * multiplier;
            character.m_runSpeed = state.OriginalRunSpeed * multiplier;

            state.AppliedMovementMultiplier = multiplier;

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo($"[Dvergr Companion] Movement speed applied: Dvergr={character.name}, Multiplier={multiplier:F2}, OriginalSpeed={state.OriginalSpeed:F2}, AppliedSpeed={character.m_speed:F2}, OriginalWalkSpeed={state.OriginalWalkSpeed:F2}, AppliedWalkSpeed={character.m_walkSpeed:F2}, OriginalRunSpeed={state.OriginalRunSpeed:F2}, AppliedRunSpeed={character.m_runSpeed:F2}");
            }
        }

        private static void ApplyAttackSpeed(MonsterAI monsterAI, CompanionBehaviorState state)
        {
            var multiplier = HexDvergrAlliesConfiguration.CompanionAttackSpeedMultiplier.Value;

            if (multiplier <= 0f || state.AppliedAttackSpeedMultiplier == multiplier)
            {
                return;
            }

            monsterAI.m_minAttackInterval = state.OriginalMinAttackInterval / multiplier;

            state.AppliedAttackSpeedMultiplier = multiplier;

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo($"[Dvergr Companion] Attack interval applied: Dvergr={monsterAI.name}, OriginalInterval={state.OriginalMinAttackInterval:F2}, Multiplier={multiplier:F2}, AppliedInterval={monsterAI.m_minAttackInterval:F2}");
            }
        }

        private static void ApplyMaximumHealth(Character character, CompanionBehaviorState state)
        {
            var multiplier = HexDvergrAlliesConfiguration.CompanionHealthMultiplier.Value;
            var originalMaxHealth = GetOriginalMaxHealth(character, state);
            var targetMaxHealth = originalMaxHealth * multiplier;
            var currentMaxHealth = character.GetMaxHealth();

            if (Mathf.Approximately(currentMaxHealth, targetMaxHealth))
            {
                return;
            }

            SetMaximumHealthPreservingPercentage(character, targetMaxHealth);
        }

        private static void ApplyAutoHealing(Character character, CompanionBehaviorState state)
        {
            if (!HexDvergrAlliesConfiguration.CompanionEnableAutoHealing.Value)
            {
                return;
            }

            var maxHealth = character.GetMaxHealth();

            if (maxHealth <= 0f)
            {
                return;
            }

            var currentHealth = character.GetHealth();

            if (currentHealth <= 0f)
            {
                return;
            }

            var threshold = HexDvergrAlliesConfiguration.CompanionAutoHealThreshold.Value;

            if (currentHealth / maxHealth > threshold)
            {
                return;
            }

            if (Time.time < state.NextAutoHealTime)
            {
                return;
            }

            character.SetHealth(maxHealth);

            var cooldown = HexDvergrAlliesConfiguration.CompanionAutoHealCooldown.Value;
            state.NextAutoHealTime = Time.time + cooldown;

            Plugin.Log.LogInfo($"[Dvergr Companion] Auto-healed: Dvergr={character.name}, PreviousHealth={currentHealth:F1}, MaxHealth={maxHealth:F1}, Cooldown={cooldown:F1}s");
        }

        private static float GetOriginalMaxHealth(Character character, CompanionBehaviorState state)
        {
            var netView = character.GetComponent<ZNetView>();

            if (netView != null && netView.IsValid())
            {
                var zdo = netView.GetZDO();

                if (zdo.GetBool(OriginalMaxHealthSavedKey, false))
                {
                    return zdo.GetFloat(OriginalMaxHealthKey, state.OriginalMaxHealth);
                }
            }

            return state.OriginalMaxHealth;
        }

        private static void SetMaximumHealthPreservingPercentage(Character character, float targetMaxHealth)
        {
            var currentMaxHealth = character.GetMaxHealth();

            if (Mathf.Approximately(currentMaxHealth, targetMaxHealth))
            {
                return;
            }

            var healthPercentage = currentMaxHealth > 0f
                ? character.GetHealth() / currentMaxHealth
                : 1f;

            character.SetMaxHealth(targetMaxHealth);
            character.SetHealth(Mathf.Clamp(targetMaxHealth * healthPercentage, 0f, targetMaxHealth));
        }
    }
}
