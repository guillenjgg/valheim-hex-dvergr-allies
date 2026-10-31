namespace HexDvergrAllies.Core.Managers
{
    internal static class DvergrCompanionModeManager
    {
        internal static bool ShouldSuppressAutomaticTargeting(BaseAI baseAI)
        {
            if (baseAI == null)
            {
                return false;
            }

            MonsterAI monsterAI = baseAI as MonsterAI;

            if (monsterAI == null)
            {
                return false;
            }

            Character character = monsterAI.GetComponent<Character>();

            if (!character || !DvergrCompanionManager.IsHired(character))
            {
                return false;
            }

            ZNetView netView = character.GetComponent<ZNetView>();

            return netView != null && netView.IsValid() && netView.IsOwner();
        }
    }
}
