using HexDvergrAllies.Core.Trading;
using UnityEngine;

namespace HexDvergrAllies.Core.UI
{
    internal static class DvergrInteractionHelper
    {
        internal static bool IsWithinRange(Player player, Character character)
        {
            return Vector3.Distance(player.transform.position, character.transform.position) <= DvergrTraderManager.InteractionDistance;
        }
    }
}