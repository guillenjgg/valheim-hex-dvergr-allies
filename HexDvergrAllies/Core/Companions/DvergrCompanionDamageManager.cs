using HexDvergrAllies.Core.Networking;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionDamageManager
    {
        internal struct DamageState
        {
            internal float Health;
            internal ZDOID CompanionId;
            internal long OwnerPlayerId;
        }

        internal static void BeforeDamage(Character character, HitData hit, out DamageState state)
        {
            state = default(DamageState);
            var netView = character.GetComponent<ZNetView>();

            if (netView == null || !netView.IsValid() || !netView.IsOwner())
            {
                return;
            }

            if (DvergrCompanionBehaviorManager.IsImmuneToDamage(character, hit))
            {
                hit.m_damage.m_damage = 0f;
                return;
            }

            if (DvergrCompanionState.IsHired(character))
            {
                var damageTakenMultiplier = HexDvergrAlliesConfiguration.CompanionDamageTakenMultiplier.Value;
                hit.ApplyModifier(damageTakenMultiplier);
            }

            var attacker = hit.GetAttacker();

            if (attacker == null || !DvergrCompanionState.IsHired(attacker))
            {
                return;
            }

            var damageDealtMultiplier = HexDvergrAlliesConfiguration.CompanionDamageDealtMultiplier.Value;
            hit.ApplyModifier(damageDealtMultiplier);

            if (HexDvergrAlliesConfiguration.CompanionEnableBloodMagicExperience.Value &&
                !character.IsPlayer() && !character.IsDead() && character.GetHealth() > 0f)
            {
                var companionZdo = attacker.GetComponent<ZNetView>().GetZDO();
                state.Health = character.GetHealth();
                state.CompanionId = companionZdo.m_uid;
                state.OwnerPlayerId = DvergrCompanionState.GetOwnerPlayerId(companionZdo);
            }
        }

        internal static void AfterDamage(Character character, DamageState state)
        {
            // Only the damaged creature's owning peer observes and reports the health loss.
            if (state.OwnerPlayerId == 0L || character.GetHealth() >= state.Health)
            {
                return;
            }

            foreach (var player in Player.GetAllPlayers())
            {
                if (!player || player.GetPlayerID() != state.OwnerPlayerId)
                {
                    continue;
                }

                var playerView = player.GetComponent<ZNetView>();
                if (playerView && playerView.IsValid())
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC(playerView.GetZDO().GetOwner(),
                        DvergrRpcManager.BloodMagicExperienceRpc, character.GetZDOID(), state.CompanionId);
                }

                return;
            }
        }

        internal static void RPC_BloodMagicExperience(long sender, ZDOID targetId, ZDOID companionId)
        {
            var player = Player.m_localPlayer;
            if (!player || player.IsDead() || !HexDvergrAlliesConfiguration.CompanionEnableBloodMagicExperience.Value)
            {
                return;
            }

            var targetZdo = ZDOMan.instance.GetZDO(targetId);
            var companionZdo = ZDOMan.instance.GetZDO(companionId);
            if (targetZdo == null || targetZdo.GetOwner() != sender || companionZdo == null ||
                !DvergrCompanionState.IsHired(companionZdo) ||
                DvergrCompanionState.GetOwnerPlayerId(companionZdo) != player.GetPlayerID())
            {
                return;
            }

            var factor = HexDvergrAlliesConfiguration.CompanionBloodMagicExperienceFactor.Value;
            if (factor > 0f)
            {
                player.RaiseSkill(Skills.SkillType.BloodMagic, factor);
            }
        }
    }
}
