
using System;
using HexDvergrAllies;
using UnityEngine;

namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrTradeRpcManager
    {
        private const string PurchaseRequestRpc = "HexDvergrBuyRequest";
        private const string PurchaseCountKey = "hex_dvergr_purchases";

        private static int _purchaseCount;
        private static bool _initialized;

        internal static void Register()
        {
            ZRoutedRpc.instance.Register<int>(PurchaseRequestRpc, OnBuyRequest);
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

            if (!_initialized)
            {
                _purchaseCount = 0;

                if (ZoneSystem.instance.GetGlobalKey(PurchaseCountKey, out var value))
                    int.TryParse(value, out _purchaseCount);

                _initialized = true;
            }

            var requiresSap = _purchaseCount >= 2;
            var purchaseNumber = _purchaseCount + 1;

            Plugin.Log.LogInfo(
                $"[Dvergr Trade] Request: Peer={sender}, Purchase={purchaseNumber}, Trophy=1, Sap={(requiresSap ? 20 : 0)}");

            // TEST ONLY: advances the counter without charging or granting items.
            _purchaseCount++;

            ZoneSystem.instance.SetGlobalKey($"{PurchaseCountKey} {_purchaseCount}");

            Plugin.Log.LogInfo($"[Dvergr Trade] World purchase count: {_purchaseCount}");
        }
    }
}
