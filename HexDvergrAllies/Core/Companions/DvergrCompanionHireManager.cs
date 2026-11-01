using System.Collections;
using HexDvergrAllies.Core.Trading;
using UnityEngine;

namespace HexDvergrAllies.Core.Companions
{
    internal static class DvergrCompanionHireManager
    {
        private const float HireTimeout = 10f;
        private static bool _hirePending;

        internal static void RequestForgePact(Player player, Character character)
        {
            if (_hirePending)
            {
                return;
            }

            if (!DvergrTraderManager.CanAffordForgePact(player, character))
            {
                Plugin.Log.LogWarning("[Dvergr Companion] Forge Pact rejected: Player ineligible or insufficient MeadTasty.");
                return;
            }

            if (Vector3.Distance(player.transform.position, character.transform.position) > DvergrTraderManager.InteractionDistance)
            {
                Plugin.Log.LogWarning("[Dvergr Companion] Forge Pact rejected: Out of range.");
                return;
            }

            var nview = character.GetComponent<ZNetView>();

            if (!nview || !nview.IsValid())
            {
                return;
            }

            var paymentName = DvergrTraderManager.GetForgePactPaymentName();
            var inventory = player.GetInventory();
            var cost = DvergrPricingConfig.ForgePactCost;
            var companionId = nview.GetZDO().m_uid;

            if (string.IsNullOrEmpty(paymentName) || inventory.CountItems(paymentName) < cost)
            {
                return;
            }

            _hirePending = true;
            inventory.RemoveItem(paymentName, cost);

            if (HexDvergrAlliesConfiguration.EnableDebugLogs.Value)
            {
                Plugin.Log.LogInfo($"[Dvergr Companion] Forge Pact requested: Player={player.GetPlayerName()}, Dvergr={character.name}, ZDO={companionId}, Cost={cost} MeadTasty");
            }

            player.StartCoroutine(CompleteForgePact(player, companionId, inventory, cost));

            DvergrCompanionManager.RequestHire(player, character);

            StoreGui.instance.Hide();
        }

        private static IEnumerator CompleteForgePact(Player player, ZDOID companionId, Inventory inventory, int cost)
        {
            var startTime = Time.time;
            var completed = false;

            while (Time.time - startTime < HireTimeout)
            {
                if (!player || ZNetScene.instance == null)
                {
                    break;
                }

                var companionObject = ZNetScene.instance.FindInstance(companionId);
                var character = companionObject ? companionObject.GetComponent<Character>() : null;

                if (character && DvergrCompanionManager.IsHired(character))
                {
                    if (!DvergrCompanionManager.IsHiredByPlayer(character, player))
                    {
                        Plugin.Log.LogWarning("[Dvergr Companion] Forge Pact failed: Dvergr hired by another player.");
                        break;
                    }

                    completed = true;

                    Plugin.Log.LogInfo($"[Dvergr Companion] Forge Pact completed: Player={player.GetPlayerName()}, Dvergr={character.name}, Cost={cost} MeadTasty");

                    player.Message(MessageHud.MessageType.Center, "Forge Pact established.");
                    break;
                }

                yield return null;
            }

            if (!completed)
            {
                var paymentPrefab = DvergrTraderManager.GetForgePactPaymentName();

                if (player && !string.IsNullOrEmpty(paymentPrefab))
                {
                    inventory.AddItem("MeadTasty", cost, 1, 0, 0L, "", false, false);
                    player.Message(MessageHud.MessageType.Center, "Unable to complete Forge Pact. Payment refunded.");
                }

                Plugin.Log.LogWarning($"[Dvergr Companion] Forge Pact not completed: Companion={companionId}, Timeout={HireTimeout}s");
            }

            _hirePending = false;
        }
    }
}