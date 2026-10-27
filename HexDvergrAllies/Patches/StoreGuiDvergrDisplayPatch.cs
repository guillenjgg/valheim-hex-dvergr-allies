
using System.Collections.Generic;
using HarmonyLib;
using HexDvergrAllies.Core.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch]
    internal static class StoreGuiDvergrDisplayPatch
    {
        private static readonly AccessTools.FieldRef<StoreGui, Trader> TraderField = AccessTools.FieldRefAccess<StoreGui, Trader>("m_trader");
        private static readonly AccessTools.FieldRef<StoreGui, List<GameObject>> ItemListField = AccessTools.FieldRefAccess<StoreGui, List<GameObject>>("m_itemList");

        private static GameObject _paymentPanel;
        private static TMP_Text _paymentText;
        private static int _lastSapCost = -1;
        private static bool _lastHasPrice;

        [HarmonyPatch(typeof(StoreGui), "FillList")]
        [HarmonyPostfix]
        private static void FillListPostfix(StoreGui __instance)
        {
            var trader = TraderField(__instance);

            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
            {
                return;
            }

            CreatePaymentPanel(__instance);
            UpdateItemPrices(trader, ItemListField(__instance));
        }

        [HarmonyPatch(typeof(StoreGui), "Update")]
        [HarmonyPostfix]
        private static void UpdatePostfix(StoreGui __instance)
        {
            if (!__instance.m_rootPanel.activeSelf)
            {
                return;
            }

            var trader = TraderField(__instance);
            var isDvergrTrader = trader && DvergrTraderManager.IsDvergrTrader(trader);

            __instance.m_coinText.transform.parent.gameObject.SetActive(!isDvergrTrader);

            if (_paymentPanel)
            {
                _paymentPanel.SetActive(isDvergrTrader);
            }

            if (!isDvergrTrader)
            {
                return;
            }

            if (!_paymentPanel)
            {
                CreatePaymentPanel(__instance);
            }

            if (_lastSapCost != DvergrTradeRpcManager.CurrentSapCost ||
                _lastHasPrice != DvergrTradeRpcManager.HasCurrentPrice)
            {
                UpdateItemPrices(trader, ItemListField(__instance));
            }
        }

        private static void CreatePaymentPanel(StoreGui storeGui)
        {
            if (_paymentPanel)
            {
                return;
            }

            var buyButton = storeGui.m_buyButton.GetComponent<RectTransform>();
            var sourceText = storeGui.m_buyButton.GetComponentInChildren<TMP_Text>();

            _paymentPanel = new GameObject("HexDvergrPaymentPanel", typeof(RectTransform), typeof(Image));
            _paymentPanel.transform.SetParent(buyButton.parent, false);

            var panelRect = _paymentPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(220f, 90f);
            panelRect.position = buyButton.position;
            panelRect.anchoredPosition += new Vector2(0f, 48f);

            var background = _paymentPanel.GetComponent<Image>();
            background.color = new Color(0.12f, 0.10f, 0.08f, 0.92f);
            background.raycastTarget = false;

            var outline = _paymentPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.42f, 0.25f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;

            var paymentObject = Object.Instantiate(sourceText.gameObject, _paymentPanel.transform);
            paymentObject.name = "PaymentText";

            _paymentText = paymentObject.GetComponent<TMP_Text>();
            _paymentText.alignment = TextAlignmentOptions.Center;
            _paymentText.textWrappingMode = TextWrappingModes.Normal;
            _paymentText.fontSize = 15f;
            _paymentText.raycastTarget = false;

            var textRect = paymentObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, -8f);

            _paymentText.text =
                "<color=#D9A75D><b>ACCEPTED CURRENCY</b></color>\n" +
                "<size=85%>1 Trophy = Seeker, Seeker Soldier, or Gjall</size>";

            _lastSapCost = -1;
            _lastHasPrice = false;
        }

        private static void UpdateItemPrices(Trader trader, List<GameObject> itemList)
        {
            _lastSapCost = DvergrTradeRpcManager.CurrentSapCost;
            _lastHasPrice = DvergrTradeRpcManager.HasCurrentPrice;

            var tradeItems = trader.GetAvailableItems();

            for (var i = 0; i < tradeItems.Count && i < itemList.Count; i++)
            {
                var item = itemList[i].transform;
                var tradeItem = tradeItems[i];
                var isSoftTissue = tradeItem.m_prefab.gameObject.name == "Softtissue";

                var coinBackground = item.Find("coin_bkg");
                var priceTransform = Utils.FindChild(item, "price", Utils.IterativeSearchType.DepthFirst);
                var priceText = priceTransform.GetComponent<TMP_Text>();

                coinBackground.gameObject.SetActive(true);
                coinBackground.GetComponent<Image>().enabled = isSoftTissue;
                coinBackground.Find("coin icon").gameObject.SetActive(isSoftTissue);

                priceText.text = isSoftTissue
                    ? tradeItem.m_price.ToString()
                    : !_lastHasPrice
                        ? "Loading..."
                        : _lastSapCost > 0
                            ? $"1 Trophy + {_lastSapCost} Sap"
                            : "1 Trophy";

                priceText.color = Color.white;
                priceText.enableAutoSizing = false;
                priceText.fontSizeMin = 15f;

                var priceRect = priceText.GetComponent<RectTransform>();
                priceRect.anchorMin = Vector2.zero;
                priceRect.anchorMax = Vector2.one;
                priceRect.offsetMin = Vector2.zero;
                priceRect.offsetMax = Vector2.zero;
            }
        }
    }
}
