using System.Globalization;

namespace HexDvergrAllies.Core.Features.Progression
{
    internal static class DvergrStarterEligibility
    {
        internal static string WorldPrefix(long characterId)
        {
            return "hex_starter_v2_" + characterId.ToString(CultureInfo.InvariantCulture) + "_";
        }

        internal static bool CanHire(long characterId, long intendedCharacter, bool introductionComplete, bool pactConsumed)
        {
            return characterId != 0L && intendedCharacter == characterId && !pactConsumed && !introductionComplete;
        }
    }
}
