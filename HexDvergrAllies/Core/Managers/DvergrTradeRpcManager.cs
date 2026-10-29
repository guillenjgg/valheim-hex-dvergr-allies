using HarmonyLib;
using HexDvergrAllies.Core.Transactions;
using System;
using System.Collections.Generic;
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
        private const string PurchaseResultRpc = "HexDvergrBuyResult";
        private const string PurchaseCountKey = "hex_dvergr_purchases";
        private const float PurchaseTimeout = 15f;

        private static readonly Func<ZRoutedRpc, long> GetServerPeerID = AccessTools.MethodDelegate<Func<ZRoutedRpc, long>>(AccessTools.Method(typeof(ZRoutedRpc), "GetServerPeerID"));

        private static readonly Queue<(long Peer, ZDOID Trader, long RequestId)> _purchaseQueue = new Queue<(long, ZDOID, long)>();

        private static readonly HashSet<long> _processedTransactionIds = new HashSet<long>();

        private static long _nextTransactionId;
        private static long _pendingTransactionId;
        private static long _pendingRequestId;
        private static long _pendingPeer;
        private static ZDOID _pendingTrader;
        private static int _pendingPurchaseNumber;
        private static float _pendingStartTime;
        private static bool _hasPendingPurchase;

        private static int _purchaseCount;
        private static bool _initialized;

        internal static int CurrentPurchaseNumber { get; private set; }
        internal static int CurrentSapCost { get; private set; }
        internal static bool HasCurrentPrice { get; private set; }

        internal static void Register()
        {
            ZRoutedRpc.instance.Register<ZDOID>(PriceRequestRpc, OnPriceRequest);
            ZRoutedRpc.instance.Register<int, int>(PriceResponseRpc, OnPriceResponse);
            ZRoutedRpc.instance.Register<ZDOID, int, long>(PurchaseRequestRpc, OnBuyRequest);
            ZRoutedRpc.instance.Register<ZDOID, int, int, long, long>(PurchaseResponseRpc, OnBuyResponse);
            ZRoutedRpc.instance.Register<ZDOID, int, bool, long, long>(PurchaseCompleteRpc, OnBuyComplete);
            ZRoutedRpc.instance.Register<long, bool>(PurchaseResultRpc, OnBuyResult);

            _initialized = false;
            _hasPendingPurchase = false;
            _purchaseQueue.Clear();
            _processedTransactionIds.Clear();
            _nextTransactionId = DateTime.UtcNow.Ticks;

            DvergrTradeTransaction.Reset();

            Plugin.Log.LogInfo("[Dvergr Trade] RPC handlers registered.");
        }

        internal static void Update()
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            if (_hasPendingPurchase && Time.time - _pendingStartTime >= PurchaseTimeout)
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Purchase timed out: Peer={_pendingPeer}, Request={_pendingRequestId}");

                FinishPurchase(false);
            }

            if (!_hasPendingPurchase && _purchaseQueue.Count > 0)
            {
                ProcessNextPurchase();
            }
        }

        internal static void RequestPrice(ZDOID traderZdoId)
        {
            HasCurrentPrice = false;

            Plugin.Log.LogInfo($"[Dvergr Trade] Requesting price: Trader={traderZdoId}");

            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerID(ZRoutedRpc.instance), PriceRequestRpc, traderZdoId);
        }

        internal static void RequestPurchase(ZDOID traderZdoId)
        {
            if (!DvergrTradeTransaction.TryBegin(out var requestId))
            {
                Plugin.Log.LogInfo("[Dvergr Trade] Purchase blocked: Transaction already pending.");
                return;
            }

            Plugin.Log.LogInfo($"[Dvergr Trade] Purchase requested: Trader={traderZdoId}, Request={requestId}");

            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerID(ZRoutedRpc.instance), PurchaseRequestRpc, traderZdoId, 1, requestId);
        }

        internal static void ResetPurchaseCount()
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            if (_hasPendingPurchase)
            {
                FinishPurchase(false);
            }

            while (_purchaseQueue.Count > 0)
            {
                var request = _purchaseQueue.Dequeue();
                SendPurchaseResult(request.Peer, request.RequestId, false);
            }

            ZoneSystem.instance.SetGlobalKey($"{PurchaseCountKey} 0");

            _purchaseCount = 0;
            _initialized = true;

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
            var sapCost = _purchaseCount >= DvergrPricingConfig.FreeExtractorPurchases ? DvergrPricingConfig.ExtractorSapCost : 0;

            Plugin.Log.LogInfo($"[Dvergr Trade] Price request: Peer={sender}, Trader={traderZdoId}, Purchase={purchaseNumber}, Trophy=1, Sap={sapCost}");

            ZRoutedRpc.instance.InvokeRoutedRPC(sender, PriceResponseRpc, purchaseNumber, sapCost);
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

        private static void OnBuyRequest(long sender, ZDOID traderZdoId, int quantity, long requestId)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            Plugin.Log.LogInfo($"[Dvergr Trade] Server received purchase request: Peer={sender}, Request={requestId}, Quantity={quantity}");

            if (sender != ZNet.GetUID() && ZNet.instance.GetPeer(sender) == null)
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Purchase rejected: Unknown peer={sender}");
                return;
            }

            if (quantity != 1 || requestId <= 0)
            {
                SendPurchaseResult(sender, requestId, false);
                return;
            }

            if (!DvergrTradeSessionManager.HasAccess(sender, traderZdoId))
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Purchase rejected: Peer={sender} does not own the trading session.");
                return;
            }

            if (_hasPendingPurchase && _pendingPeer == sender)
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Duplicate purchase rejected: Peer={sender} already active.");
                SendPurchaseResult(sender, requestId, false);
                return;
            }

            foreach (var request in _purchaseQueue)
            {
                if (request.Peer == sender)
                {
                    Plugin.Log.LogWarning($"[Dvergr Trade] Duplicate purchase rejected: Peer={sender} already queued.");
                    SendPurchaseResult(sender, requestId, false);
                    return;
                }
            }

            _purchaseQueue.Enqueue((sender, traderZdoId, requestId));

            Plugin.Log.LogInfo($"[Dvergr Trade] Purchase queued: Peer={sender}, Request={requestId}, Position={_purchaseQueue.Count}");

            if (!_hasPendingPurchase)
            {
                ProcessNextPurchase();
            }
        }

        private static void ProcessNextPurchase()
        {
            if (_hasPendingPurchase)
            {
                return;
            }

            InitializePurchaseCount();

            while (_purchaseQueue.Count > 0)
            {
                var request = _purchaseQueue.Dequeue();
                var sender = request.Peer;
                var traderZdoId = request.Trader;
                var peer = ZNet.instance.GetPeer(sender);

                if (sender != ZNet.GetUID() && peer == null)
                {
                    Plugin.Log.LogWarning($"[Dvergr Trade] Queued purchase skipped: Peer disconnected={sender}");
                    continue;
                }

                var traderZdo = ZDOMan.instance.GetZDO(traderZdoId);

                if (traderZdo == null)
                {
                    RejectQueuedPurchase(request, "Unknown trader");
                    continue;
                }

                var traderPrefab = ZNetScene.instance.GetPrefab(traderZdo.GetPrefab());
                var dvergr = traderPrefab ? traderPrefab.GetComponent<Humanoid>() : null;

                if (!dvergr || dvergr.GetFaction() != Character.Faction.Dverger)
                {
                    RejectQueuedPurchase(request, "Invalid trader");
                    continue;
                }

                var playerPosition = sender == ZNet.GetUID()
                    ? Player.m_localPlayer.transform.position
                    : peer.m_refPos;

                var distance = Vector3.Distance(playerPosition, traderZdo.GetPosition());

                if (!DvergrTradeSessionManager.HasAccess(sender, traderZdoId))
                {
                    RejectQueuedPurchase(request, "Trading session expired");
                    continue;
                }

                var purchaseNumber = _purchaseCount + 1;
                var sapCost = _purchaseCount >= DvergrPricingConfig.FreeExtractorPurchases ? DvergrPricingConfig.ExtractorSapCost : 0;

                _pendingPeer = sender;
                _pendingTrader = traderZdoId;
                _pendingPurchaseNumber = purchaseNumber;
                _pendingTransactionId = ++_nextTransactionId;
                _pendingRequestId = request.RequestId;
                _pendingStartTime = Time.time;
                _hasPendingPurchase = true;

                Plugin.Log.LogInfo($"[Dvergr Trade] Purchase authorized: Peer={sender}, Request={_pendingRequestId}, Transaction={_pendingTransactionId}, Purchase={purchaseNumber}, Sap={sapCost}");

                ZRoutedRpc.instance.InvokeRoutedRPC(sender, PurchaseResponseRpc, traderZdoId, purchaseNumber, sapCost, _pendingTransactionId, _pendingRequestId);

                return;
            }
        }

        private static void RejectQueuedPurchase((long Peer, ZDOID Trader, long RequestId) request, string reason)
        {
            Plugin.Log.LogWarning($"[Dvergr Trade] Queued purchase rejected: Peer={request.Peer}, Request={request.RequestId}, Reason={reason}");

            SendPurchaseResult(request.Peer, request.RequestId, false);
        }

        private static void OnBuyResponse(long sender, ZDOID traderZdoId, int purchaseNumber, int sapCost, long transactionId, long requestId)
        {
            if (sender != GetServerPeerID(ZRoutedRpc.instance))
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Unauthorized purchase response: Sender={sender}");
                return;
            }

            if (!DvergrTradeTransaction.IsPending || DvergrTradeTransaction.RequestId != requestId)
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Authorization rejected: No matching client request={requestId}");
                return;
            }

            if (!_processedTransactionIds.Add(transactionId))
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Duplicate authorization rejected: Transaction={transactionId}");
                return;
            }

            Plugin.Log.LogInfo($"[Dvergr Trade] Client received authorization: Request={requestId}, Purchase={purchaseNumber}, Sap={sapCost}");

            var success = ExecutePurchase(traderZdoId, sapCost);

            Plugin.Log.LogInfo($"[Dvergr Trade] Client transaction result: Request={requestId}, Success={success}");

            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerID(ZRoutedRpc.instance), PurchaseCompleteRpc, traderZdoId, purchaseNumber, success, transactionId, requestId);
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

            if (!inventory.AddItem(needlePrefab, 1))
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Client purchase failed: Could not add DvergrNeedle.");
                return false;
            }

            inventory.RemoveItem(trophyName, 1);

            if (sapCost > 0)
            {
                inventory.RemoveItem(sapName, sapCost);
            }

            player.ShowPickupMessage(needlePrefab.GetComponent<ItemDrop>().m_itemData, 1);

            Plugin.Log.LogInfo($"[Dvergr Trade] Purchase inventory transaction completed. NeedleAdded=1, TrophyRemoved=1, SapRemoved={sapCost}");

            return true;
        }

        private static void OnBuyComplete(long sender, ZDOID traderZdoId, int purchaseNumber, bool success, long transactionId, long requestId)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            Plugin.Log.LogInfo($"[Dvergr Trade] Server received completion: Peer={sender}, Request={requestId}, Success={success}");

            if (!_hasPendingPurchase || sender != _pendingPeer || traderZdoId != _pendingTrader || purchaseNumber != _pendingPurchaseNumber || transactionId != _pendingTransactionId || requestId != _pendingRequestId)
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Completion rejected: No matching pending transaction.");
                return;
            }

            if (Time.time - _pendingStartTime >= PurchaseTimeout)
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Completion rejected: Transaction expired.");
                FinishPurchase(false);
                ProcessNextPurchase();
                return;
            }

            if (success)
            {
                _purchaseCount++;
                ZoneSystem.instance.SetGlobalKey($"{PurchaseCountKey} {_purchaseCount}");

                Plugin.Log.LogInfo($"[Dvergr Trade] PURCHASE COMPLETE: Purchase={purchaseNumber}, WorldPurchaseCount={_purchaseCount}");

                var nextPurchaseNumber = _purchaseCount + 1;
                var nextSapCost = _purchaseCount >= DvergrPricingConfig.FreeExtractorPurchases ? DvergrPricingConfig.ExtractorSapCost : 0;

                ZRoutedRpc.instance.InvokeRoutedRPC(sender, PriceResponseRpc, nextPurchaseNumber, nextSapCost);
            }
            else
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Purchase failed: Purchase={purchaseNumber}. Counter unchanged.");
            }

            FinishPurchase(success);
            ProcessNextPurchase();
        }

        private static void FinishPurchase(bool success)
        {
            var peer = _pendingPeer;
            var requestId = _pendingRequestId;

            _hasPendingPurchase = false;
            _pendingPeer = 0;
            _pendingRequestId = 0;
            _pendingTransactionId = 0;

            SendPurchaseResult(peer, requestId, success);
        }

        private static void SendPurchaseResult(long peer, long requestId, bool success)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, PurchaseResultRpc, requestId, success);
        }

        private static void OnBuyResult(long sender, long requestId, bool success)
        {
            if (sender != GetServerPeerID(ZRoutedRpc.instance))
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Unauthorized purchase result: Sender={sender}");
                return;
            }

            if (!DvergrTradeTransaction.TryComplete(requestId))
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Ignored stale purchase result: Request={requestId}");
                return;
            }

            Plugin.Log.LogInfo($"[Dvergr Trade] Transaction finalized: Request={requestId}, Success={success}. Client unlocked.");
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