using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionDungeonTargeting
    {
        private sealed class TargetState
        {
            internal Character Target;
            internal float NextEvaluation;
            internal float LastActionableTime;
        }

        private const float EvaluationInterval = 0.5f;
        private const float PursuitGracePeriod = 3f;
        private static readonly Dictionary<Character, TargetState> States = new Dictionary<Character, TargetState>();
        private static readonly Func<BaseAI, Character, bool> CanSense = AccessTools.MethodDelegate<Func<BaseAI, Character, bool>>(AccessTools.Method(typeof(BaseAI), "CanSenseTarget", new[] { typeof(Character) }));
        private static readonly Func<BaseAI, Character, bool> CanSee = AccessTools.MethodDelegate<Func<BaseAI, Character, bool>>(AccessTools.Method(typeof(BaseAI), "CanSeeTarget", new[] { typeof(Character) }));
        private static readonly Func<BaseAI, Vector3, bool> HavePath = AccessTools.MethodDelegate<Func<BaseAI, Vector3, bool>>(AccessTools.Method(typeof(BaseAI), "HavePath"));
        private static readonly AccessTools.FieldRef<MonsterAI, Character> TargetField = AccessTools.FieldRefAccess<MonsterAI, Character>("m_targetCreature");
        private static readonly AccessTools.FieldRef<Character, bool> SkipTarget = AccessTools.FieldRefAccess<Character, bool>("m_aiSkipTarget");
        private static readonly AccessTools.FieldRef<BaseAI, float> ViewRange = AccessTools.FieldRefAccess<BaseAI, float>("m_viewRange");

        internal static Character SelectTarget(MonsterAI ai, Character vanillaTarget)
        {
            var character = ai.GetComponent<Character>();
            var view = character ? character.GetComponent<ZNetView>() : null;

            if (!character || !character.InInterior() || !DvergrCompanionState.IsHired(character) || !view || !view.IsValid() || !view.IsOwner() || !HexDvergrAlliesConfiguration.CompanionEnableDungeonTargetFiltering.Value)
            {
                if (character)
                {
                    States.Remove(character);
                }

                return vanillaTarget;
            }

            if (!States.TryGetValue(character, out var state))
            {
                state = new TargetState();
                States.Add(character, state);
            }

            var current = ai.GetTargetCreature();
            // Owner defense can assign a target between scans. Never replace that
            // decision with a cached null or a different cached target.
            if (current == state.Target && Time.time < state.NextEvaluation && (!state.Target || IsValidEnemy(ai, character, state.Target)))
            {
                return state.Target;
            }

            state.NextEvaluation = Time.time + EvaluationInterval;

            // Keep an actionable target instead of switching to every new noise in adjacent rooms.
            if (IsValidEnemy(ai, character, current))
            {
                if (character.InAttack() || (CanSense(ai, current) && IsActionable(ai, character, current)))
                {
                    state.Target = current;
                    state.LastActionableTime = Time.time;

                    return current;
                }

                if (state.Target == current && Time.time - state.LastActionableTime < PursuitGracePeriod)
                {
                    return current;
                }
            }

            // FindEnemy returning null alone does not clear MonsterAI's existing target.
            if (current)
            {
                TargetField(ai) = null;
            }

            state.Target = null;
            var nearest = Mathf.Max(ViewRange(ai), 12f);

            foreach (var candidate in Character.GetAllCharacters())
            {
                if (!IsValidEnemy(ai, character, candidate) || !CanSense(ai, candidate))
                {
                    continue;
                }

                var distance = Vector3.Distance(character.transform.position, candidate.transform.position);

                if (distance >= nearest || !IsActionable(ai, character, candidate))
                {
                    continue;
                }

                nearest = distance;
                state.Target = candidate;
                state.LastActionableTime = Time.time;
            }

            return state.Target;
        }

        private static bool IsValidEnemy(MonsterAI ai, Character character, Character target)
        {
            if (!target || target == character || target.IsDead() || SkipTarget(target) || !target.InInterior() || !BaseAI.IsEnemy(character, target))
            {
                return false;
            }

            var targetAI = target.GetBaseAI();

            if (targetAI && targetAI.IsSleeping())
            {
                return false;
            }

            if (DvergrCompanionSentryMode.IsActive(character) && !DvergrCompanionSentryMode.IsWithinCombatRadius(character, target))
            {
                return false;
            }

            return true;
        }

        private static bool IsActionable(MonsterAI ai, Character character, Character target)
        {
            var distance = Vector3.Distance(character.transform.position, target.transform.position) - target.GetRadius();
            var humanoid = character as Humanoid;

            if (humanoid && CanSee(ai, target))
            {
                // EquipBestWeapon can switch between the club and arbalest after acquisition.
                // The currently equipped club must not disqualify a valid ranged target.
                foreach (var weapon in humanoid.GetInventory().GetAllItems())
                {
                    if (weapon.m_shared.m_aiTargetType != ItemDrop.ItemData.AiTarget.Enemy)
                    {
                        continue;
                    }

                    if (distance < weapon.m_shared.m_aiAttackRange && distance >= weapon.m_shared.m_aiAttackRangeMin)
                    {
                        return true;
                    }
                }
            }

            return HavePath(ai, target.transform.position);
        }

        internal static void RemoveCharacter(Character character)
        {
            States.Remove(character);
        }
    }
}
