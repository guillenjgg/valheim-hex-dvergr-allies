using HarmonyLib;
using System;

namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrTradeRpcManager
    {
        private const string PurchaseRequestRpc = "HexDvergrBuyRequest";
        private const string PurchaseResponseRpc = "HexDvergrBuyResponse";
        private const string PurchaseCountKey = "hex_dvergr_purchases";

        private static readonly Func<ZRoutedRpc, long> GetServerPeerID =
    AccessTools.MethodDelegate<Func<ZRoutedRpc, long>>(
        AccessTools.Method(typeof(ZRoutedRpc), "GetServerPeerID"));

        private static int _purchaseCount;
        private static bool _initialized;

        internal static void Register()
        {
            ZRoutedRpc.instance.Register<int>(PurchaseRequestRpc, OnBuyRequest);
            ZRoutedRpc.instance.Register<int, int>(PurchaseResponseRpc, OnBuyResponse);
        }

        internal static void RequestPurchase()
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(PurchaseRequestRpc, 1);
        }

        private static void OnBuyRequest(long sender, int quantity)
        {
            if (!ZNet.instance.IsServer())
                return;

            if (quantity != 1)
                return;

            if (sender != ZNet.GetUID() && ZNet.instance.GetPeer(sender) == null)
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Rejected unknown peer: {sender}");
                return;
            }

            // A valid peer is not yet proof of proximity to a trader.
            // Trader identity and distance require additional request data.

            if (!_initialized)
            {
                _purchaseCount = 0;

                if (ZoneSystem.instance.GetGlobalKey(PurchaseCountKey, out var value))
                    int.TryParse(value, out _purchaseCount);

                _initialized = true;
            }

            var purchaseNumber = _purchaseCount + 1;
            var sapCost = _purchaseCount >= 2 ? 20 : 0;

            Plugin.Log.LogInfo(
                $"[Dvergr Trade] Request: Peer={sender}, Purchase={purchaseNumber}, Trophy=1, Sap={sapCost}");

            ZRoutedRpc.instance.InvokeRoutedRPC(sender, PurchaseResponseRpc, purchaseNumber, sapCost);
        }

        private static void OnBuyResponse(long sender, int purchaseNumber, int sapCost)
        {
            if (sender != GetServerPeerID(ZRoutedRpc.instance))
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Trade] Rejected response from unauthorized peer: {sender}");
                return;
            }

            Plugin.Log.LogInfo(
                $"[Dvergr Trade] Response: Sender={sender}, Purchase={purchaseNumber}, Trophy=1, Sap={sapCost}");
        }
    }
}
