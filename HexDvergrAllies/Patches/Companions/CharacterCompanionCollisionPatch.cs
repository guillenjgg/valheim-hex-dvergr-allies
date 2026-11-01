using System.Collections.Generic;
using HarmonyLib;
using HexDvergrAllies.Core.Companions;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Character), "CustomFixedUpdate")]
    internal static class CharacterCompanionCollisionPatch
    {
        private static readonly Dictionary<Character, bool> _collisionStates = new Dictionary<Character, bool>();
        private static CapsuleCollider _playerCollider;

        private static void Postfix(Character __instance)
        {
            var player = Player.m_localPlayer;

            if (!player || __instance == player || __instance.GetFaction() != Character.Faction.Dverger)
            {
                return;
            }

            var playerCollider = player.GetComponent<CapsuleCollider>();

            if (!playerCollider)
            {
                return;
            }

            if (_playerCollider != playerCollider)
            {
                _collisionStates.Clear();
                _playerCollider = playerCollider;
            }

            var isHired = DvergrCompanionManager.IsHired(__instance);

            if (_collisionStates.TryGetValue(__instance, out var previousState) && previousState == isHired)
            {
                return;
            }

            var dvergrCollider = __instance.GetComponent<CapsuleCollider>();

            if (!dvergrCollider)
            {
                return;
            }

            Physics.IgnoreCollision(playerCollider, dvergrCollider, isHired);
            _collisionStates[__instance] = isHired;
        }
    }
}