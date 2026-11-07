using HarmonyLib;
using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.Features.Progression;
using HexDvergrAllies.Core.HexLocalization;
using System.Collections.Generic;
using System.Text;

namespace HexDvergrAllies.Core.UI
{
    internal static class DvergrCompendiumManager
    {
        // Verified private in the installed Valheim assembly. Native list construction
        // handles button creation, scrolling and gamepad selection for our entry.
        private static readonly AccessTools.FieldRef<TextsDialog, List<TextsDialog.TextInfo>> TextsField = AccessTools.FieldRefAccess<TextsDialog, List<TextsDialog.TextInfo>>("m_texts");
        private static readonly string[] UnlockBosses = { "", "$enemy_eikthyr", "$enemy_gdking", "$enemy_bonemass", "$enemy_dragon", "$enemy_goblinking", "$enemy_seekerqueen", "$enemy_fader" };

        internal static void AddEntry(TextsDialog dialog)
        {
            if (!Player.m_localPlayer)
            {
                return;
            }

            var topic = HexLocalizationManager.Localize("$hex_dvergr_compendium_title");
            TextsField(dialog).Add(new TextsDialog.TextInfo(topic, BuildText(Player.m_localPlayer)));
        }

        internal static string BuildText(Player player)
        {
            var text = new StringBuilder();
            text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_intro"));
            text.AppendLine();
            ZDO companion;
            var currentTier = -1;

            if (DvergrCompanionManager.TryGetHiredCompanionZDO(player, out companion) && DvergrCompanionState.IsHired(companion) && DvergrCompanionState.GetOwnerPlayerId(companion) == player.GetPlayerID())
            {
                text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_current"));

                if (companion.GetBool(DvergrStarterCharacterState.StarterKey, false))
                {
                    var tier = UnityEngine.Mathf.Clamp(companion.GetInt(DvergrStarterBalanceService.TierKey, 0), 0, (int)DvergrStarterTier.DeepNorth);
                    currentTier = tier;
                    AppendTier(text, tier);
                }
                else
                {
                    AppendNormalCompanion(text, companion);
                }
            }
            else
            {
                text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_no_starter"));
            }

            var reduction = (1f - (currentTier >= 0 || companion != null ? DvergrStarterProgressionService.DamageTakenMultiplier(companion) : HexDvergrAlliesConfiguration.CompanionDamageTakenMultiplier.Value)) * 100f;
            text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_reduction", reduction));
            text.AppendLine();
            if (currentTier >= 0)
            {
                text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_next"));

                if (currentTier < (int)DvergrStarterTier.DeepNorth)
                {
                    var nextTier = currentTier + 1;
                    AppendTier(text, nextTier);
                    text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_unlock", Localization.instance.Localize(UnlockBosses[nextTier])));
                }
                else
                {
                    text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_max_level"));
                }

                text.AppendLine();
            }

            text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_tiers"));

            for (var tier = 0; tier <= (int)DvergrStarterTier.DeepNorth; tier++)
            {
                AppendTier(text, tier);
                var unlock = tier == 0 ? HexLocalizationManager.Localize("$hex_dvergr_compendium_start") : HexLocalizationManager.Localize("$hex_dvergr_compendium_unlock", Localization.instance.Localize(UnlockBosses[tier]));
                text.AppendLine(unlock);
                text.AppendLine();
            }

            text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_damage_note"));

            return text.ToString();
        }

        private static void AppendNormalCompanion(StringBuilder text, ZDO companion)
        {
            var prefab = ZNetScene.instance.GetPrefab(companion.GetPrefab());
            var template = prefab ? prefab.GetComponent<Humanoid>() : null;
            var instance = ZNetScene.instance.FindInstance(companion.m_uid);
            var humanoid = instance ? instance.GetComponent<Humanoid>() : null;
            var tierKey = prefab && prefab.name.StartsWith(PrefabHelper.AshlandsDvergrPrefix) ? "$hex_dvergr_tier_6" : "$hex_dvergr_tier_5";
            text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_native_tier", HexLocalizationManager.Localize(tierKey)));
            var maximumHealth = humanoid ? humanoid.GetMaxHealth() : companion.GetFloat(ZDOVars.s_maxHealth, 0f);

            if (maximumHealth > 0f)
            {
                text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_health", maximumHealth));
            }
            else
            {
                text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_unavailable"));
            }

            text.AppendLine(HexLocalizationManager.Localize(humanoid ? "$hex_dvergr_compendium_attacks" : "$hex_dvergr_compendium_reference_attacks"));

            if (humanoid)
            {
                foreach (var item in humanoid.GetInventory().GetAllItems())
                {
                    AppendWeapon(text, item, companion);
                }
            }
            else if (template && template.m_defaultItems != null)
            {
                foreach (var itemPrefab in template.m_defaultItems)
                {
                    var drop = itemPrefab ? itemPrefab.GetComponent<ItemDrop>() : null;

                    if (drop)
                    {
                        AppendWeapon(text, drop.m_itemData, companion);
                    }
                }
            }

            if (DvergrStarterProgressionService.FaderMultiplier(companion) > 1f)
            {
                text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_fader_bonus", (DvergrStarterBalanceConfig.FaderHealthMultiplier - 1f) * 100f, (DvergrStarterBalanceConfig.FaderBonusMultiplier - 1f) * 100f));
            }

            text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_native_note"));
        }

        private static void AppendWeapon(StringBuilder text, ItemDrop.ItemData item, ZDO companion)
        {
            if (item == null || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
            {
                return;
            }

            var damage = item.GetDamage();

            if (damage.GetTotalDamage() <= 0f)
            {
                return;
            }

            damage.Modify(HexDvergrAlliesConfiguration.CompanionDamageDealtMultiplier.Value * DvergrStarterProgressionService.FaderMultiplier(companion));
            text.AppendLine("<color=orange>" + Localization.instance.Localize(item.m_shared.m_name) + "</color>");
            text.AppendLine(Localization.instance.Localize(damage.GetTooltipString()));
        }

        private static void AppendTier(StringBuilder text, int tier)
        {
            var profile = DvergrStarterBalanceConfig.Get((DvergrStarterTier)tier);
            var biome = HexLocalizationManager.Localize("$hex_dvergr_tier_" + tier);
            text.AppendLine("<color=orange>" + HexLocalizationManager.Localize("$hex_dvergr_starter_level", tier + 1, biome) + "</color>");
            text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_stats", profile.Health, profile.ArbalestPierce, profile.MeleeBlunt));
        }
    }
}
