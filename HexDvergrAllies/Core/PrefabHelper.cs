using System.Collections.Generic;

namespace HexDvergrAllies.Core
{
    internal static class PrefabHelper
    {
        internal const string Coins = "Coins";
        internal const string Softtissue = "Softtissue";
        internal const string MeadTasty = "MeadTasty";
        internal const string StaffShieldAoe = "staff_shield_aoe";
        internal const string FlametalNew = "FlametalNew";
        internal const string MoltenCore = "MoltenCore";
        internal const string DvergrNeedle = "DvergrNeedle";
        internal const string Sap = "Sap";

        internal static readonly HashSet<string> AllowedHomesteadPieces = new HashSet<string>
        {
            "piece_workbench",
            "piece_stonecutter",
            "portal_wood",
            "portal_stone"
        };

        internal static readonly string[] MistlandsTrophyPrefabs = {
            "TrophySeeker",
            "TrophyGjall",
            "TrophySeekerBrute"
        };
    }
}