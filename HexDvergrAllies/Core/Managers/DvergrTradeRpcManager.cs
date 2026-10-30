
using HarmonyLib;
using HexDvergrAllies.Core.Transactions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexDvergrAllies.Core.Managers
{
    internal enum DvergrPurchaseType
    {
        Extractor = 1,
        Flametal = 2
    }

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
        private static readonly Queue<(long Peer, ZDOID Trader, int Type, long RequestId)> _purchaseQueue = new Queue<(long, ZDOID, int, long)>();
        private static readonly HashSet<long> _processedTransactionIds = new HashSet<long>();

        private static long _nextTransactionId;
        private static long _pendingTransactionId;
        private static long _pendingRequestId;
        private static long _pendingPeer;
        private static ZDOID _pendingTrader;
        private static int _pendingPurchaseNumber;
        private static int _pendingPurchaseType;
        private static float _pendingStartTime;
        private static bool _hasPendingPurchase;

        private static int _purchaseCount;
        private static bool _initialized;
        private static int _requestedPurchaseType;

        internal static int CurrentPurchaseNumber { get; private set; }
        internal static int CurrentSapCost { get; private set; }
        internal static bool HasCurrentPrice { get; private set; }

        internal static void Register()
        {
            ZRoutedRpc.instance.Register<ZDOID>(PriceRequestRpc, OnPriceRequest);
            ZRoutedRpc.instance.Register<int, int>(PriceResponseRpc, OnPriceResponse);
            ZRoutedRpc.instance.Register<ZDOID, int, long>(PurchaseRequestRpc, OnBuyRequest);
            ZRoutedRpc.instance.Register<ZDOID, int, int, int, long, long>(PurchaseResponseRpc, OnBuyResponse);
            ZRoutedRpc.instance.Register<ZDOID, int, int, bool, long, long>(PurchaseCompleteRpc, OnBuyComplete);
            ZRoutedRpc.instance.Register<long, bool>(PurchaseResultRpc, OnBuyResult);

            _initialized = false;
            _hasPendingPurchase = false;
            _purchaseQueue.Clear();
            _processedTransactionIds.Clear();
            _nextTransactionId = DateTime.UtcNow.Ticks;
            _requestedPurchaseType = 0;
            HasCurrentPrice = false;

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

        internal static void RequestPurchase(ZDOID traderZdoId, DvergrPurchaseType purchaseType)
        {
            if (!IsValidPurchaseType((int)purchaseType))
            {
                return;
            }

            if (!DvergrTradeTransaction.TryBegin(out long requestId))
            {
                Plugin.Log.LogInfo("[Dvergr Trade] Purchase blocked: Transaction already pending.");
                return;
            }

            _requestedPurchaseType = (int)purchaseType;

            Plugin.Log.LogInfo($"[Dvergr Trade] Purchase requested: Trader={traderZdoId}, Type={purchaseType}, Request={requestId}");

            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerID(ZRoutedRpc.instance), PurchaseRequestRpc, traderZdoId, (int)purchaseType, requestId);
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

        private static bool IsValidPurchaseType(int purchaseType)
        {
            return purchaseType == (int)DvergrPurchaseType.Extractor || purchaseType == (int)DvergrPurchaseType.Flametal;
        }

        private static bool IsValidTraderPurchase(ZDO traderZdo, int purchaseType)
        {
            if (traderZdo == null || !IsValidPurchaseType(purchaseType))
            {
                return false;
            }

            GameObject traderPrefab = ZNetScene.instance.GetPrefab(traderZdo.GetPrefab());
            Humanoid dvergr = traderPrefab ? traderPrefab.GetComponent<Humanoid>() : null;

            if (!dvergr || dvergr.GetFaction() != Character.Faction.Dverger)
            {
                return false;
            }

            Heightmap.Biome biome = Heightmap.FindBiome(traderZdo.GetPosition());

            if (purchaseType == (int)DvergrPurchaseType.Extractor)
            {
                return biome == Heightmap.Biome.Mistlands;
            }

            return biome == Heightmap.Biome.AshLands;
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

            if (!DvergrTradeSessionManager.HasAccess(sender, traderZdoId))
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Price rejected: No trading session for peer={sender}");
                return;
            }

            InitializePurchaseCount();

            int purchaseNumber = _purchaseCount + 1;
            int sapCost = _purchaseCount >= DvergrPricingConfig.FreeExtractorPurchases ? DvergrPricingConfig.ExtractorSapCost : 0;

            ZRoutedRpc.instance.InvokeRoutedRPC(sender, PriceResponseRpc, purchaseNumber, sapCost);
        }

        private static void OnPriceResponse(long sender, int purchaseNumber, int sapCost)
        {
            if (sender != GetServerPeerID(ZRoutedRpc.instance))
            {
                return;
            }

            CurrentPurchaseNumber = purchaseNumber;
            CurrentSapCost = sapCost;
            HasCurrentPrice = true;

            Plugin.Log.LogInfo($"[Dvergr Trade] Price received: Purchase={purchaseNumber}, Sap={sapCost}");
        }

        private static void OnBuyRequest(long sender, ZDOID traderZdoId, int purchaseType, long requestId)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            if (sender != ZNet.GetUID() && ZNet.instance.GetPeer(sender) == null)
            {
                return;
            }

            if (!IsValidPurchaseType(purchaseType) || requestId <= 0)
            {
                SendPurchaseResult(sender, requestId, false);
                return;
            }

            if (!DvergrTradeSessionManager.HasAccess(sender, traderZdoId))
            {
                SendPurchaseResult(sender, requestId, false);
                return;
            }

            if (_hasPendingPurchase && _pendingPeer == sender)
            {
                SendPurchaseResult(sender, requestId, false);
                return;
            }

            foreach (var request in _purchaseQueue)
            {
                if (request.Peer == sender)
                {
                    SendPurchaseResult(sender, requestId, false);
                    return;
                }
            }

            _purchaseQueue.Enqueue((sender, traderZdoId, purchaseType, requestId));

            Plugin.Log.LogInfo($"[Dvergr Trade] Purchase queued: Peer={sender}, Type={(DvergrPurchaseType)purchaseType}, Request={requestId}, Position={_purchaseQueue.Count}");

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
                long sender = request.Peer;
                ZNetPeer peer = ZNet.instance.GetPeer(sender);

                if (sender != ZNet.GetUID() && peer == null)
                {
                    Plugin.Log.LogWarning($"[Dvergr Trade] Queued purchase skipped: Peer disconnected={sender}");
                    continue;
                }

                ZDO traderZdo = ZDOMan.instance.GetZDO(request.Trader);

                if (!IsValidTraderPurchase(traderZdo, request.Type))
                {
                    RejectQueuedPurchase(request, "Item unavailable from this biome");
                    continue;
                }

                if (!DvergrTradeSessionManager.HasAccess(sender, request.Trader))
                {
                    RejectQueuedPurchase(request, "Trading session expired");
                    continue;
                }

                ZDOID characterId = sender == ZNet.GetUID() ? ZNet.instance.LocalPlayerCharacterID : peer.m_characterID;
                ZDO playerZdo = ZDOMan.instance.GetZDO(characterId);

                if (playerZdo == null || Vector3.Distance(playerZdo.GetPosition(), traderZdo.GetPosition()) > DvergrTraderManager.InteractionDistance + 2f)
                {
                    RejectQueuedPurchase(request, "Player out of range");
                    continue;
                }

                int purchaseNumber = request.Type == (int)DvergrPurchaseType.Extractor ? _purchaseCount + 1 : 0;
                int sapCost = request.Type == (int)DvergrPurchaseType.Extractor && _purchaseCount >= DvergrPricingConfig.FreeExtractorPurchases ? DvergrPricingConfig.ExtractorSapCost : 0;

                _pendingPeer = sender;
                _pendingTrader = request.Trader;
                _pendingPurchaseType = request.Type;
                _pendingPurchaseNumber = purchaseNumber;
                _pendingTransactionId = ++_nextTransactionId;
                _pendingRequestId = request.RequestId;
                _pendingStartTime = Time.time;
                _hasPendingPurchase = true;

                Plugin.Log.LogInfo($"[Dvergr Trade] Purchase authorized: Peer={sender}, Type={(DvergrPurchaseType)request.Type}, Request={request.RequestId}, Transaction={_pendingTransactionId}, Sap={sapCost}");

                ZRoutedRpc.instance.InvokeRoutedRPC(sender, PurchaseResponseRpc, request.Trader, request.Type, purchaseNumber, sapCost, _pendingTransactionId, request.RequestId);
                return;
            }
        }

        private static void RejectQueuedPurchase((long Peer, ZDOID Trader, int Type, long RequestId) request, string reason)
        {
            Plugin.Log.LogWarning($"[Dvergr Trade] Queued purchase rejected: Peer={request.Peer}, Type={request.Type}, Request={request.RequestId}, Reason={reason}");
            SendPurchaseResult(request.Peer, request.RequestId, false);
        }

        private static void OnBuyResponse(long sender, ZDOID traderZdoId, int purchaseType, int purchaseNumber, int sapCost, long transactionId, long requestId)
        {
            if (sender != GetServerPeerID(ZRoutedRpc.instance))
            {
                return;
            }

            if (!DvergrTradeTransaction.IsPending || DvergrTradeTransaction.RequestId != requestId || purchaseType != _requestedPurchaseType)
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Authorization rejected: Request={requestId}, Type={purchaseType}");
                return;
            }

            if (!IsValidPurchaseType(purchaseType) || !_processedTransactionIds.Add(transactionId))
            {
                return;
            }

            bool success = ExecutePurchase(traderZdoId, (DvergrPurchaseType)purchaseType, sapCost);

            Plugin.Log.LogInfo($"[Dvergr Trade] Client transaction result: Type={(DvergrPurchaseType)purchaseType}, Request={requestId}, Success={success}");

            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerID(ZRoutedRpc.instance), PurchaseCompleteRpc, traderZdoId, purchaseType, purchaseNumber, success, transactionId, requestId);
        }

        private static bool ExecutePurchase(ZDOID traderZdoId, DvergrPurchaseType purchaseType, int sapCost)
        {
            Player player = Player.m_localPlayer;

            if (!player || ZNetScene.instance == null)
            {
                return false;
            }

            GameObject traderObject = ZNetScene.instance.FindInstance(traderZdoId);
            Trader trader = traderObject ? traderObject.GetComponent<Trader>() : null;

            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Client purchase failed: Trader unavailable.");
                return false;
            }

            Heightmap.Biome biome = Heightmap.FindBiome(trader.transform.position);

            if (purchaseType == DvergrPurchaseType.Flametal)
            {
                if (biome != Heightmap.Biome.AshLands)
                {
                    return false;
                }

                return ExecuteFlametalPurchase(player);
            }

            if (purchaseType == DvergrPurchaseType.Extractor && biome == Heightmap.Biome.Mistlands)
            {
                return ExecuteExtractorPurchase(player, sapCost);
            }

            return false;
        }

        private static bool ExecuteFlametalPurchase(Player player)
        {
            Inventory inventory = player.GetInventory();
            GameObject flametalPrefab = ObjectDB.instance.GetItemPrefab(DvergrTraderManager.AshlandsFlametalPrefab);
            string paymentName = DvergrTraderManager.GetAshlandsPaymentName();

            if (!flametalPrefab || string.IsNullOrEmpty(paymentName))
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Flametal purchase failed: Required prefab unavailable.");
                return false;
            }

            int quantity = DvergrPricingConfig.AshlandsFlametalQuantity;
            int cost = DvergrPricingConfig.AshlandsMoltenCoreCost;

            if (inventory.CountItems(paymentName) < cost)
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Flametal purchase failed: Insufficient Molten Cores.");
                return false;
            }

            if (!inventory.CanAddItem(flametalPrefab, quantity))
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Flametal purchase failed: Inventory full.");
                return false;
            }

            if (!inventory.AddItem(flametalPrefab, quantity))
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Flametal purchase failed: Unable to add Flametal.");
                return false;
            }

            inventory.RemoveItem(paymentName, cost);

            ItemDrop itemDrop = flametalPrefab.GetComponent<ItemDrop>();

            if (itemDrop)
            {
                player.ShowPickupMessage(itemDrop.m_itemData, quantity);
            }

            Plugin.Log.LogInfo($"[Dvergr Trade] Flametal purchase complete: FlametalAdded={quantity}, MoltenCoreRemoved={cost}");
            return true;
        }

        private static bool ExecuteExtractorPurchase(Player player, int sapCost)
        {
            Inventory inventory = player.GetInventory();
            string trophyName = DvergrTraderManager.GetPaymentTrophy(inventory);
            GameObject sapPrefab = ObjectDB.instance.GetItemPrefab("Sap");
            GameObject needlePrefab = ObjectDB.instance.GetItemPrefab("DvergrNeedle");

            if (trophyName == null || !sapPrefab || !needlePrefab)
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Extractor purchase failed: Required item unavailable.");
                return false;
            }

            ItemDrop sapItem = sapPrefab.GetComponent<ItemDrop>();
            ItemDrop needleItem = needlePrefab.GetComponent<ItemDrop>();

            if (!sapItem || !needleItem)
            {
                return false;
            }

            string sapName = sapItem.m_itemData.m_shared.m_name;

            if (inventory.CountItems(sapName) < sapCost)
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Extractor purchase failed: Insufficient Sap.");
                return false;
            }

            if (!inventory.CanAddItem(needlePrefab, 1))
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Extractor purchase failed: Inventory full.");
                return false;
            }

            if (!inventory.AddItem(needlePrefab, 1))
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Extractor purchase failed: Could not add DvergrNeedle.");
                return false;
            }

            inventory.RemoveItem(trophyName, 1);

            if (sapCost > 0)
            {
                inventory.RemoveItem(sapName, sapCost);
            }

            player.ShowPickupMessage(needleItem.m_itemData, 1);

            Plugin.Log.LogInfo($"[Dvergr Trade] Extractor purchase complete: NeedleAdded=1, TrophyRemoved=1, SapRemoved={sapCost}");
            return true;
        }

        private static void OnBuyComplete(long sender, ZDOID traderZdoId, int purchaseType, int purchaseNumber, bool success, long transactionId, long requestId)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            if (!_hasPendingPurchase || sender != _pendingPeer || traderZdoId != _pendingTrader || purchaseType != _pendingPurchaseType || purchaseNumber != _pendingPurchaseNumber || transactionId != _pendingTransactionId || requestId != _pendingRequestId)
            {
                Plugin.Log.LogWarning("[Dvergr Trade] Completion rejected: No matching pending transaction.");
                return;
            }

            if (Time.time - _pendingStartTime >= PurchaseTimeout)
            {
                FinishPurchase(false);
                ProcessNextPurchase();
                return;
            }

            if (success)
            {
                if (purchaseType == (int)DvergrPurchaseType.Extractor)
                {
                    _purchaseCount++;
                    ZoneSystem.instance.SetGlobalKey($"{PurchaseCountKey} {_purchaseCount}");

                    int nextPurchaseNumber = _purchaseCount + 1;
                    int nextSapCost = _purchaseCount >= DvergrPricingConfig.FreeExtractorPurchases ? DvergrPricingConfig.ExtractorSapCost : 0;

                    ZRoutedRpc.instance.InvokeRoutedRPC(sender, PriceResponseRpc, nextPurchaseNumber, nextSapCost);

                    Plugin.Log.LogInfo($"[Dvergr Trade] Extractor purchase finalized: WorldPurchaseCount={_purchaseCount}");
                }
                else
                {
                    Plugin.Log.LogInfo("[Dvergr Trade] Flametal purchase finalized. Extractor counter unchanged.");
                }
            }
            else
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Purchase failed: Type={(DvergrPurchaseType)purchaseType}, Request={requestId}");
            }

            FinishPurchase(success);
            ProcessNextPurchase();
        }

        private static void FinishPurchase(bool success)
        {
            long peer = _pendingPeer;
            long requestId = _pendingRequestId;

            _hasPendingPurchase = false;
            _pendingPeer = 0;
            _pendingRequestId = 0;
            _pendingTransactionId = 0;
            _pendingPurchaseType = 0;
            _pendingPurchaseNumber = 0;

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
                return;
            }

            if (!DvergrTradeTransaction.TryComplete(requestId))
            {
                Plugin.Log.LogWarning($"[Dvergr Trade] Ignored stale purchase result: Request={requestId}");
                return;
            }

            _requestedPurchaseType = 0;

            Plugin.Log.LogInfo($"[Dvergr Trade] Transaction finalized: Request={requestId}, Success={success}. Client unlocked.");
        }

        private static void InitializePurchaseCount()
        {
            if (_initialized)
            {
                return;
            }

            _purchaseCount = 0;

            if (ZoneSystem.instance.GetGlobalKey(PurchaseCountKey, out string value))
            {
                int.TryParse(value, out _purchaseCount);
            }

            _initialized = true;

            Plugin.Log.LogInfo($"[Dvergr Trade] Loaded world purchase counter: {_purchaseCount}");
        }
    }
}
