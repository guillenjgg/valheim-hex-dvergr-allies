using HarmonyLib;
using HexDvergrAllies.Core.Managers;
using HexDvergrAllies.SE_Effects;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Character), "Awake")]
    internal static class CharacterAwakePatch
    {
        private static void Postfix(Character __instance)
        {
            if (__instance.GetFaction() != Character.Faction.Dverger)
            {
                return;
            }

            var monsterAI = __instance.GetComponent<MonsterAI>();

            if (monsterAI != null)
            {
                monsterAI.m_attackPlayerObjects = false;
            }

            if (__instance is Humanoid dvergr)
            {
                DvergrTraderManager.Initialize(dvergr);
            }

            DvergrCompanionManager.Initialize(__instance);

            var statusEffect = ScriptableObject.CreateInstance<SE_DvergrAlly>();
            statusEffect.name = "SE_DvergrAlly";

            __instance.GetSEMan().AddStatusEffect(statusEffect);

            Object.Destroy(statusEffect);

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo($"[Dvergr Init] Name={__instance.name}, " + $"AttackPlayerObjects={monsterAI?.m_attackPlayerObjects}");
            }
        }
    }
}