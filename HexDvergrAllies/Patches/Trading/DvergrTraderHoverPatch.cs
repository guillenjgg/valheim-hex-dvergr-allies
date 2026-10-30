
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

            var player = Player.m_localPlayer;

            if (!player || Vector3.Distance(player.transform.position, __instance.transform.position) > DvergrTraderManager.InteractionDistance)
            {
                __result = "";
                return false;
            }

            if (DvergrCompanionManager.IsHired(__instance))
            {
                var companionName = DvergrCompanionManager.GetCompanionName(__instance);

                __result = companionName + "\nCompanion";

                if (DvergrCompanionManager.IsHiredByPlayer(__instance, player))
                {
                    var interactionShortcut = HexDvergrAlliesConfiguration.CompanionInteractionShortcut.Value.ToString();
                    var renameShortcut = HexDvergrAlliesConfiguration.CompanionRenameShortcut.Value.ToString();

                    __result += "\n[<color=yellow><b>" + interactionShortcut + "</b></color>] Break Pact";
                    __result += "\n[<color=yellow><b>" + renameShortcut + "</b></color>] Rename";
                }

                return false;
            }

            __result = Localization.instance.Localize(trader.m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact");

            return false;
        }
    }
}
