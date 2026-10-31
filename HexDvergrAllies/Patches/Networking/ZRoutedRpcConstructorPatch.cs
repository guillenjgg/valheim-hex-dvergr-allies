using HarmonyLib;
using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.Trading;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(ZRoutedRpc), MethodType.Constructor, typeof(bool))]
    internal static class ZRoutedRpcConstructorPatch
    {
        private static void Postfix()
        {
            DvergrTradeRpcManager.Register();
            DvergrTradeSessionManager.Register();
            DvergrCompanionPortalRelocationManager.Register();
        }
    }
}
