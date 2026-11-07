using HexDvergrAllies.Core;
using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.HexLocalization;
using HexDvergrAllies.Core.Trading;
using UnityEngine;

namespace HexDvergrAllies.Core.Features.Progression
{
    internal static class DvergrStarterVendorService
    {
        internal const string QueryRpc = "HexDvergrStarterOwnershipQuery";
        internal const string StatusRpc = "HexDvergrStarterOwnershipStatus";
        private static ZDOID _vendor = ZDOID.None;
        private static int _status;
        private static float _nextQuery;

        internal static void OnShow(Trader trader)
        {
            if (!IsStarter(trader))
            {
                _vendor = ZDOID.None;
                return;
            }

            trader.m_items.RemoveAll(item => !DvergrTraderManager.IsForgePact(item));
            _vendor = ZDOID.None;
            Update(trader);
        }

        internal static bool IsStarter(Trader trader)
        {
            return trader && DvergrStarterCharacterState.IsStarter(trader.GetComponent<Character>());
        }

        internal static string PactTooltip(Character character)
        {
            return DvergrStarterCharacterState.IsStarter(character) ? HexLocalizationManager.Localize("$hex_dvergr_starter_journey") : HexLocalizationManager.Localize("$hex_dvergr_pact_tooltip", DvergrStarterEncounterManager.PactPrice(character));
        }

        internal static void UpdatePactTooltip(Trader trader, GameObject row)
        {
            if (!IsStarter(trader))
            {
                return;
            }

            var tooltip = row.GetComponent<UITooltip>();

            if (tooltip)
            {
                tooltip.m_topic = HexLocalizationManager.Localize("$hex_dvergr_forge_pact_name");
                tooltip.m_text = PactTooltip(trader.GetComponent<Character>());
            }
        }

        internal static bool CanBuy(Trader trader, Trader.TradeItem item)
        {
            return !IsStarter(trader) || DvergrTraderManager.IsForgePact(item);
        }

        internal static bool CanSell(Trader trader)
        {
            return !IsStarter(trader);
        }

        internal static void UpdateSellButton(StoreGui store, Trader trader)
        {
            if (!CanSell(trader))
            {
                store.m_sellButton.interactable = false;
            }
        }

        internal static void Update(Trader trader)
        {
            if (!IsStarter(trader) || !Player.m_localPlayer || ZRoutedRpc.instance == null)
            {
                return;
            }

            var view = trader.GetComponent<ZNetView>();

            if (!view || !view.IsValid())
            {
                return;
            }

            var id = view.GetZDO().m_uid;

            if (_vendor != id)
            {
                _vendor = id;
                _status = 0;
                _nextQuery = 0f;
            }

            if (Time.time >= _nextQuery)
            {
                _nextQuery = Time.time + 5f;
                ZRoutedRpc.instance.InvokeRoutedRPC(DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance), QueryRpc, Player.m_localPlayer.GetZDOID(), id);
            }
        }

        internal static bool IsAvailable(Player player, Character character)
        {
            var view = character ? character.GetComponent<ZNetView>() : null;

            return player && view && view.IsValid() && view.GetZDO().m_uid == _vendor && _status == 1 && DvergrStarterCharacterState.HasFreePact(player, character) && !DvergrCompanionState.IsHired(character);
        }

        internal static string Description(Trader trader)
        {
            var character = trader.GetComponent<Character>();
            var player = Player.m_localPlayer;

            if (!DvergrStarterCharacterState.CanHire(player, character))
            {
                var view = character.GetComponent<ZNetView>();
                var assigned = view.GetZDO().GetLong(DvergrStarterCharacterState.CharacterKey, 0L);

                return HexLocalizationManager.Localize(player && assigned == player.GetPlayerID() ? "$hex_dvergr_starter_complete" : "$hex_dvergr_starter_reserved");
            }

            if (_status == 2 || (_status == 0 && player && DvergrCompanionManager.HasHiredCompanion(player)))
            {
                return HexLocalizationManager.Localize("$hex_dvergr_one_companion_only");
            }

            return HexLocalizationManager.Localize(_status == 1 ? "$hex_dvergr_starter_offer" : "$hex_dvergr_starter_checking");
        }

        internal static void OnQuery(long sender, ZDOID playerId, ZDOID vendorId)
        {
            if (!ZNet.instance || !ZNet.instance.IsServer())
            {
                return;
            }

            var characterId = DvergrPactAuthority.AuthenticatePlayer(sender, playerId);
            var player = ZDOMan.instance.GetZDO(playerId);
            var vendor = ZDOMan.instance.GetZDO(vendorId);

            if (characterId == 0L || vendor == null || !vendor.GetBool(DvergrStarterCharacterState.StarterKey, false) || Vector3.Distance(player.GetPosition(), vendor.GetPosition()) > DvergrTraderManager.InteractionDistance + 2f)
            {
                return;
            }

            var status = 0;

            try
            {
                status = DvergrPactAuthority.HasCompanionOrPendingPact(characterId) ? 2 : 1;
            }
            catch (System.Exception exception)
            {
                HexLog.LogError($"[Starter Encounter] Ownership query failed: {exception}");
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(sender, StatusRpc, vendorId, status);
        }

        internal static void OnStatus(long sender, ZDOID vendorId, int status)
        {
            if (sender == DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance) && vendorId == _vendor)
            {
                _status = status;
            }
        }
    }
}
