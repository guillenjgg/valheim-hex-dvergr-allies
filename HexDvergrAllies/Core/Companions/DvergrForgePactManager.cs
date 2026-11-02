using HexDvergrAllies.Core.Trading;
using HexDvergrAllies.Core.UI;
using HexDvergrAllies.Core.Networking;
using System;
using UnityEngine;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrForgePactManager
    {
        private static bool _hirePending;
        private static long _pendingRequestId;
        private static ZDOID _pendingCompanionId;
        private static ZDOID _pendingPlayerId;
        private static Player _pendingPlayer;
        private static Inventory _pendingInventory;
        private static int _pendingCost;

        internal static void RequestForgePact(Player player, Character character)
        {
            if (_hirePending || !DvergrTraderManager.CanAffordForgePact(player, character))
            {
                return;
            }

            if (Vector3.Distance(player.transform.position, character.transform.position) > DvergrTraderManager.InteractionDistance)
            {
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                return;
            }

            var paymentName = DvergrTraderManager.GetForgePactPaymentName();
            var inventory = player.GetInventory();
            var cost = DvergrPricingConfig.ForgePactCost;

            if (string.IsNullOrEmpty(paymentName) || inventory.CountItems(paymentName) < cost)
            {
                return;
            }

            _hirePending = true;
            _pendingRequestId = DateTime.UtcNow.Ticks;
            _pendingCompanionId = nview.GetZDO().m_uid;
            _pendingPlayerId = player.GetZDOID();
            _pendingPlayer = player;
            _pendingInventory = inventory;
            _pendingCost = cost;

            inventory.RemoveItem(paymentName, cost);

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo($"[Dvergr Companion] Forge Pact requested: Player={player.GetPlayerName()}, Dvergr={character.name}, Request={_pendingRequestId}, Cost={cost} MeadTasty");
            }

            RequestHire(player, character, _pendingRequestId);
            StoreGui.instance.Hide();
        }

        internal static void OnHireResult(Character character, long sender, ZDOID playerId, long requestId, bool success)
        {
            if (!_hirePending || requestId != _pendingRequestId || playerId != _pendingPlayerId)
            {
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || nview.GetZDO().m_uid != _pendingCompanionId || sender != nview.GetZDO().GetOwner())
            {
                return;
            }

            var player = _pendingPlayer;
            var inventory = _pendingInventory;
            var cost = _pendingCost;

            if (success)
            {
                if (player)
                {
                    player.Message(MessageHud.MessageType.Center, "Forge Pact established.");
                }

                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogInfo($"[Dvergr Companion] Forge Pact completed: Request={requestId}, Cost={cost} MeadTasty");
                }
            }
            else
            {
                if (player)
                {
                    inventory.AddItem(PrefabHelper.MeadTasty, cost, 1, 0, 0L, "", false, false);
                    player.Message(MessageHud.MessageType.Center, "Unable to complete Forge Pact. Payment refunded.");
                }

                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogWarning($"[Dvergr Companion] Forge Pact rejected: Request={requestId}");
                }
            }

            _hirePending = false;
            _pendingRequestId = 0L;
            _pendingPlayer = null;
            _pendingInventory = null;
        }

        internal static void Hire(Character character, long sender, ZDOID playerId, long requestId)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var success = TryHire(character, playerId);
            nview.InvokeRPC(sender, DvergrRpcManager.HireResultRpc, playerId, requestId, success);
        }

        internal static bool IsHired(Character character)
        {
            if (!character)
            {
                return false;
            }

            var nview = character.GetComponent<ZNetView>();

            return nview && nview.IsValid() && DvergrCompanionState.IsHired(nview.GetZDO());
        }

        internal static void RequestHire(Player player, Character character, long requestId)
        {
            if (!player || !character)
            {
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                Plugin.Log.LogWarning("[Dvergr Companion] Hire request rejected: Reason=Invalid ZNetView");
                return;
            }

            if (DvergrCompanionManager.TryGetHiredCompanion(player.GetPlayerID(), out var existingCompanion))
            {
                Plugin.Log.LogWarning($"[Dvergr Companion] Hire request rejected: Player={player.GetPlayerName()}, ExistingCompanion={existingCompanion}, Reason=Already has companion");

                player.Message(MessageHud.MessageType.Center, "You already have a Dvergr companion.");
                return;
            }

            Plugin.Log.LogInfo($"[Dvergr Companion] Sending hire RPC: DvergrZDO={nview.GetZDO().m_uid}, PlayerZDO={player.GetZDOID()}");

            nview.InvokeRPC(DvergrRpcManager.HireRpc, player.GetZDOID(), requestId);
        }

        private static bool TryHire(Character character, ZDOID playerId)
        {
            var player = DvergrCompanionManager.ResolvePlayer(playerId);

            if (!player || !DvergrInteractionHelper.IsWithinRange(player, character))
            {
                return false;
            }

            var nview = character.GetComponent<ZNetView>();
            var zdo = nview.GetZDO();

            if (DvergrCompanionState.IsHired(zdo))
            {
                return false;
            }

            var ownerPlayerId = player.GetPlayerID();

            if (DvergrCompanionManager.TryGetHiredCompanion(ownerPlayerId, out var existingCompanion))
            {
                return false;
            }

            var monsterAI = character.GetComponent<MonsterAI>();

            if (!monsterAI)
            {
                return false;
            }

            DvergrCompanionBehaviorManager.CaptureState(character, monsterAI);
            zdo.Set(DvergrCompanionState.ModeKey, (int)DvergrCompanionMode.Companion);

            if (!DvergrCompanionState.HasHome(zdo))
            {
                zdo.Set(DvergrCompanionState.HomeKey, character.transform.position);
                zdo.Set(DvergrCompanionState.HasHomeKeyBeenSaved, true);
            }

            zdo.Set(DvergrCompanionState.OwnerKey, ownerPlayerId);
            zdo.Set(DvergrCompanionState.HiredKey, true);

            DvergrCompanionManager.InitializeCompanion(character);
            DvergrCompanionManager.CacheHiredCompanion(ownerPlayerId, zdo.m_uid);

            monsterAI.ResetPatrolPoint();
            monsterAI.SetFollowTarget(player.gameObject);

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo($"[Dvergr Companion] Hired successfully: Dvergr={character.name}, Player={player.GetPlayerName()}, PlayerID={ownerPlayerId}");
            }

            return true;
        }
    }
}
