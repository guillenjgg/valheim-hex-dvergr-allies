using HexDvergrAllies.Core;
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
            internal string StarterDiagnostic;
            internal float DiagnosticHealth;
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
                var damageTakenMultiplier = Features.Progression.DvergrStarterProgressionService.DamageTakenMultiplier(netView.GetZDO());
                hit.ApplyModifier(damageTakenMultiplier);
            }

            var attacker = hit.GetAttacker();

            if (attacker == null || !DvergrCompanionState.IsHired(attacker))
            {
                return;
            }

            var damageDealtMultiplier = Features.Progression.DvergrStarterCharacterState.IsStarter(attacker) ? 1f : HexDvergrAlliesConfiguration.CompanionDamageDealtMultiplier.Value * Features.Progression.DvergrStarterProgressionService.FaderMultiplier(attacker.GetComponent<ZNetView>().GetZDO());
            hit.ApplyModifier(damageDealtMultiplier);

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value && Features.Progression.DvergrStarterCharacterState.IsStarter(attacker))
            {
                var zdo = attacker.GetComponent<ZNetView>().GetZDO();
                var profile = Features.Progression.DvergrStarterBalanceService.GetProfile(attacker);
                var tier = zdo.GetInt(Features.Progression.DvergrStarterBalanceService.TierKey, 0);
                state.DiagnosticHealth = character.GetHealth();
                state.StarterDiagnostic = $"[Starter Damage] Dvergr={zdo.m_uid}, Level={tier + 1}, AttackerHealth={attacker.GetHealth():F1}/{attacker.GetMaxHealth():F1}, ExpectedMaxHealth={profile.Health:F1}, ExpectedBasePierce={profile.ArbalestPierce:F1}, ExpectedBaseBlunt={profile.MeleeBlunt:F1}, Target={character.name}, ApplyDamagePierce={hit.m_damage.m_pierce:F2}, ApplyDamageBlunt={hit.m_damage.m_blunt:F2}, ApplyDamageTotal={hit.GetTotalDamage():F2}, OutgoingMultiplier={damageDealtMultiplier:F2}";
            }

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
            if (state.StarterDiagnostic != null)
            {
                var healthLost = UnityEngine.Mathf.Max(0f, state.DiagnosticHealth - character.GetHealth());
                HexLog.LogInfo($"{state.StarterDiagnostic}, TargetHealthBefore={state.DiagnosticHealth:F2}, TargetHealthAfter={character.GetHealth():F2}, ObservedHealthLost={healthLost:F2}");
            }

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
