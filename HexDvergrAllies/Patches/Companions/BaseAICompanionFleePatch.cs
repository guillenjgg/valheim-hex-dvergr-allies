using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(BaseAI), "Flee")]
    internal static class BaseAICompanionFleePatch
    {
        private static bool Prefix(BaseAI __instance)
        {
            if (__instance == null)
            {
                return true;
            }

            MonsterAI monsterAI = __instance as MonsterAI;

            if (monsterAI == null)
            {
                return true;
            }

            Character character = monsterAI.GetComponent<Character>();

            if (character == null || !DvergrCompanionManager.IsHired(character))
            {
                return true;
            }

            ZNetView netView = character.GetComponent<ZNetView>();

            if (netView == null || !netView.IsValid() || !netView.IsOwner())
            {
                return true;
            }

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo(
                    $"[Dvergr Companion] Flee suppressed: " +
                    $"Dvergr={character.name}, " +
                    $"Health={character.GetHealth():F1}, " +
                    $"MaxHealth={character.GetMaxHealth():F1}, " +
                    $"Target={monsterAI.GetTargetCreature()?.name ?? "None"}");
            }

            return false;
        }
    }
}
