using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(ZNet), "Update")]
    internal static class DvergrTradeQueueUpdatePatch
    {
        private static void Postfix()
        {
            DvergrTradeRpcManager.Update();
            DvergrTradeSessionManager.Update();
        }
    }
}
