using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class MonsterAICompanionShipPatch
    {
        private static void Postfix(MonsterAI __instance)
        {
            Character character = __instance.GetComponent<Character>();

            if (!character)
            {
                return;
            }

            if (!DvergrCompanionShipManager.IsPassenger(character))
            {
                return;
            }

            ZNetView netView = character.GetComponent<ZNetView>();

            if (!netView || !netView.IsValid() || !netView.IsOwner())
            {
                return;
            }

            __instance.StopMoving();
        }
    }
}