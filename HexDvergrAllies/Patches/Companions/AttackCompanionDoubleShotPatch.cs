using HarmonyLib;
using HexDvergrAllies.Core;
using HexDvergrAllies.Core.Companions;

namespace HexDvergrAllies.Patches.Companions
{
    [HarmonyPatch(typeof(Attack), "FireProjectileBurst")]
    internal static class AttackCompanionDoubleShotPatch
    {
        private static readonly AccessTools.FieldRef<Attack, Humanoid> CharacterField = AccessTools.FieldRefAccess<Attack, Humanoid>("m_character");
        private static readonly AccessTools.FieldRef<Attack, ItemDrop.ItemData> WeaponField = AccessTools.FieldRefAccess<Attack, ItemDrop.ItemData>("m_weapon");

        private static void Prefix(Attack __instance, out int __state)
        {
            __state = __instance.m_projectiles;

            var character = CharacterField(__instance);
            var weapon = WeaponField(__instance);

            if (!character || !DvergrCompanionState.IsHired(character) ||
                character.GetFaction() != Character.Faction.Dverger ||
                weapon == null || weapon.m_dropPrefab.name != PrefabHelper.CrossbowArbalest)
            {
                return;
            }

            __instance.m_projectiles = 2;
        }

        private static void Postfix(Attack __instance, int __state)
        {
            __instance.m_projectiles = __state;
        }
    }
}