using HarmonyLib;
using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Character), nameof(Character.GetHoverName))]
    internal static class DvergrCompanionHoverNamePatch
    {
        private static void Postfix(Character __instance, ref string __result)
        {
            if (!DvergrCompanionManager.IsHired(__instance))
            {
                return;
            }

            __result = DvergrCompanionManager.GetCompanionName(__instance);
        }
    }
}
