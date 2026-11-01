using HarmonyLib;
using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
    internal static class PlayerCompanionPinPatch
    {
        private static void Postfix(Player __instance)
        {
            if (!__instance.TryGetComponent<DvergrCompanionPinManager>(out var pinManager))
            {
                __instance.gameObject.AddComponent<DvergrCompanionPinManager>();
            }
        }
    }
}