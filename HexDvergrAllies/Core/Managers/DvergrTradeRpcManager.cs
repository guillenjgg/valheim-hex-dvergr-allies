
using HarmonyLib;
using System;
using UnityEngine;

namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrTradeRpcManager
    {
        private const string PriceRequestRpc = "HexDvergrPriceRequest";
        private const string PriceResponseRpc = "HexDvergrPriceResponse";
        private const string PurchaseRequestRpc = "HexDvergrBuyRequest";
        private const string PurchaseResponseRpc = "HexDvergrBuyResponse";
        private const string PurchaseCompleteRpc = "HexDvergrBuyComplete";
        private const string PurchaseCountKey = "hex_dvergr_purchases";
        private const float PurchaseTimeout = 15f;

        private static readonly Func<ZRoutedRpc, long> GetServerPeerID =
            AccessTools.MethodDelegate<Func<ZRoutedRpc, long>>(
                AccessTools.Method(typeof(ZRoutedRpc), "GetServerPeerID"));

        private static int _purchaseCount;
        private static bool _initialized;

        private static long _pendingPeer;
        private static ZDOID _pendingTrader;
        private static int _pendingPurchaseNumber;
        private static float _pendingStartTime;
        private static bool _hasPendingPurchase;

        internal static int CurrentPurchaseNumber { get; private set; }
        internal static int CurrentSapCost { get; private set; }
        internal static bool HasCurrentPrice { get; private set; }

        internal static void Register()
        {
            ZRoutedRpc.instance.Register<ZDOID>(PriceRequestRpc, OnPriceRequest);
            ZRoutedRpc.instance.Register<int, int>(PriceResponseRpc, OnPriceResponse);
            ZRoutedRpc.instance.Register<ZDOID, int>(PurchaseRequestRpc, OnBuyRequest);
            ZRoutedRpc.instance.Register<ZDOID, int, int>(PurchaseResponseRpc, OnBuyResponse);
            ZRoutedRpc.instance.Register<ZDOID, int, bool>(PurchaseCompleteRpc, OnBuyComplete);

            _initialized = false;
            _hasPendingPurchase = false;

            Plugin.Log.LogInfo("[Dvergr Trade] RPC handlers registered.");
        }

        internal static void RequestPrice(ZDOID traderZdoId)
        {
            HasCurrentPrice = false;

            Plugin.Log.LogInfo($"[Dvergr Trade] Requesting price: Trader={traderZdoId}");

            ZRoutedRpc.instance.InvokeRoutedRPC(
                GetServerPeerID(ZRoutedRpc.instance),
                PriceRequestRpc,
                traderZdoId);
        }

        internal static void RequestPurchase(ZDOID traderZdoId)
        {
            Plugin.Log.LogInfo($"[Dvergr Trade] Purchase requested: Trader={traderZdoId}");

            ZRoutedRpc.instance.InvokeRoutedRPC(
                GetServerPeerID(ZRoutedRpc.instance),
                PurchaseRequestRpc,
                traderZdoId,
                1);
        }

        internal static void ResetPurchaseCount()
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            ZoneSystem.instance.SetGlobalKey($"{PurchaseCountKey} 0");

            _purchaseCount = 0;
            _initialized = true;
            _hasPendingPurchase = false;

            Plugin.Log.LogInfo("[Dvergr Trade] World purchase counter reset to 0.");
        }

        private static void OnPriceRequest(long sender, ZDOID traderZdoId)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            if (sender != ZNet.GetUID() && ZNet.instance.GetPeer(sender) == null)
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Price rejected: Unknown peer={sender}");
                return;
            }

            InitializePurchaseCount();

            var purchaseNumber = _purchaseCount + 1;
            var sapCost = _purchaseCount >= 2 ? 20 : 0;

            Plugin.Log.LogInfo($"[Dvergr Trade] Price request: Peer={sender}, Trader={traderZdoId}, Purchase={purchaseNumber}, Trophy=1, Sap={sapCost}");

            ZRoutedRpc.instance.InvokeRoutedRPC(
                sender,
                PriceResponseRpc,
                purchaseNumber,
                sapCost);
        }

        private static void OnPriceResponse(long sender, int purchaseNumber, int sapCost)
        {
            if (sender != GetServerPeerID(ZRoutedRpc.instance))
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Price response rejected: Sender={sender}");
                return;
            }

            CurrentPurchaseNumber = purchaseNumber;
            CurrentSapCost = sapCost;
            HasCurrentPrice = true;

            Plugin.Log.LogInfo($"[Dvergr Trade] Price received: Purchase={purchaseNumber}, Trophy=1, Sap={sapCost}");
        }

        private static void OnBuyRequest(long sender, ZDOID traderZdoId, int quantity)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            Plugin.Log.LogInfo($"[Dvergr Trade] Server received purchase request: Peer={sender}, Trader={traderZdoId}, Quantity={quantity}");

            if (quantity != 1)
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Purchase rejected: Invalid quantity.");
                return;
            }

            var peer = ZNet.instance.GetPeer(sender);

            if (sender != ZNet.GetUID() && peer == null)
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Purchase rejected: Unknown peer={sender}");
                return;
            }

            if (_hasPendingPurchase)
            {
                if (Time.time - _pendingStartTime < PurchaseTimeout)
                {
                    Plugin.Log.LogWarning($"[Dvergr Trade] Purchase rejected: Another transaction is pending for peer={_pendingPeer}");
                    return;
                }

                Plugin.Log.LogWarning("[Dvergr Trade] Previous purchase timed out. Clearing reservation.");
                _hasPendingPurchase = false;
            }

            var traderZdo = ZDOMan.instance.GetZDO(traderZdoId);

            if (traderZdo == null)
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Purchase rejected: Unknown trader={traderZdoId}");
                return;
            }

            var traderObject = ZNetScene.instance.FindInstance(traderZdoId);
            var trader = traderObject ? traderObject.GetComponent<Trader>() : null;

            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Purchase rejected: Invalid Dvergr trader={traderZdoId}");
                return;
            }

            var playerPosition = sender == ZNet.GetUID()
                ? Player.m_localPlayer.transform.position
                : peer.m_refPos;

            var distance = Vector3.Distance(playerPosition, trader.transform.position);

            if (distance > DvergrTraderManager.InteractionDistance)
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Purchase rejected: Player too far away. Distance={distance:F1}");
                return;
            }

            InitializePurchaseCount();

            var purchaseNumber = _purchaseCount + 1;
            var sapCost = _purchaseCount >= 2 ? 20 : 0;

            _pendingPeer = sender;
            _pendingTrader = traderZdoId;
            _pendingPurchaseNumber = purchaseNumber;
            _pendingStartTime = Time.time;
            _hasPendingPurchase = true;

            Plugin.Log.LogInfo($"[Dvergr Trade] Purchase authorized: Peer={sender}, Purchase={purchaseNumber}, Trophy=1, Sap={sapCost}, Distance={distance:F1}");

            ZRoutedRpc.instance.InvokeRoutedRPC(
                sender,
                PurchaseResponseRpc,
                traderZdoId,
                purchaseNumber,
                sapCost);
        }

        private static void OnBuyResponse(long sender, ZDOID traderZdoId, int purchaseNumber, int sapCost)
        {
            if (sender != GetServerPeerID(ZRoutedRpc.instance))
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Unauthorized purchase response: Sender={sender}");
                return;
            }

            Plugin.Log.LogInfo($"[Dvergr Trade] Client received authorization: Purchase={purchaseNumber}, Trophy=1, Sap={sapCost}");

            var success = ExecutePurchase(traderZdoId, sapCost);

            Plugin.Log.LogInfo($"[Dvergr Trade] Client transaction result: Purchase={purchaseNumber}, Success={success}");

            ZRoutedRpc.instance.InvokeRoutedRPC(
                GetServerPeerID(ZRoutedRpc.instance),
                PurchaseCompleteRpc,
                traderZdoId,
                purchaseNumber,
                success);
        }

        private static bool ExecutePurchase(ZDOID traderZdoId, int sapCost)
        {
            var player = Player.m_localPlayer;
            var inventory = player.GetInventory();

            var traderObject = ZNetScene.instance.FindInstance(traderZdoId);
            var trader = traderObject ? traderObject.GetComponent<Trader>() : null;

            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Client purchase failed: Trader unavailable.");
                return false;
            }

            var distance = Vector3.Distance(player.transform.position, trader.transform.position);

            if (distance > DvergrTraderManager.InteractionDistance)
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Client purchase failed: Too far away ({distance:F1}m).");
                return false;
            }

            var trophyName = DvergrTraderManager.GetPaymentTrophy(inventory);
            var sapPrefab = ObjectDB.instance.GetItemPrefab("Sap");
            var needlePrefab = ObjectDB.instance.GetItemPrefab("DvergrNeedle");

            if (trophyName == null)
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Client purchase failed: No eligible trophy.");
                return false;
            }

            var sapName = sapPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
            var sapCount = inventory.CountItems(sapName);

            Plugin.Log.LogInfo($"[Dvergr Trade] Inventory check: Trophy={trophyName}, SapAvailable={sapCount}, SapRequired={sapCost}");

            if (sapCount < sapCost)
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Client purchase failed: Insufficient Sap.");
                return false;
            }

            if (!inventory.CanAddItem(needlePrefab, 1))
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Client purchase failed: Inventory full.");
                return false;
            }

            Plugin.Log.LogInfo("[Dvergr Trade] Adding DvergrNeedle to inventory.");

            if (!inventory.AddItem(needlePrefab, 1))
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Client purchase failed: Could not add DvergrNeedle.");
                return false;
            }

            Plugin.Log.LogInfo($"[Dvergr Trade] Removing payment: Trophy={trophyName}, Sap={sapCost}");

            inventory.RemoveItem(trophyName, 1);

            if (sapCost > 0)
            {
                inventory.RemoveItem(sapName, sapCost);
            }

            player.ShowPickupMessage(needlePrefab.GetComponent<ItemDrop>().m_itemData, 1);

            Plugin.Log.LogInfo($"[Dvergr Trade] Purchase inventory transaction completed. NeedleAdded=1, TrophyRemoved=1, SapRemoved={sapCost}");

            return true;
        }

        private static void OnBuyComplete(long sender, ZDOID traderZdoId, int purchaseNumber, bool success)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            Plugin.Log.LogInfo($"[Dvergr Trade] Server received completion: Peer={sender}, Purchase={purchaseNumber}, Success={success}");

            if (!_hasPendingPurchase ||
                sender != _pendingPeer ||
                traderZdoId != _pendingTrader ||
                purchaseNumber != _pendingPurchaseNumber)
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Completion rejected: No matching pending transaction.");
                return;
            }

            _hasPendingPurchase = false;

            if (!success)
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Purchase cancelled: Purchase={purchaseNumber}. Counter unchanged.");
                return;
            }

            _purchaseCount++;

            ZoneSystem.instance.SetGlobalKey($"{PurchaseCountKey} {_purchaseCount}");

            Plugin.Log.LogInfo($"[Dvergr Trade] PURCHASE COMPLETE: Purchase={purchaseNumber}, WorldPurchaseCount={_purchaseCount}, SavedGlobalKey={PurchaseCountKey} {_purchaseCount}");

            var nextPurchaseNumber = _purchaseCount + 1;
            var nextSapCost = _purchaseCount >= 2 ? 20 : 0;

            ZRoutedRpc.instance.InvokeRoutedRPC(
                sender,
                PriceResponseRpc,
                nextPurchaseNumber,
                nextSapCost);
        }

        private static void InitializePurchaseCount()
        {
            if (_initialized)
            {
                return;
            }

            _purchaseCount = 0;

            if (ZoneSystem.instance.GetGlobalKey(PurchaseCountKey, out var value))
            {
                int.TryParse(value, out _purchaseCount);
            }

            _initialized = true;

            Plugin.Log.LogInfo($"[Dvergr Trade] Loaded world purchase counter: {_purchaseCount}");
        }
    }
}
