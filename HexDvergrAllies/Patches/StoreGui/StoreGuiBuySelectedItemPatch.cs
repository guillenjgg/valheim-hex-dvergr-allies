using System.Collections;
using HarmonyLib;
using HexDvergrAllies.Core.Companions;
using HexDvergrAllies.Core.Trading;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch(typeof(StoreGui), "BuySelectedItem")]
    internal static class StoreGuiBuySelectedItemPatch
    {
        private const float HireTimeout = 10f;

        private static bool _hirePending;

        private static bool Prefix(StoreGui __instance, Trader ___m_trader, Trader.TradeItem ___m_selectedItem)
        {
            if (!___m_trader || !DvergrTraderManager.IsDvergrTrader(___m_trader))
            {
                return true;
            }

            if (___m_selectedItem == null)
            {
                return false;
            }

            if (DvergrTraderManager.IsForgePact(___m_selectedItem))
            {
                if (_hirePending)
                {
                    Plugin.Log.LogInfo("[Dvergr Companion] Forge Pact blocked: Hire already pending.");

                    return false;
                }

                Player player = Player.m_localPlayer;
                Character character = ___m_trader.GetComponent<Character>();

                if (!DvergrTraderManager.CanAffordForgePact(player, character))
                {
                    Plugin.Log.LogWarning("[Dvergr Companion] Forge Pact rejected: Player ineligible or insufficient MeadTasty.");

                    return false;
                }

                float distance = Vector3.Distance(player.transform.position, character.transform.position);

                if (distance > DvergrTraderManager.InteractionDistance)
                {
                    Plugin.Log.LogWarning("[Dvergr Companion] Forge Pact rejected: Out of range.");

                    return false;
                }

                ZNetView nview = character.GetComponent<ZNetView>();

                if (!nview || !nview.IsValid())
                {
                    return false;
                }

                ZDOID companionId = nview.GetZDO().m_uid;

                _hirePending = true;

                Plugin.Log.LogInfo($"[Dvergr Companion] Forge Pact requested: " + $"Player={player.GetPlayerName()}, " + $"Dvergr={character.name}, " + $"ZDO={companionId}, " + $"Cost={DvergrPricingConfig.ForgePactCost} MeadTasty");

                __instance.StartCoroutine(CompleteForgePact(player, companionId));

                DvergrCompanionManager.RequestHire(player, character);

                __instance.Hide();

                return false;
            }

            if (___m_selectedItem.m_prefab.gameObject.name == "Softtissue")
            {
                if (!__instance.m_coinPrefab)
                {
                    GameObject coinPrefab = ObjectDB.instance.GetItemPrefab("Coins");

                    if (!coinPrefab)
                    {
                        Plugin.Log.LogError("[Dvergr Trader] Coins prefab not found.");

                        return false;
                    }

                    __instance.m_coinPrefab = coinPrefab.GetComponent<ItemDrop>();
                }

                return true;
            }

            ZNetView traderNview = ___m_trader.GetComponent<ZNetView>();

            if (!traderNview || !traderNview.IsValid())
            {
                return false;
            }

            DvergrPurchaseType purchaseType = DvergrTraderManager.IsAshlandsFlametal(___m_selectedItem) ? DvergrPurchaseType.Flametal : DvergrPurchaseType.Extractor;

            DvergrTradeRpcManager.RequestPurchase(traderNview.GetZDO().m_uid, purchaseType);

            return false;
        }

        private static IEnumerator CompleteForgePact(Player player, ZDOID companionId)
        {
            float startTime = Time.time;

            while (Time.time - startTime < HireTimeout)
            {
                if (!player || ZNetScene.instance == null)
                {
                    break;
                }

                GameObject companionObject = ZNetScene.instance.FindInstance(companionId);

                Character character = companionObject
                    ? companionObject.GetComponent<Character>()
                    : null;

                if (character && DvergrCompanionManager.IsHired(character))
                {
                    if (!DvergrCompanionManager.IsHiredByPlayer(character, player))
                    {
                        Plugin.Log.LogWarning("[Dvergr Companion] Forge Pact failed: Dvergr hired by another player.");

                        break;
                    }

                    string paymentName = DvergrTraderManager.GetForgePactPaymentName();

                    if (string.IsNullOrEmpty(paymentName))
                    {
                        Plugin.Log.LogError("[Dvergr Companion] Forge Pact payment failed: MeadTasty prefab unavailable.");

                        break;
                    }

                    Inventory inventory = player.GetInventory();

                    if (inventory.CountItems(paymentName) < DvergrPricingConfig.ForgePactCost)
                    {
                        Plugin.Log.LogError("[Dvergr Companion] Forge Pact payment failed: MeadTasty no longer available.");

                        break;
                    }

                    inventory.RemoveItem(paymentName, DvergrPricingConfig.ForgePactCost);

                    Plugin.Log.LogInfo($"[Dvergr Companion] Forge Pact completed: " + $"Player={player.GetPlayerName()}, " + $"Dvergr={character.name}, " + $"Cost={DvergrPricingConfig.ForgePactCost} MeadTasty");

                    player.Message(MessageHud.MessageType.Center, "Forge Pact established.");

                    _hirePending = false;

                    yield break;
                }

                yield return null;
            }

            Plugin.Log.LogWarning($"[Dvergr Companion] Forge Pact not completed: " + $"Companion={companionId}, " + $"Timeout={HireTimeout}s");

            if (player)
            {
                player.Message(MessageHud.MessageType.Center, "Unable to complete Forge Pact.");
            }

            _hirePending = false;
        }
    }
}