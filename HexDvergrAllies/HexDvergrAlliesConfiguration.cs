
using System;
using BepInEx.Configuration;
using UnityEngine;

namespace HexDvergrAllies
{
    internal static class HexDvergrAlliesConfiguration
    {
        internal static ConfigEntry<bool> EnablePlayerDamageImmunity;
        internal static ConfigEntry<bool> EnableDebugLogs;
        internal static ConfigEntry<bool> AllowTemporaryHomestead;

        internal static ConfigEntry<KeyboardShortcut> CompanionInteractionShortcut;
        internal static ConfigEntry<float> CompanionMovementSpeedMultiplier;
        internal static ConfigEntry<float> CompanionFollowDistance;
        internal static ConfigEntry<bool> CompanionEnableJumping;
        internal static ConfigEntry<float> CompanionHealthMultiplier;
        internal static ConfigEntry<float> CompanionDamageTakenMultiplier;
        internal static ConfigEntry<float> CompanionDamageDealtMultiplier;
        internal static ConfigEntry<float> CompanionAttackSpeedMultiplier;
        internal static ConfigEntry<bool> CompanionEnableAutoHealing;
        internal static ConfigEntry<float> CompanionAutoHealThreshold;
        internal static ConfigEntry<float> CompanionAutoHealCooldown;

        internal static void Init(ConfigFile config)
        {
            EnablePlayerDamageImmunity = config.Bind("General", "EnablePlayerDamageImmunity", true, new ConfigDescription("Prevents players from damaging Dvergr.", null, new ConfigurationManagerAttributes { Order = 100 }));

            AllowTemporaryHomestead = config.Bind("Dvergr Wards", "AllowTemporaryHomestead", true, new ConfigDescription("Allows players to place a workbench, stonecutter, wood portal, and stone portal inside Dvergr ward-protected areas. All other structures remain restricted.", null, new ConfigurationManagerAttributes { Order = 100 }));

            CompanionInteractionShortcut = config.Bind("Dvergr Companion", "InteractionShortcut", new KeyboardShortcut(KeyCode.E, KeyCode.LeftShift), new ConfigDescription("Keyboard shortcut to forge or break a pact with a Dvergr companion.", null, new ConfigurationManagerAttributes { Order = 1000 }));

            CompanionMovementSpeedMultiplier = config.Bind("Dvergr Companion", "MovementSpeedMultiplier", 2.5f, new ConfigDescription("Movement speed multiplier for hired Dvergr companions.", new AcceptableValueRange<float>(1f, 5f), new ConfigurationManagerAttributes { Order = 900 }));

            CompanionFollowDistance = config.Bind("Dvergr Companion", "FollowDistance", 5f, new ConfigDescription("Desired following distance in meters. Not implemented yet.", new AcceptableValueRange<float>(1f, 30f), new ConfigurationManagerAttributes { Order = 890, IsAdvanced = true }));

            CompanionEnableJumping = config.Bind("Dvergr Companion", "EnableJumping", true, new ConfigDescription("Reserved for a future companion obstacle navigation feature.", null, new ConfigurationManagerAttributes { Order = 880, IsAdvanced = true }));

            CompanionHealthMultiplier = config.Bind("Dvergr Companion", "HealthMultiplier", 2f, new ConfigDescription("Maximum health multiplier for hired Dvergr companions.", new AcceptableValueRange<float>(1f, 10f), new ConfigurationManagerAttributes { Order = 800 }));

            CompanionDamageTakenMultiplier = config.Bind("Dvergr Companion", "DamageTakenMultiplier", 0.5f, new ConfigDescription("Incoming damage multiplier. 0.5 means 50% less damage.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 790 }));

            CompanionDamageDealtMultiplier = config.Bind("Dvergr Companion", "DamageDealtMultiplier", 1.5f, new ConfigDescription("Outgoing damage multiplier for hired Dvergr companions. 1.5 means 50% more damage.", new AcceptableValueRange<float>(0f, 10f), new ConfigurationManagerAttributes { Order = 780 }));

            CompanionAttackSpeedMultiplier = config.Bind("Dvergr Companion", "AttackSpeedMultiplier", 1.25f, new ConfigDescription("Attack cooldown speed multiplier. 1.25 means 25% faster cooldown recovery.", new AcceptableValueRange<float>(1f, 5f), new ConfigurationManagerAttributes { Order = 770 }));

            CompanionEnableAutoHealing = config.Bind("Dvergr Companion", "EnableAutoHealing", true, new ConfigDescription("Automatically heals hired Dvergr companions when their health reaches the configured threshold.", null, new ConfigurationManagerAttributes { Order = 700 }));

            CompanionAutoHealThreshold = config.Bind("Dvergr Companion", "AutoHealThreshold", 0.5f, new ConfigDescription("Health percentage that triggers automatic healing. 0.5 means 50% health.", new AcceptableValueRange<float>(0.01f, 1f), new ConfigurationManagerAttributes { Order = 690 }));

            CompanionAutoHealCooldown = config.Bind("Dvergr Companion", "AutoHealCooldown", 60f, new ConfigDescription("Minimum number of seconds between automatic heals for each companion.", new AcceptableValueRange<float>(0f, 3600f), new ConfigurationManagerAttributes { Order = 680 }));

            EnableDebugLogs = config.Bind("Debug", "EnableDebugLogs", true, new ConfigDescription("Logs Dvergr damage events, status effects, and AI states.", null, new ConfigurationManagerAttributes { Order = 100, IsAdvanced = true }));
        }
    }

    internal sealed class ConfigurationManagerAttributes
    {
        public bool? ShowRangeAsPercent;
        public Action<ConfigEntryBase> CustomDrawer;
        public CustomHotkeyDrawerFunc CustomHotkeyDrawer;
        public bool? Browsable;
        public string Category;
        public object DefaultValue;
        public bool? HideDefaultButton;
        public bool? HideSettingName;
        public string Description;
        public string DispName;
        public int? Order;
        public bool? ReadOnly;
        public bool? IsAdvanced;
        public Func<object, string> ObjToStr;
        public Func<string, object> StrToObj;

        public delegate void CustomHotkeyDrawerFunc(ConfigEntryBase setting, ref bool isCurrentlyAcceptingInput);
    }
}
