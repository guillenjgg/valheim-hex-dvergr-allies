using BepInEx.Configuration;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace HexDvergrAllies.Core
{
    internal static class HexConfigurationMigration
    {
        private static readonly PropertyInfo UnboundEntriesProperty = typeof(ConfigFile).GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo BoundEntriesProperty = typeof(ConfigFile).GetProperty("Entries", BindingFlags.Instance | BindingFlags.NonPublic);

        // Bind consumes saved values for current options and supplies defaults for missing ones.
        // Anything left unbound is obsolete and must not be written back by ConfigFile.Save.
        internal static bool Complete(ConfigFile config)
        {
            var unbound = (Dictionary<ConfigDefinition, string>)UnboundEntriesProperty.GetValue(config, null);
            var removedEntries = unbound.Count > 0;
            unbound.Clear();

            var entries = (Dictionary<ConfigDefinition, ConfigEntryBase>)BoundEntriesProperty.GetValue(config, null);
            var ordered = entries.OrderBy(pair => pair.Key.Section, System.StringComparer.Ordinal)
                .ThenByDescending(pair => pair.Value.Description.Tags.OfType<ConfigurationManagerAttributes>()
                    .Select(attributes => attributes.Order ?? 0).FirstOrDefault())
                .ThenBy(pair => pair.Key.Key, System.StringComparer.Ordinal).ToArray();

            entries.Clear();
            foreach (var entry in ordered)
            {
                entries.Add(entry.Key, entry.Value);
            }

            return removedEntries;
        }
    }
}
