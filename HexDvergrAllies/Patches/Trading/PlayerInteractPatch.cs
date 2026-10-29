
using HarmonyLib;
using HexDvergrAllies.Core.Managers;
using HexDvergrAllies.Core.UI;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.Interact))]
    internal static class PlayerInteractPatch
    {
        internal static void HandleCompanionInteraction(
            Player player,
            Character character)
        {
            if (!player || !character)
            {
                return;
            }

            if (DvergrCompanionManager.IsHired(character))
            {
                if (!DvergrCompanionManager.IsHiredByPlayer(
                    character,
                    player))
                {
                    Plugin.Log.LogWarning(
                        "[Dvergr Companion] Break Pact rejected: Player does not own companion.");

                    return;
                }

                DvergrPactDialogController.ShowBreakPactConfirmation(
                    player,
                    character);

                return;
            }

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Forge Pact interaction: Player={player.GetPlayerName()}, Dvergr={character.name}");

            DvergrCompanionManager.RequestHire(
                player,
                character);
        }

        private static bool Prefix(
            Player __instance,
            GameObject go,
            bool hold,
            bool alt)
        {
            if (hold || !go)
            {
                return true;
            }

            Trader trader = go.GetComponentInParent<Trader>();

            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
            {
                return true;
            }

            Character character = trader.GetComponent<Character>();

            if (!character)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Interaction rejected: Trader={trader.name}, Reason=Character missing");

                return false;
            }

            float distance = Vector3.Distance(
                __instance.transform.position,
                trader.transform.position);

            if (distance > DvergrTraderManager.InteractionDistance)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Interaction rejected: Dvergr={character.name}, Distance={distance:F2}, Reason=Out of range");

                return false;
            }

            // The configured companion shortcut is handled by Player.Update.
            // Do not allow the same keypress to open the trader.
            if (HexDvergrAlliesConfiguration
                .CompanionInteractionShortcut.Value.IsPressed())
            {
                return false;
            }

            if (alt)
            {
                return false;
            }

            if (DvergrCompanionManager.IsHired(character))
            {
                return false;
            }

            DvergrTradeSessionManager.RequestOpen(trader);

            return false;
        }
    }
}
