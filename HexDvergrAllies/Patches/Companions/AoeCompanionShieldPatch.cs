using HarmonyLib;
using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Aoe), nameof(Aoe.Initiate))]
    internal static class AoeCompanionShieldPatch
    {
        private static readonly int ShieldHash = "Staff_shield".GetStableHashCode();

        private static readonly AccessTools.FieldRef<Aoe, Character> OwnerField = AccessTools.FieldRefAccess<Aoe, Character>("m_owner");
        private static readonly AccessTools.FieldRef<Aoe, int> LevelField = AccessTools.FieldRefAccess<Aoe, int>("m_level");

        private static void Postfix(Aoe __instance)
        {
            if (__instance.gameObject.name.Replace("(Clone)", "").Trim() != "staff_shield_aoe")
            {
                return;
            }

            var player = OwnerField(__instance) as Player;

            if (!player || player != Player.m_localPlayer)
            {
                return;
            }

            if (!DvergrCompanionManager.TryGetHiredCompanionId(player, out var companionId))
            {
                return;
            }

            var companionObject = ZNetScene.instance.FindInstance(companionId);

            if (!companionObject)
            {
                return;
            }

            var companion = companionObject.GetComponent<Character>();

            if (!DvergrCompanionManager.IsHiredByPlayer(companion, player) || companion.IsDead())
            {
                return;
            }

            var itemLevel = LevelField(__instance);
            var skillLevel = player.GetSkillLevel(__instance.m_skill);

            companion.GetSEMan().AddStatusEffect(ShieldHash, true, itemLevel, skillLevel);
        }
    }
}