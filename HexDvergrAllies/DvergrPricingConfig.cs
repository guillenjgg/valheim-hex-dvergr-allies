namespace HexDvergrAllies
{
    /// <summary>
    /// Centralizes all Dvergr store purchase prices so they can be tuned from a single location.
    /// </summary>
    internal static class DvergrPricingConfig
    {
        /// <summary>
        /// Number of extractor purchases before Sap is required.
        /// </summary>
        internal const int FreeExtractorPurchases = 2;

        /// <summary>
        /// Sap required after the initial extractor purchases.
        /// </summary>
        internal const int ExtractorSapCost = 10;

        /// <summary>
        /// Tasty Mead required to hire a Dvergr companion.
        /// </summary>
        internal const int ForgePactCost = 2;

        internal const string ForgePactCostDisplay = "2 Tasty Mead";

        /// <summary>
        /// Flametal received per Ashlands purchase.
        /// </summary>
        internal const int AshlandsFlametalQuantity = 10;

        /// <summary>
        /// Molten Cores required per Ashlands purchase.
        /// </summary>
        internal const int AshlandsMoltenCoreCost = 1;

        internal const string AshlandsFlametalCostDisplay = "1 Molten Core";
    }
}
