using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrCompanionBehaviorManager
    {
        private const string OriginalMaxHealthKey = "HexDvergr_OriginalMaxHealth";
        private const string OriginalMaxHealthSavedKey = "HexDvergr_OriginalMaxHealthSaved";

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

        private static readonly Dictionary<Character, CompanionBehaviorState> CompanionStates =
            new Dictionary<Character, CompanionBehaviorState>();

        private static readonly AccessTools.FieldRef<Character, Animator> AnimatorField =
    AccessTools.FieldRefAccess<Character, Animator>("m_animator");

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

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Original stats cached: Dvergr={character.name}, Speed={state.OriginalSpeed}, WalkSpeed={state.OriginalWalkSpeed}, RunSpeed={state.OriginalRunSpeed}, AttackInterval={state.OriginalMinAttackInterval}, MaxHealth={state.OriginalMaxHealth}");
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
            }

            var multiplier = HexDvergrAlliesConfiguration.CompanionAttackSpeedMultiplier.Value;

            if (multiplier <= 0f)
            {
                multiplier = 1f;
            }

            var targetMultiplier = humanoid.InAttack() ? multiplier : 1f;

            if (Mathf.Approximately(state.AppliedAnimationMultiplier, targetMultiplier))
            {
                return;
            }

            syncAnimation.SetSpeed(state.OriginalAnimationSpeed * targetMultiplier);

            state.AppliedAnimationMultiplier = targetMultiplier;

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo(
                    $"[Dvergr Companion] Animation speed changed: Dvergr={character.name}, InAttack={humanoid.InAttack()}, Multiplier={targetMultiplier:F2}, Speed={animator.speed:F2}");
            }
        }

        private static void RestoreAttackAnimation(Character character, CompanionBehaviorState state)
        {
            if (!state.AnimationSpeedCaptured ||
                Mathf.Approximately(state.AppliedAnimationMultiplier, 1f))
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

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Auto-healed: Dvergr={character.name}, PreviousHealth={currentHealth:F1}, MaxHealth={maxHealth:F1}, Cooldown={cooldown:F1}s");
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

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Original stats restored: Dvergr={character.name}, MaxHealth={originalMaxHealth}");
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
    }
}
