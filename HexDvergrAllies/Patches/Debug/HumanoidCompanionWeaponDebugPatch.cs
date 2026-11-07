using HexDvergrAllies.Core;
using System.Collections.Generic;
using HarmonyLib;
using HexDvergrAllies.Core.Companions;
using UnityEngine;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(MonsterAI), "UpdateTarget")]
    internal static class MonsterAICompanionAttackConditionsDebugPatch
    {
        private static readonly AccessTools.FieldRef<MonsterAI, Character> CharacterField = AccessTools.FieldRefAccess<MonsterAI, Character>("m_character");
        private static readonly AccessTools.FieldRef<MonsterAI, Character> TargetField = AccessTools.FieldRefAccess<MonsterAI, Character>("m_targetCreature");
        private static readonly AccessTools.FieldRef<MonsterAI, float> AttackIntervalField = AccessTools.FieldRefAccess<MonsterAI, float>("m_minAttackInterval");
        private static readonly Dictionary<MonsterAI, float> LastLogTimes = new Dictionary<MonsterAI, float>();

        private static void Postfix(MonsterAI __instance, bool canHearTarget, bool canSeeTarget)
        {
            var character = CharacterField(__instance);

            if (!character || !DvergrCompanionState.IsHired(character))
            {
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var target = TargetField(__instance);

            if (!target)
            {
                return;
            }

            if (LastLogTimes.TryGetValue(__instance, out var lastLogTime) && Time.time - lastLogTime < 1f)
            {
                return;
            }

            LastLogTimes[__instance] = Time.time;

            var humanoid = character as Humanoid;
            var weapon = humanoid.GetCurrentWeapon();
            var distance = Vector3.Distance(character.transform.position, target.transform.position) - target.GetRadius();

            if (weapon == null)
            {
                HexLog.LogInfo($"[Attack Conditions] Dvergr={character.name}, Target={target.name}, Distance={distance:F1}, CanSee={canSeeTarget}, CanHear={canHearTarget}, Alerted={__instance.IsAlerted()}, Weapon=None");
                return;
            }

            var shared = weapon.m_shared;
            var weaponCooldownReady = Time.time - weapon.m_lastAttackTime > shared.m_aiAttackInterval;
            var characterCooldownReady = character.GetTimeSinceLastAttack() >= AttackIntervalField(__instance);
            var inRange = distance < shared.m_aiAttackRange;
            var facingTarget = __instance.IsLookingAt(target.transform.position, shared.m_aiAttackMaxAngle, shared.m_aiInvertAngleCheck);

            HexLog.LogInfo(
                $"[Attack Conditions] Dvergr={character.name}, " +
                $"Target={target.name}, " +
                $"Weapon={shared.m_name}, " +
                $"Distance={distance:F1}, " +
                $"AttackRange={shared.m_aiAttackRange:F1}, " +
                $"AttackRangeMin={shared.m_aiAttackRangeMin:F1}, " +
                $"InRange={inRange}, " +
                $"CanSee={canSeeTarget}, " +
                $"CanHear={canHearTarget}, " +
                $"FacingTarget={facingTarget}, " +
                $"MaxAngle={shared.m_aiAttackMaxAngle:F1}, " +
                $"WeaponCooldownReady={weaponCooldownReady}, " +
                $"CharacterCooldownReady={characterCooldownReady}, " +
                $"Alerted={__instance.IsAlerted()}, " +
                $"InAttack={character.InAttack()}");
        }
    }
}