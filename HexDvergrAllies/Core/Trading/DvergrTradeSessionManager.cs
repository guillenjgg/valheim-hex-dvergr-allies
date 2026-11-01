using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexDvergrAllies.Core.Trading
{
    internal static class DvergrTradeSessionManager
    {
        private const string OpenRequestRpc = "HexDvergrTradeOpen";
        private const string OpenResponseRpc = "HexDvergrTradeOpenResponse";
        private const string CloseRequestRpc = "HexDvergrTradeClose";
        private const float SessionTimeout = 30f;
        private const string HeartbeatRpc = "HexDvergrTradeHeartbeat";
        private const float HeartbeatInterval = 5f;

        private static float _lastHeartbeatTime;

        private static readonly Dictionary<ZDOID, TradeSession> _sessions = new Dictionary<ZDOID, TradeSession>();

        private static readonly Func<ZRoutedRpc, long> GetServerPeerID = AccessTools.MethodDelegate<Func<ZRoutedRpc, long>>(AccessTools.Method(typeof(ZRoutedRpc), "GetServerPeerID"));

        private static ZDOID _currentTrader;
        private static bool _waitingForApproval;
        private static bool _sessionApproved;

        private sealed class TradeSession
        {
            internal long Peer;
            internal float LastActivity;
        }

        internal static void Register()
        {
            ZRoutedRpc.instance.Register<ZDOID>(OpenRequestRpc, OnOpenRequest);
            ZRoutedRpc.instance.Register<ZDOID, bool, string>(OpenResponseRpc, OnOpenResponse);
            ZRoutedRpc.instance.Register<ZDOID>(CloseRequestRpc, OnCloseRequest);
            ZRoutedRpc.instance.Register<ZDOID>(HeartbeatRpc, OnHeartbeat);

            _sessions.Clear();
            _waitingForApproval = false;
            _sessionApproved = false;
            _lastHeartbeatTime = 0f;
        }

        internal static void RequestOpen(Trader trader)
        {
            if (_waitingForApproval || _sessionApproved)
            {
                return;
            }

            _currentTrader = trader.GetComponent<ZNetView>().GetZDO().m_uid;
            _waitingForApproval = true;

            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerID(ZRoutedRpc.instance), OpenRequestRpc, _currentTrader);
        }

        internal static void Close()
        {
            if (!_waitingForApproval && !_sessionApproved)
            {
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerID(ZRoutedRpc.instance), CloseRequestRpc, _currentTrader);

            _waitingForApproval = false;
            _sessionApproved = false;
        }

        internal static bool HasAccess(long peer, ZDOID traderZdoId)
        {
            if (!_sessions.TryGetValue(traderZdoId, out var session))
            {
                return false;
            }

            if (session.Peer != peer)
            {
                return false;
            }

            session.LastActivity = Time.time;
            return true;
        }

        internal static bool IsTrading(ZDOID traderZdoId)
        {
            return _sessions.ContainsKey(traderZdoId);
        }

        internal static void Update()
        {
            if (_sessionApproved && Time.time - _lastHeartbeatTime >= HeartbeatInterval)
            {
                _lastHeartbeatTime = Time.time;

                ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerID(ZRoutedRpc.instance), HeartbeatRpc, _currentTrader);
            }

            if (!ZNet.instance.IsServer())
            {
                return;
            }

            var expiredSessions = new List<ZDOID>();

            foreach (var entry in _sessions)
            {
                var disconnected = entry.Value.Peer != ZNet.GetUID() && ZNet.instance.GetPeer(entry.Value.Peer) == null;

                if (disconnected || Time.time - entry.Value.LastActivity >= SessionTimeout)
                {
                    expiredSessions.Add(entry.Key);
                }
            }

            foreach (var traderZdoId in expiredSessions)
            {
                _sessions.Remove(traderZdoId);
            }
        }

        private static void OnOpenRequest(long sender, ZDOID traderZdoId)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            var approved = false;
            var reason = "Unable to open Dvergr store.";
            var traderZdo = ZDOMan.instance.GetZDO(traderZdoId);
            var peer = ZNet.instance.GetPeer(sender);

            if (traderZdo == null)
            {
                reason = "Trader ZDO not found.";
            }
            else if (sender != ZNet.GetUID() && peer == null)
            {
                reason = "Player peer not found.";
            }
            else
            {
                var traderPrefab = ZNetScene.instance.GetPrefab(traderZdo.GetPrefab());
                var humanoid = traderPrefab ? traderPrefab.GetComponent<Humanoid>() : null;
                var characterId = sender == ZNet.GetUID() ? ZNet.instance.LocalPlayerCharacterID : peer.m_characterID;
                var playerZdo = ZDOMan.instance.GetZDO(characterId);

                if (!humanoid || humanoid.GetFaction() != Character.Faction.Dverger)
                {
                    reason = "Invalid Dvergr trader.";
                }
                else if (playerZdo == null)
                {
                    reason = "Player character ZDO not found.";
                }
                else
                {
                    var distance = Vector3.Distance(playerZdo.GetPosition(), traderZdo.GetPosition());
                    var maximumDistance = DvergrTraderManager.InteractionDistance + 2f;

                    if (distance > maximumDistance)
                    {
                        reason = $"Too far away from Dvergr ({distance:F2}m).";
                    }
                    else if (!_sessions.TryGetValue(traderZdoId, out var session))
                    {
                        _sessions[traderZdoId] = new TradeSession
                        {
                            Peer = sender,
                            LastActivity = Time.time
                        };

                        approved = true;
                        reason = "New session created.";
                    }
                    else if (session.Peer == sender)
                    {
                        session.LastActivity = Time.time;
                        approved = true;
                        reason = "Existing session reused.";
                    }
                    else
                    {
                        reason = "This Dvergr is currently trading with another player.";
                    }
                }
            }

            Plugin.Log.LogInfo($"[Dvergr Trade Session] Open: Peer={sender}, Trader={traderZdoId}, Approved={approved}, Reason={reason}");

            ZRoutedRpc.instance.InvokeRoutedRPC(sender, OpenResponseRpc, traderZdoId, approved, reason);
        }

        private static void OnOpenResponse(long sender, ZDOID traderZdoId, bool approved, string reason)
        {
            if (sender != GetServerPeerID(ZRoutedRpc.instance))
            {
                return;
            }

            if (!_waitingForApproval || traderZdoId != _currentTrader)
            {
                return;
            }

            _waitingForApproval = false;

            if (!approved)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, reason);
                return;
            }

            var traderObject = ZNetScene.instance.FindInstance(traderZdoId);
            var trader = traderObject ? traderObject.GetComponent<Trader>() : null;

            if (!trader || Vector3.Distance(Player.m_localPlayer.transform.position, trader.transform.position) > DvergrTraderManager.InteractionDistance || trader.m_items.Count == 0)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerID(ZRoutedRpc.instance), CloseRequestRpc, traderZdoId);

                return;
            }

            _sessionApproved = true;
            _lastHeartbeatTime = Time.time;

            DvergrTradeRpcManager.RequestPrice(traderZdoId);
            StoreGui.instance.Show(trader);
        }

        private static void OnCloseRequest(long sender, ZDOID traderZdoId)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            if (!_sessions.TryGetValue(traderZdoId, out var session))
            {
                Plugin.Log.LogInfo($"[Dvergr Trade Session] Close: Peer={sender}, Trader={traderZdoId}, Reason=No active session");
                return;
            }

            if (session.Peer != sender)
            {
                Plugin.Log.LogWarning($"[Dvergr Trade Session] Close rejected: Peer={sender}, Trader={traderZdoId}, Owner={session.Peer}");
                return;
            }

            _sessions.Remove(traderZdoId);

            Plugin.Log.LogInfo($"[Dvergr Trade Session] Close: Peer={sender}, Trader={traderZdoId}, Session removed");
        }

        private static void OnHeartbeat(long sender, ZDOID traderZdoId)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            if (!_sessions.TryGetValue(traderZdoId, out var session) || session.Peer != sender)
            {
                return;
            }

            var traderZdo = ZDOMan.instance.GetZDO(traderZdoId);
            var peer = ZNet.instance.GetPeer(sender);

            if (traderZdo == null || (sender != ZNet.GetUID() && peer == null))
            {
                return;
            }

            session.LastActivity = Time.time;
        }
    }
}