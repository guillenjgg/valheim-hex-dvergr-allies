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
        private static GameObject _paymentPanel;
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

            CreatePaymentPanel(__instance);
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

            if (_paymentPanel)
                _paymentPanel.SetActive(isDvergrTrader);

            if (!isDvergrTrader)
                return;

            if (!_paymentPanel)
                CreatePaymentPanel(__instance);

            if (_lastSapCost != DvergrTradeRpcManager.CurrentSapCost ||
                _lastHasPrice != DvergrTradeRpcManager.HasCurrentPrice)
            {
                UpdatePaymentText();
            }
        }

        private static void CreatePaymentPanel(StoreGui storeGui)
        {
            if (_paymentPanel)
                return;

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
                _paymentText.text = "<color=#D9A75D>Loading payment...</color>";
                return;
            }

            var payment = "<color=#D9A75D><b>PAYMENT REQUIRED</b></color>\n" +
                          "<size=90%>1 Trophy</size>\n" +
                          "<color=#B9A58A><size=75%>Seeker, Gjall, or Seeker Brute</size></color>";

            if (_lastSapCost > 0)
                payment += $"\n<color=#E8D8B8>+ {_lastSapCost} Sap</color>";

            _paymentText.text = payment;
        }
    }
}
