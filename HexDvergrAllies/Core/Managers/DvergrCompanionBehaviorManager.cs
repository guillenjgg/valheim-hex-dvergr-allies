using System.Collections.Generic;

namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrCompanionBehaviorManager
    {
        private class MovementSpeedState
        {
            internal float OriginalSpeed;
            internal float OriginalWalkSpeed;
            internal float OriginalRunSpeed;
            internal float AppliedMultiplier;
        }

        private static readonly Dictionary<Character, MovementSpeedState> OriginalMovementSpeeds = new Dictionary<Character, MovementSpeedState>();

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
                return;
            }

            ApplyMovementSpeed(character);
        }

        private static void ApplyMovementSpeed(Character character)
        {
            var multiplier = HexDvergrAlliesConfiguration.CompanionMovementSpeedMultiplier.Value;

            if (!OriginalMovementSpeeds.TryGetValue(character, out var state))
            {
                state = new MovementSpeedState
                {
                    OriginalSpeed = character.m_speed,
                    OriginalWalkSpeed = character.m_walkSpeed,
                    OriginalRunSpeed = character.m_runSpeed,
                    AppliedMultiplier = 1f
                };

                OriginalMovementSpeeds.Add(character, state);
            }

            if (state.AppliedMultiplier == multiplier)
            {
                return;
            }

            character.m_speed = state.OriginalSpeed * multiplier;
            character.m_walkSpeed = state.OriginalWalkSpeed * multiplier;
            character.m_runSpeed = state.OriginalRunSpeed * multiplier;

            state.AppliedMultiplier = multiplier;
        }
    }
}