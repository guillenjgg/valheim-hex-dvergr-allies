using HexDvergrAllies.Core;
using HexDvergrAllies.Core.HexLocalization;
using HexDvergrAllies.Core.Trading;
using HexDvergrAllies.Core.UI;
using HexDvergrAllies.Core.Networking;
using System;
using UnityEngine;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrForgePactManager
    {
        internal static bool IsHirePending => _hirePending;

        private static bool _hirePending;
        private static long _pendingRequestId;
        private static ZDOID _pendingCompanionId;
        private static ZDOID _pendingPlayerId;
        private static Player _pendingPlayer;
        private static Inventory _pendingInventory;
        private static int _pendingCost;

        internal static void ResetSessionState()
        {
            // Old-world replies cannot settle a new-world request. Payment is
            // reconciled by the authority while connected, never guessed here.
            _hirePending = false;
            _pendingRequestId = 0L;
            _pendingCompanionId = ZDOID.None;
            _pendingPlayerId = ZDOID.None;
            _pendingPlayer = null;
            _pendingInventory = null;
            _pendingCost = 0;
        }

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
            var cost = Core.Features.Progression.DvergrStarterEncounterManager.PactCost(character);

            if (cost > 0 && (string.IsNullOrEmpty(paymentName) || inventory.CountItems(paymentName) < cost))
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

            if (cost > 0)
            {
                inventory.RemoveItem(paymentName, cost);
            }

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                HexLog.LogInfo($"[Dvergr Companion] Forge Pact requested: Player={player.GetPlayerName()}, Dvergr={character.name}, Request={_pendingRequestId}, Cost={cost} MeadTasty");
            }

            RequestHire(player, character, _pendingRequestId);
            StoreGui.instance.Hide();
        }

        internal static void OnAuthoritativeResult(long sender, ZDOID playerId, ZDOID companionId, long requestId, bool success)
        {
            if (!_hirePending || requestId != _pendingRequestId || playerId != _pendingPlayerId)
            {
                return;
            }

            if (sender != DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance) || companionId != _pendingCompanionId)
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
                    player.Message(MessageHud.MessageType.Center, HexLocalizationManager.Localize("$hex_dvergr_pact_established"));
                }

                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    HexLog.LogInfo($"[Dvergr Companion] Forge Pact completed: Request={requestId}, Cost={cost} MeadTasty");
                }
            }
            else
            {
                if (player)
                {
                    if (cost > 0)
                    {
                        inventory.AddItem(PrefabHelper.MeadTasty, cost, 1, 0, 0L, "", false, false);
                    }

                    player.Message(MessageHud.MessageType.Center, HexLocalizationManager.Localize(cost > 0 ? "$hex_dvergr_pact_refunded" : "$hex_dvergr_starter_rejected"));
                }

                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    HexLog.LogWarning($"[Dvergr Companion] Forge Pact rejected: Request={requestId}");
                }
            }

            _hirePending = false;
            _pendingRequestId = 0L;
            _pendingPlayer = null;
            _pendingInventory = null;
        }

        internal static bool CommitApproved(Character character, ZDOID playerId)
        {
            var view = character.GetComponent<ZNetView>();

            return view && view.IsValid() && view.IsOwner() && TryHire(character, playerId);
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
                HexLog.LogWarning("[Dvergr Companion] Hire request rejected: Reason=Invalid ZNetView");
                return;
            }

            HexLog.LogInfo($"[Dvergr Companion] Sending hire RPC: DvergrZDO={nview.GetZDO().m_uid}, PlayerZDO={player.GetZDOID()}");

            DvergrPactAuthority.Request(player, character, requestId);
        }

        private static bool TryHire(Character character, ZDOID playerId)
        {
            var player = DvergrCompanionManager.ResolvePlayer(playerId);

            if (!player || !DvergrInteractionHelper.IsWithinRange(player, character) || !Features.Progression.DvergrStarterCharacterState.CanHire(player, character))
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

            // The server validated persistent ownership and simultaneous requests.
            // An owning client's unloaded-hire cache must not veto that decision.
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
            Features.Progression.DvergrStarterCharacterState.OnPactAccepted(player, character);

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                HexLog.LogInfo($"[Dvergr Companion] Hired successfully: Dvergr={character.name}, Player={player.GetPlayerName()}, PlayerID={ownerPlayerId}");
            }

            return true;
        }
    }
}
