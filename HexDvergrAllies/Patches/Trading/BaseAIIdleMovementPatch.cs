using HarmonyLib;
using HexDvergrAllies.Core.Trading;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(BaseAI), "IdleMovement")]
    internal static class BaseAIIdleMovementPatch
    {
        private static bool Prefix(BaseAI __instance)
        {
            var nview = __instance.GetComponent<ZNetView>();

            if (!nview || !nview.IsOwner())
            {
                return true;
            }

            if (!DvergrTradeSessionManager.IsTrading(nview.GetZDO().m_uid))
            {
                return true;
            }

            __instance.StopMoving();
            return false;
        }
    }
}