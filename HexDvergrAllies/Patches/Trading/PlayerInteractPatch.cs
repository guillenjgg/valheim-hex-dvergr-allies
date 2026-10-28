using HarmonyLib;
using HexDvergrAllies.Core.Managers;
using HexDvergrAllies.Core.UI;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.Interact))]
    internal static class PlayerInteractPatch
    {
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

            bool shiftPressed =
                ZInput.GetKey(KeyCode.LeftShift) ||
                ZInput.GetKey(KeyCode.RightShift);

            bool isHired = DvergrCompanionManager.IsHired(character);

            if (alt)
            {
                return false;
            }

            if (shiftPressed)
            {
                if (isHired)
                {
                    if (!DvergrCompanionManager.IsHiredByPlayer(
                        character,
                        __instance))
                    {
                        Plugin.Log.LogWarning(
                            "[Dvergr Companion] Break Pact rejected: Player does not own companion.");

                        return false;
                    }

                    DvergrPactDialogController.ShowBreakPactConfirmation(
                        __instance,
                        character);
                }
                else
                {
                    Plugin.Log.LogInfo(
                        $"[Dvergr Companion] Forge Pact interaction: Player={__instance.GetPlayerName()}, Dvergr={character.name}");

                    DvergrCompanionManager.RequestHire(
                        __instance,
                        character);
                }

                return false;
            }

            if (isHired)
            {
                return false;
            }

            DvergrTradeSessionManager.RequestOpen(trader);

            return false;
        }
    }
}