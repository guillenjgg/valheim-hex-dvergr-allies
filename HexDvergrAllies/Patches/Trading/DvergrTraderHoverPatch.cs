
using HarmonyLib;
using HexDvergrAllies.Core.Managers;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Character), nameof(Character.GetHoverText))]
    internal static class DvergrTraderHoverPatch
    {
        private static bool Prefix(
            Character __instance,
            ref string __result)
        {
            Trader trader = __instance.GetComponent<Trader>();

            if (!trader ||
                !DvergrTraderManager.IsDvergrTrader(trader))
            {
                return true;
            }

            Player player = Player.m_localPlayer;

            if (!player ||
                Vector3.Distance(
                    player.transform.position,
                    __instance.transform.position) >
                DvergrTraderManager.InteractionDistance)
            {
                __result = "";
                return false;
            }

            string shortcutText =
                HexDvergrAlliesConfiguration
                    .CompanionInteractionShortcut.Value.ToString();

            string companionPrompt =
                "[<color=yellow><b>" +
                shortcutText +
                "</b></color>] ";

            if (DvergrCompanionManager.IsHired(__instance))
            {
                __result = Localization.instance.Localize(
                    trader.m_name + "\nCompanion");

                if (DvergrCompanionManager.IsHiredByPlayer(
                    __instance,
                    player))
                {
                    __result +=
                        "\n" + companionPrompt + "Break Pact";
                }

                return false;
            }

            __result = Localization.instance.Localize(
                trader.m_name +
                "\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact");

            __result +=
                "\n" + companionPrompt + "Forge Pact";

            return false;
        }
    }
}
