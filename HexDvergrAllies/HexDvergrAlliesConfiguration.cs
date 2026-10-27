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
                    "Desired maximum following distance in meters for hired Dvergr companions.",
                    new AcceptableValueRange<float>(1f, 30f)));

            CompanionEnableJumping = config.Bind(
                "Dvergr Companion",
                "EnableJumping",
                true,
                "Allows hired Dvergr companions to jump over obstacles when supported by their movement controller.");
        }
    }
}