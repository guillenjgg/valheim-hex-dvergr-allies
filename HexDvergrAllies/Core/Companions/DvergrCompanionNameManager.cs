using HexDvergrAllies.Core.Networking;
using HexDvergrAllies.Core.UI;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionNameManager
    {
        private const int MaxNameLength = 24;

        internal static string GetCompanionName(Character character)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                return Localization.instance.Localize(character.m_name);
            }

            var name = nview.GetZDO().GetString(DvergrCompanionState.NameKey, "");

            return string.IsNullOrEmpty(name) ? Localization.instance.Localize(character.m_name) : name;
        }

        internal static void RequestRename(Player player, Character character, string name)
        {
            if (!player || !character || !DvergrCompanionManager.IsHiredByPlayer(character, player))
            {
                return;
            }

            name = name.Trim();

            if (name.Length == 0 || name.Length > MaxNameLength || name.Contains("<") || name.Contains(">"))
            {
                player.Message(MessageHud.MessageType.Center, "Invalid companion name.");
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                return;
            }

            nview.InvokeRPC(DvergrRpcManager.RenameRpc, player.GetZDOID(), name);
        }

        internal static void Rename(Character character, long sender, ZDOID playerId, string name)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var player = DvergrCompanionManager.ResolvePlayer(playerId);

            if (!player || !DvergrCompanionManager.IsHiredByPlayer(character, player) || !DvergrInteractionHelper.IsWithinRange(player, character))
            {
                return;
            }

            name = name.Trim();

            if (name.Length == 0 || name.Length > MaxNameLength || name.Contains("<") || name.Contains(">"))
            {
                return;
            }

            nview.GetZDO().Set(DvergrCompanionState.NameKey, name);

            Plugin.Log.LogInfo($"[Dvergr Companion] Renamed: Dvergr={character.name}, Name={name}, Player={player.GetPlayerName()}");
        }

        internal static void RequestRemoteRename(Player player, string name)
        {
            if (!player || !DvergrCompanionManager.TryGetHiredCompanionId(player, out var companionId))
            {
                return;
            }

            name = name.Trim();

            if (name.Length == 0 || name.Length > MaxNameLength || name.Contains("<") || name.Contains(">"))
            {
                player.Message(MessageHud.MessageType.Center, "Invalid companion name.");
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance), DvergrRpcManager.RemoteRenameRpc, player.GetZDOID(), companionId, name);
        }

        internal static void RPC_RemoteRename(long sender, ZDOID playerId, ZDOID companionId, string name)
        {
            if (!ZNet.instance.IsServer())
            {
                return;
            }

            var playerZdo = ZDOMan.instance.GetZDO(playerId);
            var companionZdo = ZDOMan.instance.GetZDO(companionId);

            if (playerZdo == null || companionZdo == null || playerZdo.GetOwner() != sender)
            {
                return;
            }

            var verifiedPlayerId = playerZdo.GetLong(ZDOVars.s_playerID, 0L);

            if (verifiedPlayerId == 0L || !companionZdo.GetBool(DvergrCompanionState.HiredKey, false) || companionZdo.GetLong(DvergrCompanionState.OwnerKey, 0L) != verifiedPlayerId)
            {
                return;
            }

            name = name.Trim();

            if (name.Length == 0 || name.Length > MaxNameLength || name.Contains("<") || name.Contains(">"))
            {
                return;
            }

            companionZdo.Set(DvergrCompanionState.NameKey, name);
        }
    }
}