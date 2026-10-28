using HarmonyLib;
using HexDvergrAllies.Core.Managers;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Character), nameof(Character.GetHoverText))]
    internal static class DvergrTraderHoverPatch
    {
        private static bool Prefix(Character __instance, ref string __result)
        {
            var trader = __instance.GetComponent<Trader>();

            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
            {
                return true;
            }

            if (!Player.m_localPlayer ||
                Vector3.Distance(Player.m_localPlayer.transform.position, __instance.transform.position) >
                DvergrTraderManager.InteractionDistance)
            {
                __result = "";
                return false;
            }

            if (DvergrCompanionManager.IsHired(__instance))
            {
                __result = Localization.instance.Localize(
                    trader.m_name + "\nCompanion");

                return false;
            }

            __result = Localization.instance.Localize(
                trader.m_name +
                "\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact");

            return false;
        }
    }
}