using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.CustomFixedUpdate))]
    internal static class HumanoidCompanionAttackAnimationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Humanoid __instance)
        {
            DvergrCompanionBehaviorManager.UpdateAttackAnimation(__instance);
        }
    }
}