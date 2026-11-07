namespace HexDvergrAllies.Core
{
    internal static class HexLog
    {
        private static bool Enabled => HexDvergrAlliesConfiguration.EnableDebugLogs != null && HexDvergrAlliesConfiguration.EnableDebugLogs.Value;

        internal static void LogInfo(object message)
        {
            if (Enabled)
            {
                Plugin.Log.LogInfo(message);
            }
        }

        internal static void LogWarning(object message)
        {
            if (Enabled)
            {
                Plugin.Log.LogWarning(message);
            }
        }

        internal static void LogError(object message)
        {
            if (Enabled)
            {
                Plugin.Log.LogError(message);
            }
        }
    }
}
