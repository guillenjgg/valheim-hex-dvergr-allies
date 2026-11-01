using HarmonyLib;
using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    internal static class ShipCompanionBoardingPatch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
            {
                return;
            }

            DvergrCompanionShipManager.Update();
        }
    }
}