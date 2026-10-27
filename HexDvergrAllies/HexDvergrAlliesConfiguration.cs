
using BepInEx.Configuration;

namespace HexDvergrAllies
{
    internal static class HexDvergrAlliesConfiguration
    {
        internal static ConfigEntry<bool> EnablePlayerDamageImmunity;
        internal static ConfigEntry<bool> EnableDebugLogs;
        internal static ConfigEntry<bool> AllowTemporaryHomestead;

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
            EnablePlayerDamageImmunity = config.Bind(
                "General",
                "EnablePlayerDamageImmunity",
                true,
                "Prevents players from damaging Dvergr.");

            EnableDebugLogs = config.Bind(
                "Debug",
                "EnableDebugLogs",
                true,
                "Logs Dvergr damage events, status effects, and AI states.");

            AllowTemporaryHomestead = config.Bind(
                "Dvergr Wards",
                "AllowTemporaryHomestead",
                true,
                "Allows players to place a workbench, stonecutter, wood portal, and stone portal inside Dvergr ward-protected areas. All other structures remain restricted.");

            CompanionMovementSpeedMultiplier = config.Bind(
                "Dvergr Companion",
                "MovementSpeedMultiplier",
                2.5f,
                new ConfigDescription(
                    "Movement speed multiplier for hired Dvergr companions.",
                    new AcceptableValueRange<float>(1f, 5f)));

            CompanionFollowDistance = config.Bind(
                "Dvergr Companion",
                "FollowDistance",
                5f,
                new ConfigDescription(
                    "Desired following distance in meters. Not implemented yet.",
                    new AcceptableValueRange<float>(1f, 30f)));

            CompanionEnableJumping = config.Bind(
                "Dvergr Companion",
                "EnableJumping",
                true,
                "Reserved for a future companion obstacle navigation feature.");

            CompanionHealthMultiplier = config.Bind(
                "Dvergr Companion",
                "HealthMultiplier",
                2f,
                new ConfigDescription(
                    "Maximum health multiplier for hired Dvergr companions.",
                    new AcceptableValueRange<float>(1f, 10f)));

            CompanionDamageTakenMultiplier = config.Bind(
                "Dvergr Companion",
                "DamageTakenMultiplier",
                0.5f,
                new ConfigDescription(
                    "Incoming damage multiplier. 0.5 means 50% less damage.",
                    new AcceptableValueRange<float>(0f, 1f)));

            CompanionDamageDealtMultiplier = config.Bind(
                "Dvergr Companion",
                "DamageDealtMultiplier",
                1.5f,
                new ConfigDescription(
                    "Outgoing damage multiplier for hired Dvergr companions. 1.5 means 50% more damage.",
                    new AcceptableValueRange<float>(0f, 10f)));

            CompanionAttackSpeedMultiplier = config.Bind(
                "Dvergr Companion",
                "AttackSpeedMultiplier",
                1.25f,
                new ConfigDescription(
                    "Attack cooldown speed multiplier. 1.25 means 25% faster cooldown recovery.",
                    new AcceptableValueRange<float>(1f, 5f)));

            CompanionEnableAutoHealing = config.Bind(
                "Dvergr Companion",
                "EnableAutoHealing",
                true,
                "Automatically heals hired Dvergr companions when their health reaches the configured threshold.");

            CompanionAutoHealThreshold = config.Bind(
                "Dvergr Companion",
                "AutoHealThreshold",
                0.5f,
                new ConfigDescription(
                    "Health percentage that triggers automatic healing. 0.5 means 50% health.",
                    new AcceptableValueRange<float>(0.01f, 1f)));

            CompanionAutoHealCooldown = config.Bind(
                "Dvergr Companion",
                "AutoHealCooldown",
                60f,
                new ConfigDescription(
                    "Minimum number of seconds between automatic heals for each companion.",
                    new AcceptableValueRange<float>(0f, 3600f)));
        }
    }
}
