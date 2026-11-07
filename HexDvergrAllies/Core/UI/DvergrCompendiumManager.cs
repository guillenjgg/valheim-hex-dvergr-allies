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
        private static readonly string[] UnlockBosses = { "", "$enemy_eikthyr", "$enemy_gdking", "$enemy_bonemass", "$enemy_dragon", "$enemy_goblinking", "$enemy_seekerqueen" };

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

            if (DvergrCompanionManager.TryGetHiredCompanionZDO(player, out companion) && companion.GetBool(DvergrStarterCharacterState.StarterKey, false))
            {
                var tier = UnityEngine.Mathf.Clamp(companion.GetInt(DvergrStarterBalanceService.TierKey, 0), 0, (int)DvergrStarterTier.Ashlands);
                currentTier = tier;
                text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_current"));
                AppendTier(text, tier);
            }
            else
            {
                text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_no_starter"));
            }

            var reduction = (1f - HexDvergrAlliesConfiguration.CompanionDamageTakenMultiplier.Value) * 100f;
            text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_reduction", reduction));
            text.AppendLine();
            if (currentTier >= 0)
            {
                text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_next"));

                if (currentTier < (int)DvergrStarterTier.Ashlands)
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

            for (var tier = 0; tier <= (int)DvergrStarterTier.Ashlands; tier++)
            {
                AppendTier(text, tier);
                var unlock = tier == 0 ? HexLocalizationManager.Localize("$hex_dvergr_compendium_start") : HexLocalizationManager.Localize("$hex_dvergr_compendium_unlock", Localization.instance.Localize(UnlockBosses[tier]));
                text.AppendLine(unlock);
                text.AppendLine();
            }

            text.AppendLine(HexLocalizationManager.Localize("$hex_dvergr_compendium_damage_note"));

            return text.ToString();
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
