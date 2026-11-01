using HarmonyLib;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    /// <summary>
    /// Prevents dvergrs hitting the player
    /// </summary>
    [HarmonyPatch(typeof(Attack), nameof(Attack.AddHitPoint))]
    internal static class AttackAddHitPointPatch
    {
        private static bool Prefix(Humanoid ___m_character, GameObject go)
        {
            if (!HexDvergrAlliesConfiguration.EnablePlayerDamageImmunity.Value)
            {
                return true;
            }

            if (!___m_character.IsPlayer())
            {
                return true;
            }

            var character = go.GetComponent<Character>();
            return character == null || character.GetFaction() != Character.Faction.Dverger;
        }
    }
}