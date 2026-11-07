using System;

namespace HexDvergrAllies.Core.Features.Progression
{
    // Coordinates are local to the wreck: +Z points inland, camp center is Z=14.
    // Keeping this policy independent of Unity lets shoreline shapes be verified offline.
    internal static class DvergrStarterTerrainRules
    {
        internal static bool Evaluate(Func<float, float, float> height, Func<float, float, bool> meadows, Func<float, float, bool> occupied, float waterLevel, out string reason, bool includeWreck = true)
        {
            var shoreHeight = height(0f, 0f);
            var campHeight = height(0f, 14f);

            if (!CheckSample(0f, 14f, campHeight, 0.75f, height, meadows, occupied, waterLevel, "CampSlope", out reason))
            {
                return false;
            }

            for (var z = -4f; includeWreck && z <= 8f; z += 2f)
            {
                for (var x = -4f; x <= 4f; x += 2f)
                {
                    if (!CheckSample(x, z, shoreHeight, 2.5f, height, meadows, occupied, waterLevel, "WreckSlope", out reason))
                    {
                        return false;
                    }
                }
            }

            // The camp can be higher than the shore; only its own footprint must
            // be fairly level. Grounded lower posts support the shared roof plane.
            for (var z = 12f; z <= 16f; z += 2f)
            {
                for (var x = -2f; x <= 2f; x += 2f)
                {
                    if (!CheckSample(x, z, campHeight, 0.75f, height, meadows, occupied, waterLevel, "CampSlope", out reason))
                    {
                        return false;
                    }
                }
            }

            // Include the Dvergr, fire and dry access between the wreck and camp.
            for (var z = includeWreck ? 8f : 10f; z <= 20f; z += 2f)
            {
                if (!CheckSample(0f, z, campHeight, 3f, height, meadows, occupied, waterLevel, "AccessSlope", out reason))
                {
                    return false;
                }
            }

            reason = null;
            return true;
        }

        private static bool CheckSample(float x, float z, float reference, float maximumDelta, Func<float, float, float> height, Func<float, float, bool> meadows, Func<float, float, bool> occupied, float waterLevel, string slopeReason, out string reason)
        {
            var sampleHeight = height(x, z);
            reason = null;

            if (float.IsNaN(sampleHeight) || float.IsInfinity(sampleHeight) || sampleHeight < waterLevel + 0.3f)
            {
                reason = "WetFootprint";
            }
            else if (!meadows(x, z))
            {
                reason = "BiomeBoundary";
            }
            else if (occupied(x, z))
            {
                reason = "NearbyLocation";
            }
            else if (Math.Abs(sampleHeight - reference) > maximumDelta)
            {
                reason = slopeReason;
            }

            return reason == null;
        }
    }
}
