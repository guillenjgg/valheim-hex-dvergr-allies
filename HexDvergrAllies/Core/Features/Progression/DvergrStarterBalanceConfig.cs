using System.Collections.Generic;

namespace HexDvergrAllies.Core.Features.Progression
{
    internal enum DvergrStarterTier
    {
        Meadows,
        BlackForest,
        Swamp,
        Mountains,
        Plains,
        Mistlands,
        Ashlands,
        DeepNorth
    }

    internal sealed class DvergrStarterTierProfile
    {
        internal readonly float Health;
        internal readonly float ArbalestPierce;
        internal readonly float MeleeBlunt;

        internal DvergrStarterTierProfile(float health, float pierce, float blunt)
        {
            Health = health;
            ArbalestPierce = pierce;
            MeleeBlunt = blunt;
        }
    }

    internal static class DvergrStarterBalanceConfig
    {
        internal const float FaderHealthMultiplier = 2f;
        internal const float FaderBonusMultiplier = 1.5f;
        internal const float FaderDamageTakenMultiplier = 0.25f;
        private static DvergrStarterTierProfile _deepNorth;
        // Edit starter balance here, then rebuild. These are absolute base stats.
        // Ashlands matches DvergerAshlands and its shooting/melee weapon prefabs.
        private static readonly Dictionary<DvergrStarterTier, DvergrStarterTierProfile> Profiles = new Dictionary<DvergrStarterTier, DvergrStarterTierProfile>
        {
            { DvergrStarterTier.Meadows, new DvergrStarterTierProfile(health: 200f, pierce: 40f, blunt: 15f) },
            { DvergrStarterTier.BlackForest, new DvergrStarterTierProfile(health: 330f, pierce: 70f, blunt: 25f) },
            { DvergrStarterTier.Swamp, new DvergrStarterTierProfile(health: 470f, pierce: 95f, blunt: 35f) },
            { DvergrStarterTier.Mountains, new DvergrStarterTierProfile(health: 600f, pierce: 125f, blunt: 45f) },
            { DvergrStarterTier.Plains, new DvergrStarterTierProfile(health: 730f, pierce: 155f, blunt: 50f) },
            { DvergrStarterTier.Mistlands, new DvergrStarterTierProfile(health: 870f, pierce: 180f, blunt: 60f) },
            { DvergrStarterTier.Ashlands, new DvergrStarterTierProfile(health: 1000f, pierce: 210f, blunt: 70f) }
        };

        internal static DvergrStarterTierProfile Get(DvergrStarterTier tier)
        {
            if (tier == DvergrStarterTier.DeepNorth)
            {
                var ashlands = Profiles[DvergrStarterTier.Ashlands];

                if (_deepNorth == null)
                {
                    _deepNorth = new DvergrStarterTierProfile(ashlands.Health * FaderHealthMultiplier, ashlands.ArbalestPierce * FaderBonusMultiplier, ashlands.MeleeBlunt * FaderBonusMultiplier);
                }

                return _deepNorth;
            }

            DvergrStarterTierProfile profile;

            return Profiles.TryGetValue(tier, out profile) ? profile : Profiles[DvergrStarterTier.Meadows];
        }
    }
}
