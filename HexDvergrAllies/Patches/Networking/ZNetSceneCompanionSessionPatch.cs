using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    internal static class ZNetSceneCompanionSessionPatch
    {
        private static void Prefix()
        {
            DvergrCompanionManager.ResetSessionState();
        }
    }
}