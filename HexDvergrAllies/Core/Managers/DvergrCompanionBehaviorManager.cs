using System.Collections.Generic;
using UnityEngine;

namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrCompanionBehaviorManager
    {
        private class CompanionBehaviorState
        {
            internal float OriginalSpeed;
            internal float OriginalWalkSpeed;
            internal float OriginalRunSpeed;
            internal float OriginalMinAttackInterval;
            internal float AppliedMovementMultiplier = 1f;
            internal float AppliedAttackSpeedMultiplier = 1f;
        }

        private static readonly Dictionary<Character, CompanionBehaviorState> CompanionStates =
            new Dictionary<Character, CompanionBehaviorState>();

        internal static void UpdateBehavior(MonsterAI monsterAI)
        {
            if (monsterAI == null)
            {
                return;
            }

            var character = monsterAI.GetComponent<Character>();

            if (character == null || !DvergrCompanionManager.IsHired(character))
            {
                return;
            }

            if (!CompanionStates.TryGetValue(character, out var state))
            {
                state = new CompanionBehaviorState
                {
                    OriginalSpeed = character.m_speed,
                    OriginalWalkSpeed = character.m_walkSpeed,
                    OriginalRunSpeed = character.m_runSpeed,
                    OriginalMinAttackInterval = monsterAI.m_minAttackInterval
                };

                CompanionStates.Add(character, state);
            }

            ApplyMovementSpeed(character, state);
            ApplyAttackSpeed(monsterAI, state);

            var netView = character.GetComponent<ZNetView>();

            if (netView != null && netView.IsValid() && netView.IsOwner())
            {
                ApplyMaximumHealth(character);
            }
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

            if (state.AppliedAttackSpeedMultiplier == multiplier)
            {
                return;
            }

            monsterAI.m_minAttackInterval = state.OriginalMinAttackInterval / multiplier;

            state.AppliedAttackSpeedMultiplier = multiplier;
        }

        private static void ApplyMaximumHealth(Character character)
        {
            var multiplier = HexDvergrAlliesConfiguration.CompanionHealthMultiplier.Value;
            var originalMaxHealth = character.GetMaxHealthBase() * character.GetLevel();
            var targetMaxHealth = originalMaxHealth * multiplier;
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
            if (CompanionStates.TryGetValue(character, out var state))
            {
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

            var originalMaxHealth = character.GetMaxHealthBase() * character.GetLevel();
            var currentMaxHealth = character.GetMaxHealth();

            if (Mathf.Approximately(currentMaxHealth, originalMaxHealth))
            {
                return;
            }

            var healthPercentage = currentMaxHealth > 0f
                ? character.GetHealth() / currentMaxHealth
                : 1f;

            character.SetMaxHealth(originalMaxHealth);
            character.SetHealth(Mathf.Clamp(originalMaxHealth * healthPercentage, 0f, originalMaxHealth));
        }

        internal static void RemoveCharacter(Character character)
        {
            CompanionStates.Remove(character);
        }
    }
}