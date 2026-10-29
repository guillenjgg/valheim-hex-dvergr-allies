namespace HexDvergrAllies
{
    /// <summary>
    /// Centralizes all Dvergr store purchase prices so they can be tuned from a single location.
    /// </summary>
    internal static class DvergrPricingConfig
    {
        /// <summary>
        /// Number of extractor purchases a player can make before Sap is required in addition to the Trophy.
        /// </summary>
        internal const int FreeExtractorPurchases = 2;

        /// <summary>
        /// Amount of Sap required per extractor purchase once <see cref="FreeExtractorPurchases"/> has been exceeded.
        /// </summary>
        internal const int ExtractorSapCost = 10;

        /// <summary>
        /// Amount of Tasty Mead required to forge a pact (hire) with a Dvergr companion.
        /// </summary>
        internal const int ForgePactCost = 2;

        /// <summary>
        /// Human-readable description of the Forge Pact cost, kept in sync with <see cref="ForgePactCost"/>.
        /// </summary>
        internal const string ForgePactCostDisplay = "2 Tasty Mead";
    }
}
