using HexDvergrAllies.Core.Networking;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionUnhireManager
    {
        internal static void RequestRemoteUnhire(Player player)
        {
            if (!player || !DvergrCompanionManager.TryGetHiredCompanionId(player, out var companionId))
            {
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance), DvergrRpcManager.RemoteUnhireRpc, player.GetZDOID(), companionId);
        }

        internal static void RequestUnhire(Player player, Character character)
        {
            if (!player || !character)
            {
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogWarning($"[Dvergr Companion] Unhire request rejected: Invalid ZNetView for Character={character.name}");
                }
                
                return;
            }

            if(HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo($"[Dvergr Companion] Sending unhire RPC: DvergrZDO={nview.GetZDO().m_uid}, PlayerZDO={player.GetZDOID()}");
            }

            nview.InvokeRPC(DvergrRpcManager.UnhireRpc, player.GetZDOID());
        }

        internal static void Unhire(Character character, long sender, ZDOID playerId)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var zdo = nview.GetZDO();

            if (!zdo.GetBool(DvergrCompanionState.HiredKey, false))
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogInfo($"[Dvergr Companion] Unhire request ignored: Dvergr={character.name}, Reason=Not hired");
                }

                return;
            }

            var playerZdo = ZDOMan.instance.GetZDO(playerId);

            if (playerZdo == null || playerZdo.GetOwner() != sender)
            {
                return;
            }

            var player = DvergrCompanionManager.ResolvePlayer(playerId);

            if (!player)
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogInfo($"[Dvergr Companion] Unhire request ignored: Dvergr={character.name}, PlayerZDO={playerId}, Reason=Player missing or out of range");
                }

                return;
            }

            var ownerPlayerId = zdo.GetLong(DvergrCompanionState.OwnerKey, 0L);

            if (ownerPlayerId != player.GetPlayerID())
            {
                if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
                {
                    Plugin.Log.LogInfo($"[Dvergr Companion] Unhire request ignored: Dvergr={character.name}, Player={player.GetPlayerName()}, PlayerID={player.GetPlayerID()}, OwnerPlayerID={ownerPlayerId}, Reason=Not owner");
                }

                return;
            }

            DvergrCompanionManager.CleanupUnhire(character, zdo, ownerPlayerId);
        }
    }
}