using HarmonyLib;
using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.Networking;
using HexDvergrAllies.Core.Trading;

namespace HexDvergrAllies.Patches
{
    /// <summary>
    /// RPC registration patch
    /// </summary>
    [HarmonyPatch(typeof(ZRoutedRpc), MethodType.Constructor, typeof(bool))]
    internal static class ZRoutedRpcRegistrationPatch
    {
        private static void Postfix()
        {
            DvergrTradeRpcManager.Register();
            DvergrTradeSessionManager.Register();
            DvergrCompanionPortalRelocationManager.Register();
            DvergrRpcManager.RegisterRoutedRpcs();
        }
    }
}
