using System;
using System.Collections.Generic;

namespace HexDvergrAllies.Core.Features.Progression
{
    internal static class DvergrStarterMapRefreshGuard
    {
        private static bool _suppressMapRefresh;
        private static int _syncDepth;

        internal static bool ShouldRefreshMap => !_suppressMapRefresh;

        internal static IDisposable BeginKeyUpdate(string key)
        {
            return new Scope(_syncDepth > 0 ? _suppressMapRefresh : IsEncounterKey(key), _syncDepth);
        }

        internal static IDisposable BeginSync(IEnumerable<string> currentKeys, IEnumerable<string> incomingKeys)
        {
            bool currentHasMetadata;
            bool incomingHasMetadata;
            var currentSettings = GetSettings(currentKeys, out currentHasMetadata);
            var incomingSettings = GetSettings(incomingKeys, out incomingHasMetadata);
            var metadataOnly = (currentHasMetadata || incomingHasMetadata) && currentSettings.SetEquals(incomingSettings);

            // A real settings change must refresh map availability, including
            // when NoMap was removed and the incoming snapshot has only metadata.
            return new Scope(metadataOnly, _syncDepth + 1);
        }

        private static bool IsEncounterKey(string key)
        {
            return key != null && (key.StartsWith("hex_starter_v1_", StringComparison.OrdinalIgnoreCase) || key.StartsWith("hex_starter_v2_", StringComparison.OrdinalIgnoreCase));
        }

        private static HashSet<string> GetSettings(IEnumerable<string> keys, out bool hasMetadata)
        {
            var settings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            hasMetadata = false;

            foreach (var key in keys)
            {
                if (IsEncounterKey(key))
                {
                    hasMetadata = true;
                }
                else
                {
                    settings.Add(key);
                }
            }

            return settings;
        }

        private sealed class Scope : IDisposable
        {
            private readonly bool _previousSuppression;
            private readonly int _previousDepth;
            private bool _disposed;

            internal Scope(bool suppress, int depth)
            {
                _previousSuppression = _suppressMapRefresh;
                _previousDepth = _syncDepth;
                _suppressMapRefresh = suppress;
                _syncDepth = depth;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _suppressMapRefresh = _previousSuppression;
                _syncDepth = _previousDepth;
            }
        }
    }
}
