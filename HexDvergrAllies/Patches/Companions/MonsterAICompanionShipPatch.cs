using HarmonyLib;
using HexDvergrAllies.Core.Managers;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    internal static class MonsterAICompanionShipPatch
    {
        private static bool Prefix(MonsterAI __instance)
        {
            Character character = __instance.GetComponent<Character>();

            if (!DvergrCompanionShipManager.IsPassenger(character))
            {
                return true;
            }

            ZNetView netView = character.GetComponent<ZNetView>();

            if (!netView || !netView.IsValid())
            {
                return true;
            }

            return !netView.IsOwner();
        }
    }
}