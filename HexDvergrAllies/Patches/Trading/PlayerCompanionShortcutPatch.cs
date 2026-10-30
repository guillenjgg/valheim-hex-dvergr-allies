
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

            var renamePressed = HexDvergrAlliesConfiguration.CompanionRenameShortcut.Value.IsDown();
            var interactionPressed = HexDvergrAlliesConfiguration.CompanionInteractionShortcut.Value.IsDown();

            if (!renamePressed && !interactionPressed)
            {
                return;
            }

            if (TextInput.IsVisible())
            {
                return;
            }

            var hoveringObject = __instance.GetHoverObject();

            if (!hoveringObject)
            {
                return;
            }

            var trader = hoveringObject.GetComponentInParent<Trader>();

            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
            {
                return;
            }

            var character = trader.GetComponent<Character>();

            if (!character)
            {
                return;
            }

            var distance = Vector3.Distance(__instance.transform.position, trader.transform.position);

            if (distance > DvergrTraderManager.InteractionDistance)
            {
                return;
            }

            if (renamePressed)
            {
                if (!DvergrCompanionManager.IsHiredByPlayer(character, __instance))
                {
                    return;
                }

                var receiver = character.GetComponent<DvergrCompanionNameReceiver>();

                if (!receiver)
                {
                    receiver = character.gameObject.AddComponent<DvergrCompanionNameReceiver>();
                }

                receiver.Initialize(character);

                TextInput.instance.RequestText(receiver, "Rename Companion", 24);
                return;
            }

            PlayerInteractPatch.HandleCompanionInteraction(__instance, character);
        }
    }
}
