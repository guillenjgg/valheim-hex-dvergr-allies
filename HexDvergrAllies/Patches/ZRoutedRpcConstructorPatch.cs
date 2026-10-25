using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(ZRoutedRpc), MethodType.Constructor, typeof(bool))]
    internal static class ZRoutedRpcConstructorPatch
    {
        private static void Postfix()
        {
            DvergrTradeRpcManager.Register();
        }
    }
}