using System.Collections.Generic;

namespace HexDvergrAllies.Core
{
    internal static class PrefabHelper
    {
        internal static readonly HashSet<string> AllowedHomesteadPieces = new HashSet<string>
        {
            "piece_workbench",
            "piece_stonecutter",
            "portal_wood",
            "portal_stone"
        };
    }
}
