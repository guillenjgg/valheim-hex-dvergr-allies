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

            if (Vector3.Distance(__instance.transform.position, trader.transform.position) > DvergrTraderManager.InteractionDistance)
            {
                return false;
            }

            var traderZdoId = trader.GetComponent<ZNetView>().GetZDO().m_uid;

            DvergrTradeRpcManager.RequestPrice(traderZdoId);
            StoreGui.instance.Show(trader);

            return false;
        }
    }
}
