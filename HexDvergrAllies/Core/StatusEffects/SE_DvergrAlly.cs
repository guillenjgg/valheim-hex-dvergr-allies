namespace HexDvergrAllies.Core.StatusEffects
{
    internal class SE_DvergrAlly : StatusEffect
    {
        public override void OnDamaged(HitData hit, Character attacker)
        {
            if (attacker == null || !attacker.IsPlayer())
            {
                return;
            }

            if (!HexDvergrAlliesConfiguration.EnablePlayerDamageImmunity.Value)
            {
                return;
            }

            hit.ApplyModifier(0f);
        }
    }
}