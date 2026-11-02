namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionModeManager
    {
        internal static bool ShouldSuppressAutomaticTargeting(BaseAI baseAI)
        {
            if (!(baseAI is MonsterAI monsterAI))
            {
                return false;
            }

            var character = monsterAI.GetComponent<Character>();

            if (!character || !DvergrCompanionManager.IsHired(character))
            {
                return false;
            }

            if (DvergrCompanionManager.GetMode(character) == DvergrCompanionMode.Sentry)
            {
                return false;
            }

            var nview = character.GetComponent<ZNetView>();

            return nview && nview.IsValid() && nview.IsOwner();
        }
    }
}