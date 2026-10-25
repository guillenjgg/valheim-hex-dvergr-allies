using HarmonyLib;
using HexDvergrAllies.Core.Managers;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Player), "Interact")]
    internal static class PlayerInteractPatch
    {
        private static bool Prefix(GameObject go, bool hold, bool alt)
        {
            if (hold)
                return true;

            var trader = go.GetComponentInParent<Trader>();

            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
                return true;

            StoreGui.instance.Show(trader);
            return false;
        }
    }
}