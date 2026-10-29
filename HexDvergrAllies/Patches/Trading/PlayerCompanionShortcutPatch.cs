using HarmonyLib;
using HexDvergrAllies.Core.Managers;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    internal static class PlayerCompanionShortcutPatch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
            {
                return;
            }

            if (!HexDvergrAlliesConfiguration.CompanionInteractionShortcut.Value.IsDown())
            {
                return;
            }

            GameObject hoveringObject = __instance.GetHoverObject();

            if (!hoveringObject)
            {
                return;
            }

            Trader trader = hoveringObject.GetComponentInParent<Trader>();

            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
            {
                return;
            }

            Character character = trader.GetComponent<Character>();

            if (!character)
            {
                return;
            }

            float distance = Vector3.Distance(
                __instance.transform.position,
                trader.transform.position);

            if (distance > DvergrTraderManager.InteractionDistance)
            {
                return;
            }

            Plugin.Log.LogInfo(
                $"[Dvergr Companion] Configured shortcut triggered: " +
                $"Shortcut={HexDvergrAlliesConfiguration.CompanionInteractionShortcut.Value}, " +
                $"Dvergr={character.name}");

            PlayerInteractPatch.HandleCompanionInteraction(
                __instance,
                character);
        }
    }
}