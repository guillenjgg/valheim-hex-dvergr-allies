using BepInEx.Configuration;

namespace HexDvergrAllies
{
    internal static class HexDvergrAlliesConfiguration
    {
        internal static ConfigEntry<bool> EnablePlayerDamageImmunity;
        internal static ConfigEntry<bool> EnableDebugLogs;

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
        }
    }
}