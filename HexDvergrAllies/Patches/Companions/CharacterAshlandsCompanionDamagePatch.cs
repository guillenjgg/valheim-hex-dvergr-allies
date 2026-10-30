using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    internal static class CharacterAshlandsCompanionDamagePatch
    {
        private static bool Prefix(Character __instance, HitData hit)
        {
            if (!DvergrCompanionManager.IsHired(__instance) || !__instance.name.StartsWith("DvergerAshlands"))
            {
                return true;
            }

            if (hit.m_hitType == HitData.HitType.AshlandsLava)
            {
                return false;
            }

            hit.m_damage.m_fire = 0f;
            return true;
        }
    }
}
