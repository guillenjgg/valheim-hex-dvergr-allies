using HexDvergrAllies.Core;
using HarmonyLib;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace HexDvergrAllies.Core.Features.Progression
{
    internal static class DvergrStarterBalanceService
    {
        internal const string TierKey = "hex_starter_tier";
        private static readonly MethodInfo CloneSharedData = AccessTools.Method(typeof(object), "MemberwiseClone");
        private static readonly ConditionalWeakTable<ItemDrop.ItemData, WeaponState> Weapons = new ConditionalWeakTable<ItemDrop.ItemData, WeaponState>();

        private static readonly ConditionalWeakTable<Character, BalanceLogState> LoggedBalance = new ConditionalWeakTable<Character, BalanceLogState>();

        private sealed class BalanceLogState
        {
            internal bool Initialized;
            internal bool Hired;
            internal int Tier;
            internal float MaximumHealth;
            internal float Pierce;
            internal float Blunt;
            internal float DamageTaken;
        }

        private sealed class WeaponState
        {
            internal HitData.DamageTypes OriginalDamage;
        }

        internal static DvergrStarterTierProfile GetProfile(Character character)
        {
            var zdo = character.GetComponent<ZNetView>().GetZDO();
            var tier = (DvergrStarterTier)zdo.GetInt(TierKey, (int)DvergrStarterTier.Meadows);

            return DvergrStarterBalanceConfig.Get(tier);
        }

        internal static void Update(Character character)
        {
            if (!DvergrStarterCharacterState.IsStarter(character))
            {
                return;
            }

            var view = character.GetComponent<ZNetView>();
            var profile = GetProfile(character);

            if (view.IsOwner() && !character.IsDead() && !Mathf.Approximately(character.GetMaxHealth(), profile.Health))
            {
                var percentage = character.GetHealth() / Mathf.Max(1f, character.GetMaxHealth());
                character.SetMaxHealth(profile.Health);
                character.SetHealth(profile.Health * percentage);
            }

            var humanoid = character as Humanoid;

            if (!humanoid)
            {
                return;
            }

            foreach (var item in humanoid.GetInventory().GetAllItems())
            {
                // Clone only this instance's weapon data. Never edit shared prefab data.
                if (item.m_shared.m_damages.m_pierce <= 0f && item.m_shared.m_damages.m_blunt <= 0f && !Weapons.TryGetValue(item, out _))
                {
                    continue;
                }

                WeaponState state;

                if (!Weapons.TryGetValue(item, out state))
                {
                    state = new WeaponState { OriginalDamage = item.m_shared.m_damages };
                    item.m_shared = (ItemDrop.ItemData.SharedData)CloneSharedData.Invoke(item.m_shared, null);
                    Weapons.Add(item, state);
                }

                var damage = state.OriginalDamage;

                if (damage.m_pierce > 0f)
                {
                    damage.m_pierce = profile.ArbalestPierce;
                }

                if (damage.m_blunt > 0f)
                {
                    damage.m_blunt = profile.MeleeBlunt;
                }

                item.m_shared.m_damages = damage;
            }

            LogAppliedBalance(character, view, profile, humanoid);
        }

        private static void LogAppliedBalance(Character character, ZNetView view, DvergrStarterTierProfile profile, Humanoid humanoid)
        {
            if (!HexDvergrAlliesConfiguration.EnableDebugLogs.Value || !view.IsOwner() || character.IsDead())
            {
                return;
            }

            var zdo = view.GetZDO();
            var tier = zdo.GetInt(TierKey, 0);
            var hired = Companions.DvergrCompanionState.IsHired(zdo);
            var taken = DvergrStarterProgressionService.DamageTakenMultiplier(zdo);
            var state = LoggedBalance.GetValue(character, _ => new BalanceLogState());

            if (state.Initialized && state.Tier == tier && state.Hired == hired && state.MaximumHealth == character.GetMaxHealth() && state.Pierce == profile.ArbalestPierce && state.Blunt == profile.MeleeBlunt && state.DamageTaken == taken)
            {
                return;
            }

            state.Initialized = true;
            state.Tier = tier;
            state.Hired = hired;
            state.MaximumHealth = character.GetMaxHealth();
            state.Pierce = profile.ArbalestPierce;
            state.Blunt = profile.MeleeBlunt;
            state.DamageTaken = taken;
            HexLog.LogInfo($"[Starter Balance] Character={zdo.GetLong(DvergrStarterCharacterState.CharacterKey, 0L)}, Dvergr={zdo.m_uid}, Tier={(DvergrStarterTier)tier}, Level={tier + 1}, Hired={hired}, Health={character.GetHealth():F1}/{character.GetMaxHealth():F1}, TargetHealth={profile.Health:F1}, ArbalestBasePierce={profile.ArbalestPierce:F1}, MeleeBaseBlunt={profile.MeleeBlunt:F1}, DamageTakenMultiplier={taken:F2}, DamageReduction={(1f - taken) * 100f:F0}%, PreHireImmune={DvergrStarterCharacterState.IsProtected(character)}");

            foreach (var item in humanoid.GetInventory().GetAllItems())
            {
                if (Weapons.TryGetValue(item, out _))
                {
                    var damage = item.m_shared.m_damages;
                    HexLog.LogInfo($"[Starter Weapon] Dvergr={zdo.m_uid}, Item={item.m_shared.m_name}, BasePierce={damage.m_pierce:F1}, BaseBlunt={damage.m_blunt:F1}");
                }
            }
        }
    }
}
