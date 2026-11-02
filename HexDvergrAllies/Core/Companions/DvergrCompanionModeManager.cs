using HexDvergrAllies.Core.Networking;
using UnityEngine;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionModeManager
    {
        internal static DvergrCompanionMode GetMode(Character character)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                return DvergrCompanionMode.Companion;
            }

            return DvergrCompanionState.GetMode(nview.GetZDO());
        }

        internal static bool ShouldSuppressAutomaticTargeting(BaseAI baseAI)
        {
            if (!(baseAI is MonsterAI monsterAI))
            {
                return false;
            }

            var character = monsterAI.GetComponent<Character>();

            if (!character || !DvergrCompanionState.IsHired(character))
            {
                return false;
            }

            if (GetMode(character) == DvergrCompanionMode.Sentry)
            {
                return false;
            }

            var nview = character.GetComponent<ZNetView>();

            return nview && nview.IsValid() && nview.IsOwner();
        }

        internal static void RequestToggleMode(Player player, Character character)
        {
            if (!DvergrCompanionManager.IsHiredByPlayer(character, player))
            {
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                return;
            }

            nview.InvokeRPC(DvergrRpcManager.SetModeRpc, player.GetZDOID());
        }

        internal static void SetMode(Character character, long sender, ZDOID playerId)
        {
            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var player = DvergrCompanionManager.ResolvePlayer(playerId);

            if (!player || !DvergrCompanionManager.IsHiredByPlayer(character, player))
            {
                return;
            }

            var playerZdo = player.GetComponent<ZNetView>().GetZDO();

            if (playerZdo.GetOwner() != sender)
            {
                return;
            }

            var mode = GetMode(character) == DvergrCompanionMode.Companion ? DvergrCompanionMode.Sentry : DvergrCompanionMode.Companion;
            var zdo = nview.GetZDO();

            if (mode == DvergrCompanionMode.Sentry)
            {
                DvergrCompanionSentryMode.Activate(character);
            }
            else
            {
                zdo.Set(DvergrCompanionState.RecallPositionKey, player.transform.position + player.transform.right * 2f);
                zdo.Set(DvergrCompanionState.PendingModeTeleportKey, true);
            }

            zdo.Set(DvergrCompanionState.ModeKey, (int)mode);
            Plugin.Log.LogInfo($"[Dvergr Companion] Mode changed: Dvergr={character.name}, Player={player.GetPlayerName()}, Mode={mode}");
        }

        internal static void RequestRemoteToggleMode(Player player)
        {
            if (!player || !DvergrCompanionManager.TryGetHiredCompanionId(player, out var companionId))
            {
                return;
            }

            ZRoutedRpc.instance.InvokeRoutedRPC(DvergrCompanionManager.GetServerPeerID(ZRoutedRpc.instance), DvergrRpcManager.RemoteSetModeRpc, player.GetZDOID(), companionId);
        }

        internal static void RPC_RemoteSetMode(long sender, ZDOID playerId, ZDOID companionId)
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

            var mode = companionZdo.GetInt(DvergrCompanionState.ModeKey, 0) == (int)DvergrCompanionMode.Companion ? DvergrCompanionMode.Sentry : DvergrCompanionMode.Companion;

            if (mode == DvergrCompanionMode.Sentry)
            {
                companionZdo.Set("HexDvergr_SentryPosition", companionZdo.GetPosition());
            }
            else
            {
                companionZdo.Set(DvergrCompanionState.RecallPositionKey, playerZdo.GetPosition() + Vector3.right * 2f);
                companionZdo.Set(DvergrCompanionState.PendingModeTeleportKey, true);
            }

            companionZdo.Set(DvergrCompanionState.ModeKey, (int)mode);
        }
    }
}