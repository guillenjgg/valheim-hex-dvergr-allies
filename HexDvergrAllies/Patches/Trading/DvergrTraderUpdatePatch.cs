using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Trader), "Update")]
    internal static class DvergrTraderUpdatePatch
    {
        private static bool Prefix(Trader __instance)
        {
            return !DvergrTraderManager.IsDvergrTrader(__instance);
        }
    }
}