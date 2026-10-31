using HarmonyLib;
using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.Trading;
using HexDvergrAllies.Core.UI;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.Interact))]
    internal static class PlayerInteractPatch
    {
        internal static void HandleCompanionInteraction(Player player, Character character)
        {
            if (!player || !character)
            {
                return;
            }

            if (!DvergrCompanionManager.IsHiredByPlayer(character, player))
            {
                return;
            }

            DvergrPactDialogController.ShowBreakPactConfirmation(player, character);
        }

        private static bool Prefix(Player __instance, GameObject go, bool hold, bool alt)
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
                return false;
            }

            float distance = Vector3.Distance(__instance.transform.position, trader.transform.position);

            if (distance > DvergrTraderManager.InteractionDistance)
            {
                return false;
            }

            if (HexDvergrAlliesConfiguration .CompanionInteractionShortcut.Value.IsPressed())
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