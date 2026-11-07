using HexDvergrAllies.Core;
using HarmonyLib;
using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(BaseAI), "Flee")]
    internal static class BaseAICompanionFleePatch
    {

        private static bool Prefix(BaseAI __instance)
        {
            if (!(__instance is MonsterAI monsterAI))
            {
                return true;
            }

            var character = monsterAI.GetComponent<Character>();

            if (!character || !DvergrCompanionState.IsHired(character))
            {
                return true;
            }

            var netView = character.GetComponent<ZNetView>();

            if (!netView || !netView.IsValid() || !netView.IsOwner())
            {
                return true;
            }

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                var target = monsterAI.GetTargetCreature();
                var targetName = target ? target.name : "None";

                HexLog.LogInfo($"[Dvergr Companion] Flee suppressed: Dvergr={character.name}, Health={character.GetHealth():F1}, MaxHealth={character.GetMaxHealth():F1}, Target={targetName}");
            }

            return false;
        }

    }
}
