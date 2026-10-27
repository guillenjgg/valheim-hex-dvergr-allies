using HarmonyLib;
using HexDvergrAllies.Core.Managers;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Player), "Interact")]
    internal static class PlayerInteractPatch
    {
        private static bool Prefix(Player __instance, GameObject go, bool hold, bool alt)
        {
            if (hold)
            {
                return true;
            }

            var trader = go.GetComponentInParent<Trader>();

            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
            {
                return true;
            }

            var character = trader.GetComponent<Character>();

            if (!character)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Interaction rejected: Trader={trader.name}, Reason=Character missing");
                return false;
            }

            var distance = Vector3.Distance(
                __instance.transform.position,
                trader.transform.position);

            if (distance > DvergrTraderManager.InteractionDistance)
            {
                Plugin.Log.LogWarning(
                    $"[Dvergr Companion] Interaction rejected: Dvergr={character.name}, Distance={distance:F2}, Reason=Out of range");
                return false;
            }

            var shiftPressed = ZInput.GetKey(KeyCode.LeftShift) ||
                               ZInput.GetKey(KeyCode.RightShift);

            var isHired = DvergrCompanionManager.IsHired(character);

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Player.Interact: Dvergr={character.name}, Hold={hold}, Alt={alt}, Shift={shiftPressed}, Distance={distance:F2}, IsHired={isHired}");

            if (shiftPressed)
            {
                if (isHired)
                {
                    Plugin.Log.LogInfo(
                        $"[Dvergr Companion] Unhire interaction detected: Player={__instance.GetPlayerName()}, Dvergr={character.name}");

                    DvergrCompanionManager.RequestUnhire(__instance, character);
                }
                else
                {
                    Plugin.Log.LogInfo(
                        $"[Dvergr Companion] Hire interaction detected: Player={__instance.GetPlayerName()}, Dvergr={character.name}");

                    DvergrCompanionManager.RequestHire(__instance, character);
                }

                return false;
            }

            if (isHired)
            {
                Plugin.Log.LogInfo(
                    $"[Dvergr Companion] Trade blocked: Dvergr={character.name}, Reason=Already hired");
                return false;
            }

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Opening trader: Dvergr={character.name}");

            DvergrTradeSessionManager.RequestOpen(trader);

            return false;
        }
    }
}