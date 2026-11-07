using HexDvergrAllies.Core;
using System.Collections.Generic;
using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.HexLocalization;
using UnityEngine;

namespace HexDvergrAllies.Core.Features.Progression
{
    internal static class DvergrStarterProgressionService
    {
        private const string FaderOwnerKey = "hex_dvergr_fader_owner";

        internal static float FaderMultiplier(ZDO companion)
        {
            var owner = DvergrCompanionState.GetOwnerPlayerId(companion);

            return owner != 0L && DvergrCompanionState.IsHired(companion) && companion.GetLong(FaderOwnerKey, 0L) == owner ? DvergrStarterBalanceConfig.FaderBonusMultiplier : 1f;
        }

        internal static bool HasFaderBonus(ZDO companion)
        {
            if (!DvergrCompanionState.IsHired(companion))
            {
                return false;
            }

            return companion.GetBool(DvergrStarterCharacterState.StarterKey, false) ? companion.GetInt(DvergrStarterBalanceService.TierKey, 0) >= (int)DvergrStarterTier.DeepNorth : FaderMultiplier(companion) > 1f;
        }

        internal static float HealthMultiplier(ZDO companion)
        {
            return HasFaderBonus(companion) ? DvergrStarterBalanceConfig.FaderHealthMultiplier : 1f;
        }

        internal static float DamageTakenMultiplier(ZDO companion)
        {
            var configured = HexDvergrAlliesConfiguration.CompanionDamageTakenMultiplier.Value;

            return HasFaderBonus(companion) ? Mathf.Min(configured, DvergrStarterBalanceConfig.FaderDamageTakenMultiplier) : configured;
        }

        internal const string ReportRpc = "HexStarterBossProgress";
        internal const string ApplyRpc = "HexStarterApplyTier";
        private static readonly string[] BossNames = { "$enemy_eikthyr", "$enemy_gdking", "$enemy_bonemass", "$enemy_dragon", "$enemy_goblinking", "$enemy_seekerqueen", "$enemy_fader" };

        internal static int TierFromKills(IDictionary<string, float> kills)
        {
            for (var index = BossNames.Length - 1; index >= 0; index--)
            {
                float count;

                if (kills.TryGetValue(BossNames[index], out count) && count > 0f)
                {
                    return index + 1;
                }
            }

            return 0;
        }

        internal static void Report(Player player)
        {
            var profile = Game.instance.GetPlayerProfile();

            if (player != Player.m_localPlayer || profile == null || profile.GetPlayerID() != player.GetPlayerID() || profile.m_playerStats == null || profile.m_playerStats.Length == 0)
            {
                return;
            }

            // Index zero holds raw character statistics, including all difficulties.
            var stats = profile.m_playerStats[0].m_enemyStats;

            if (stats == null || stats.Length == 0 || stats[0] == null)
            {
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance), ReportRpc, player.GetZDOID(), TierFromKills(stats[0]));
        }

        internal static void OnReport(long sender, ZDOID playerId, int tier)
        {
            if (!ZNet.instance || !ZNet.instance.IsServer() || tier < 0 || tier > (int)DvergrStarterTier.DeepNorth)
            {
                return;
            }

            var player = ZDOMan.instance.GetZDO(playerId);
            var connected = Player.m_localPlayer && Player.m_localPlayer.GetZDOID() == playerId;

            foreach (var peer in ZNet.instance.GetPeers())
            {
                connected |= peer.IsReady() && peer.m_characterID == playerId;
            }

            if (!connected || player == null || player.GetOwner() != sender)
            {
                return;
            }

            var characterId = player.GetLong(ZDOVars.s_playerID, 0L);

            if (characterId == 0L)
            {
                return;
            }

            if (tier == 0)
            {
                return;
            }

            foreach (var starter in DvergrPersistentOwnership.GetHiredCompanions(characterId))
            {
                var isStarter = starter.GetBool(DvergrStarterCharacterState.StarterKey, false);

                if (!starter.HasOwner() || (isStarter ? tier <= starter.GetInt(DvergrStarterBalanceService.TierKey, 0) : tier < (int)DvergrStarterTier.DeepNorth || starter.GetLong(FaderOwnerKey, 0L) == characterId))
                {
                    continue;
                }

                // The owning peer commits changes so ownership transfer cannot overwrite them.
                ZRoutedRpc.instance.InvokeRoutedRPC(starter.GetOwner(), ApplyRpc, starter.m_uid, characterId, tier);
            }
        }

        internal static void OnApply(long sender, ZDOID companionId, long characterId, int tier)
        {
            if (!ZNetScene.instance || sender != DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance) || tier < 0 || tier > (int)DvergrStarterTier.DeepNorth)
            {
                return;
            }

            var instance = ZNetScene.instance.FindInstance(companionId);
            var character = instance ? instance.GetComponent<Character>() : null;

            if (!character)
            {
                return;
            }

            var view = character.GetComponent<ZNetView>();
            var zdo = view.GetZDO();
            var oldTier = zdo.GetInt(DvergrStarterBalanceService.TierKey, 0);
            var isStarter = DvergrStarterCharacterState.IsStarter(character);

            if (!view.IsOwner() || character.IsDead() || !DvergrCompanionState.IsHired(zdo) || DvergrCompanionState.GetOwnerPlayerId(zdo) != characterId || (isStarter && (zdo.GetLong(DvergrStarterCharacterState.CharacterKey, 0L) != characterId || tier <= oldTier)))
            {
                return;
            }

            if (!isStarter)
            {
                if (tier == (int)DvergrStarterTier.DeepNorth && zdo.GetLong(FaderOwnerKey, 0L) != characterId)
                {
                    zdo.Set(FaderOwnerKey, characterId);
                    HexLog.LogInfo($"[Dvergr Progression] Fader bonus applied: Character={characterId}, Companion={companionId}, HealthMultiplier={DvergrStarterBalanceConfig.FaderHealthMultiplier}, DamageMultiplier={DvergrStarterBalanceConfig.FaderBonusMultiplier}, DamageTakenMultiplier={DamageTakenMultiplier(zdo)}");
                }

                return;
            }

            zdo.Set(DvergrStarterBalanceService.TierKey, tier);
            DvergrStarterBalanceService.Update(character);

            if (DvergrCompanionState.IsHired(zdo))
            {
                PlayEffect("vfx_skilllevelup", character);
                PlayEffect("sfx_levelup", character);
            }

            HexLog.LogInfo($"[Starter Progression] Character={characterId}, Dvergr={companionId}, Tier={tier}, Level={tier + 1}");
        }

        private static void PlayEffect(string name, Character character)
        {
            var prefab = ZNetScene.instance.GetPrefab(name);

            if (prefab)
            {
                Object.Instantiate(prefab, character.GetCenterPoint(), Quaternion.identity);
            }
        }

        internal static string LevelLabel(Character character)
        {
            var tier = Mathf.Clamp(character.GetComponent<ZNetView>().GetZDO().GetInt(DvergrStarterBalanceService.TierKey, 0), 0, (int)DvergrStarterTier.DeepNorth);
            var biome = HexLocalizationManager.Localize("$hex_dvergr_tier_" + tier);

            return string.Format(HexLocalizationManager.Localize("$hex_dvergr_starter_level"), tier + 1, biome);
        }

        internal static string AppendLevel(Character character, string name)
        {
            if (!DvergrStarterCharacterState.IsStarter(character))
            {
                return name;
            }

            var tier = Mathf.Clamp(character.GetComponent<ZNetView>().GetZDO().GetInt(DvergrStarterBalanceService.TierKey, 0), 0, (int)DvergrStarterTier.DeepNorth);

            return name + " · " + (tier + 1);
        }
    }
}
