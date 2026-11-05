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
        internal static ConfigEntry<KeyboardShortcut> CompanionSentryShortcut;
        internal static ConfigEntry<KeyboardShortcut> CompanionRecallShortcut;
        internal static ConfigEntry<float> CompanionMovementSpeedMultiplier;
        internal static ConfigEntry<float> CompanionFollowDistance;
        internal static ConfigEntry<bool> CompanionEnableBloodMagicExperience;
        internal static ConfigEntry<float> CompanionBloodMagicExperienceFactor;
        internal static ConfigEntry<float> CompanionHealthMultiplier;
        internal static ConfigEntry<float> CompanionDamageTakenMultiplier;
        internal static ConfigEntry<float> CompanionDamageDealtMultiplier;
        internal static ConfigEntry<float> CompanionAttackSpeedMultiplier;
        internal static ConfigEntry<bool> CompanionEnableAutoHealing;
        internal static ConfigEntry<float> CompanionAutoHealThreshold;
        internal static ConfigEntry<float> CompanionAutoHealCooldown;
        internal static ConfigEntry<KeyboardShortcut> CompanionUIShortcut;

        internal static void Init(ConfigFile config)
        {
            CompanionEnableBloodMagicExperience = config.Bind("Dvergr Companion", "EnableBloodMagicExperience", true, new ConfigDescription("Hired Dvergr grant their owner Blood Magic experience when they deal health damage to a creature.", null, new ConfigurationManagerAttributes { Order = 1100 }));

            CompanionBloodMagicExperienceFactor = config.Bind("Dvergr Companion", "BloodMagicExperienceFactor", 1f, new ConfigDescription("Blood Magic skill experience factor per damaging hit. Uses normal skill progression, not whole skill levels.", new AcceptableValueRange<float>(0f, 10f), new ConfigurationManagerAttributes { Order = 1090 }));

            CompanionRecallShortcut = config.Bind("Dvergr Companion", "RecallShortcut", new KeyboardShortcut(KeyCode.O), new ConfigDescription("Keyboard shortcut for Get over here. Recalls your hired Dvergr to your location and switches them to Companion mode.", null, new ConfigurationManagerAttributes { Order = 980 }));

            CompanionSentryShortcut = config.Bind("Dvergr Companion", "SentryShortcut", new KeyboardShortcut(KeyCode.P, KeyCode.LeftControl), new ConfigDescription("Keyboard shortcut to toggle your hired Dvergr between Sentry and Companion mode. Sentry mode guards your current position.", null, new ConfigurationManagerAttributes { Order = 990 }));

            EnablePlayerDamageImmunity = config.Bind("General", "EnablePlayerDamageImmunity", true, new ConfigDescription("Prevents players from damaging Dvergr.", null, new ConfigurationManagerAttributes { Order = 100 }));

            AllowTemporaryHomestead = config.Bind("Dvergr Wards", "AllowTemporaryHomestead", true, new ConfigDescription("Allows players to place a workbench, stonecutter, wood portal, and stone portal inside Dvergr ward-protected areas. All other structures remain restricted.", null, new ConfigurationManagerAttributes { Order = 100 }));

            CompanionInteractionShortcut = config.Bind("Dvergr Companion", "InteractionShortcut", new KeyboardShortcut(KeyCode.E, KeyCode.LeftShift), new ConfigDescription("Keyboard shortcut to forge or break a pact with a Dvergr companion.", null, new ConfigurationManagerAttributes { Order = 1000 }));

            CompanionMovementSpeedMultiplier = config.Bind("Dvergr Companion", "MovementSpeedMultiplier", 2.5f, new ConfigDescription("Movement speed multiplier for hired Dvergr companions.", new AcceptableValueRange<float>(1f, 5f), new ConfigurationManagerAttributes { Order = 900 }));

            CompanionFollowDistance = config.Bind("Dvergr Companion", "FollowDistance", 2f, new ConfigDescription("Distance in meters at which your companion stops following you. Does not limit combat range.", new AcceptableValueRange<float>(1f, 30f), new ConfigurationManagerAttributes { Order = 890 }));

            CompanionHealthMultiplier = config.Bind("Dvergr Companion", "HealthMultiplier", 2f, new ConfigDescription("Maximum health multiplier for hired Dvergr companions.", new AcceptableValueRange<float>(1f, 10f), new ConfigurationManagerAttributes { Order = 800 }));

            CompanionDamageTakenMultiplier = config.Bind("Dvergr Companion", "DamageTakenMultiplier", 0.5f, new ConfigDescription("Incoming damage multiplier. 0.5 means 50% less damage.", new AcceptableValueRange<float>(0f, 1f), new ConfigurationManagerAttributes { Order = 790 }));

            CompanionDamageDealtMultiplier = config.Bind("Dvergr Companion", "DamageDealtMultiplier", 1.5f, new ConfigDescription("Outgoing damage multiplier for hired Dvergr companions. 1.5 means 50% more damage.", new AcceptableValueRange<float>(0f, 10f), new ConfigurationManagerAttributes { Order = 780 }));

            CompanionAttackSpeedMultiplier = config.Bind("Dvergr Companion", "AttackSpeedMultiplier", 3.0f, new ConfigDescription("Attack cooldown speed multiplier. 2.0 means 100% faster cooldown recovery.", new AcceptableValueRange<float>(1f, 5f), new ConfigurationManagerAttributes { Order = 770 }));

            CompanionEnableAutoHealing = config.Bind("Dvergr Companion", "EnableAutoHealing", true, new ConfigDescription("Automatically heals hired Dvergr companions when their health reaches the configured threshold.", null, new ConfigurationManagerAttributes { Order = 700 }));

            CompanionAutoHealThreshold = config.Bind("Dvergr Companion", "AutoHealThreshold", 0.5f, new ConfigDescription("Health percentage that triggers automatic healing. 0.5 means 50% health.", new AcceptableValueRange<float>(0.01f, 1f), new ConfigurationManagerAttributes { Order = 690 }));

            CompanionAutoHealCooldown = config.Bind("Dvergr Companion", "AutoHealCooldown", 60f, new ConfigDescription("Minimum number of seconds between automatic heals for each companion.", new AcceptableValueRange<float>(0f, 3600f), new ConfigurationManagerAttributes { Order = 680 }));

            EnableDebugLogs = config.Bind("Debug", "EnableDebugLogs", false, new ConfigDescription("Logs Dvergr damage events, status effects, and AI states.", null, new ConfigurationManagerAttributes { Order = 100, IsAdvanced = true }));

            CompanionUIShortcut = config.Bind("Dvergr Companion", "UIShortcut", new KeyboardShortcut(KeyCode.F9), new ConfigDescription("Keyboard shortcut to open or close the Dvergr Companion Management UI.", null, new ConfigurationManagerAttributes { Order = 1010 }));
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
