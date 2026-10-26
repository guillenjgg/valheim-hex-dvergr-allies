
using HarmonyLib;
using HexDvergrAllies.Core.Managers;
using TMPro;
using UnityEngine;

namespace HexDvergrAllies.Patches
{
    [HarmonyPatch]
    internal static class StoreGuiDvergrDisplayPatch
    {
        private static TMP_Text _paymentText;
        private static int _lastSapCost = -1;
        private static bool _lastHasPrice;

        [HarmonyPatch(typeof(StoreGui), "FillList")]
        [HarmonyPostfix]
        private static void FillListPostfix(StoreGui __instance, Trader ___m_trader)
        {
            if (!___m_trader || !DvergrTraderManager.IsDvergrTrader(___m_trader))
                return;

            foreach (Transform item in __instance.m_listRoot)
            {
                var coinBackground = item.Find("coin_bkg");

                if (coinBackground)
                    coinBackground.gameObject.SetActive(false);
            }

            CreatePaymentText(__instance);
            UpdatePaymentText();
        }

        [HarmonyPatch(typeof(StoreGui), "Update")]
        [HarmonyPostfix]
        private static void UpdatePostfix(StoreGui __instance, Trader ___m_trader)
        {
            if (!__instance.m_rootPanel.activeSelf)
                return;

            var isDvergrTrader = ___m_trader && DvergrTraderManager.IsDvergrTrader(___m_trader);

            __instance.m_coinText.transform.parent.gameObject.SetActive(!isDvergrTrader);

            if (_paymentText)
                _paymentText.gameObject.SetActive(isDvergrTrader);

            if (!isDvergrTrader)
                return;

            if (!_paymentText)
                CreatePaymentText(__instance);

            if (_lastSapCost != DvergrTradeRpcManager.CurrentSapCost ||
                _lastHasPrice != DvergrTradeRpcManager.HasCurrentPrice)
            {
                UpdatePaymentText();
            }
        }

        private static void CreatePaymentText(StoreGui storeGui)
        {
            if (_paymentText)
                return;

            var buyButton = storeGui.m_buyButton.GetComponent<RectTransform>();
            var sourceText = storeGui.m_buyButton.GetComponentInChildren<TMP_Text>();
            var paymentObject = Object.Instantiate(sourceText.gameObject, buyButton.parent);

            paymentObject.name = "HexDvergrPaymentText";

            _paymentText = paymentObject.GetComponent<TMP_Text>();
            _paymentText.alignment = TextAlignmentOptions.Center;
            _paymentText.textWrappingMode = TextWrappingModes.Normal;
            _paymentText.fontSize = 15f;
            _paymentText.raycastTarget = false;

            var rect = paymentObject.GetComponent<RectTransform>();
            rect.anchorMin = buyButton.anchorMin;
            rect.anchorMax = buyButton.anchorMax;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(230f, 75f);
            rect.anchoredPosition = buyButton.anchoredPosition + new Vector2(0f, 50f);

            _lastSapCost = -1;
            _lastHasPrice = false;
        }

        private static void UpdatePaymentText()
        {
            if (!_paymentText)
                return;

            _lastSapCost = DvergrTradeRpcManager.CurrentSapCost;
            _lastHasPrice = DvergrTradeRpcManager.HasCurrentPrice;

            if (!_lastHasPrice)
            {
                _paymentText.text = "Loading payment...";
                return;
            }

            var payment = "<color=#FFA500>Payment Required</color>\n" +
                          "1 Seeker, Gjall, or\nSeeker Brute Trophy";

            if (_lastSapCost > 0)
                payment += $"\n+ {_lastSapCost} Sap";

            _paymentText.text = payment;
        }
    }
}
