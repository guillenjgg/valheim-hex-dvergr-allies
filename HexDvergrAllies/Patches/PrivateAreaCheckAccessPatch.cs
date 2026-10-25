using HarmonyLib;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(PrivateArea), nameof(PrivateArea.CheckAccess))]
    internal static class PrivateAreaCheckAccessPatch
    {
        private static void Postfix(Vector3 point, bool flash, ref bool __result)
        {
            if (__result || !HexDvergrAlliesConfiguration.AllowTemporaryHomestead.Value)
            {
                return;
            }

            var player = Player.m_localPlayer;

            if (!player || !player.InPlaceMode() || !flash)
            {
                return;
            }

            if (PrivateArea.InsideFactionArea(point, Character.Faction.Dverger))
            {
                __result = true;
            }
        }
    }
}