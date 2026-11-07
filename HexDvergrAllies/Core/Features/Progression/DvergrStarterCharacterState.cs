using HexDvergrAllies.Core.Companions;
using System.Globalization;
using UnityEngine;

namespace HexDvergrAllies.Core.Features.Progression
{
    internal static class DvergrStarterCharacterState
    {
        internal const string StarterKey = "hex_starter_dvergr";
        internal const string CharacterKey = "hex_starter_character";
        internal const string ConsumedKey = "hex_starter_consumed";

        internal static string WorldPrefix(long characterId)
        {
            return DvergrStarterEligibility.WorldPrefix(characterId);
        }

        internal static string DoneKey(long characterId)
        {
            return WorldPrefix(characterId) + "done";
        }

        internal static bool HasCompleted(Player player)
        {
            if (!player)
            {
                return false;
            }

            return ZoneSystem.instance && ZoneSystem.instance.GetGlobalKey(DoneKey(player.GetPlayerID()));
        }

        internal static bool HasAcceptedStarter(Player player)
        {
            ZDO companion;

            if (!player || !DvergrCompanionManager.TryGetHiredCompanionZDO(player, out companion) || !companion.GetBool(StarterKey, false) || !DvergrCompanionState.IsHired(companion) || DvergrCompanionState.GetOwnerPlayerId(companion) != player.GetPlayerID())
            {
                return false;
            }

            var intendedCharacter = companion.GetLong(CharacterKey, 0L);

            return intendedCharacter == 0L || intendedCharacter == player.GetPlayerID();
        }

        internal static bool IsStarter(Character character)
        {
            var view = character ? character.GetComponent<ZNetView>() : null;

            return view && view.IsValid() && view.GetZDO().GetBool(StarterKey, false);
        }

        internal static bool IsProtected(Character character)
        {
            if (!IsStarter(character))
            {
                return false;
            }

            var zdo = character.GetComponent<ZNetView>().GetZDO();

            return !DvergrCompanionState.IsHired(zdo) && !zdo.GetBool(ConsumedKey, false);
        }

        internal static bool CanHire(Player player, Character character)
        {
            if (!IsStarter(character))
            {
                return true;
            }

            if (!player)
            {
                return false;
            }

            var zdo = character.GetComponent<ZNetView>().GetZDO();
            var intendedCharacter = zdo.GetLong(CharacterKey, 0L);

            return DvergrStarterEligibility.CanHire(player.GetPlayerID(), intendedCharacter, HasCompleted(player), zdo.GetBool(ConsumedKey, false));
        }

        internal static bool HasFreePact(Player player, Character character)
        {
            return IsStarter(character) && CanHire(player, character) && !character.GetComponent<ZNetView>().GetZDO().GetBool(ConsumedKey, false);
        }

        internal static void OnPactAccepted(Player player, Character character)
        {
            if (!IsStarter(character))
            {
                return;
            }

            var zdo = character.GetComponent<ZNetView>().GetZDO();
            zdo.Set(ConsumedKey, true);
            ZRoutedRpc.instance.InvokeRoutedRPC(DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance), DvergrStarterEncounterManager.AcceptedRpc, player.GetZDOID(), zdo.m_uid);
        }
    }
}
