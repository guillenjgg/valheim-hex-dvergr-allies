using HexDvergrAllies.Core.Constants;
using UnityEngine;

namespace HexDvergrAllies.Core.Monoscripts
{
    internal sealed class DvergrCompanionComponent : MonoBehaviour
    {
        internal bool FireImmune { get; set; }
        internal bool LavaImmune { get; set; }

        internal void Initialize(Character character)
        {
            var isAshlandsDvergr = character.name.StartsWith(ValheimPrefabNames.AshlandsDvergrPrefix);

            FireImmune = isAshlandsDvergr;
            LavaImmune = isAshlandsDvergr;
        }

        internal void ApplyDamageModifiers(ref HitData.DamageModifiers modifiers)
        {
            if (FireImmune)
            {
                modifiers.m_fire = HitData.DamageModifier.Immune;
            }
        }

        internal bool ProcessDamage(HitData hit)
        {
            if (LavaImmune && hit.m_hitType == HitData.HitType.AshlandsLava)
            {
                return false;
            }

            return true;
        }

        internal bool CanApplyStatusEffect(int statusEffectHash)
        {
            if (FireImmune && statusEffectHash == SEMan.s_statusEffectBurning)
            {
                return false;
            }

            return true;
        }

        internal bool CanApplyStatusEffect(StatusEffect statusEffect)
        {
            return CanApplyStatusEffect(statusEffect.NameHash());
        }
    }
}
