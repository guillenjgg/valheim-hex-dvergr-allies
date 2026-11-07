using HexDvergrAllies.Core;
using HexDvergrAllies.Core.HexLocalization;
using HarmonyLib;
using HexDvergrAllies.Assets;
using HexDvergrAllies.Core.Trading;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HexDvergrAllies.Core.UI
{
    internal static class DvergrTraderDisplayManager
    {
        private static readonly AccessTools.FieldRef<StoreGui, Trader> TraderField = AccessTools.FieldRefAccess<StoreGui, Trader>("m_trader");
        private static readonly AccessTools.FieldRef<StoreGui, List<GameObject>> ItemListField = AccessTools.FieldRefAccess<StoreGui, List<GameObject>>("m_itemList");

        private static GameObject _paymentPanel;
        private static TMP_Text _paymentText;
        private static int _lastSapCost = -1;
        private static bool _lastHasPrice;
        private static string _lastLanguage;
        private static Heightmap.Biome _lastBiome = Heightmap.Biome.None;

        internal static void OnFillList(StoreGui __instance)
        {
            Trader trader = TraderField(__instance);

            if (!trader || !DvergrTraderManager.IsDvergrTrader(trader))
            {
                return;
            }

            CreatePaymentPanel(__instance);
            UpdatePaymentPanel(trader);
            UpdateItemPrices(trader, ItemListField(__instance));
            UpdateAffordability(trader, ItemListField(__instance));
        }

        internal static void OnUpdate(StoreGui __instance)
        {
            if (!__instance.m_rootPanel.activeSelf)
            {
                return;
            }

            Trader trader = TraderField(__instance);
            bool isDvergrTrader = trader && DvergrTraderManager.IsDvergrTrader(trader);

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

            Features.Progression.DvergrStarterVendorService.Update(trader);
            Heightmap.Biome biome = Heightmap.FindBiome(trader.transform.position);
            bool biomeChanged = biome != _lastBiome;
            var language = Localization.instance.GetSelectedLanguage();
            var languageChanged = language != _lastLanguage;
            _lastLanguage = language;

            if (biomeChanged || languageChanged || Features.Progression.DvergrStarterVendorService.IsStarter(trader))
            {
                UpdatePaymentPanel(trader);
            }

            List<GameObject> itemList = ItemListField(__instance);

            if (biomeChanged || languageChanged || _lastSapCost != DvergrTradeRpcManager.CurrentSapCost || _lastHasPrice != DvergrTradeRpcManager.HasCurrentPrice)
            {
                UpdateItemPrices(trader, itemList);
            }

            UpdateAffordability(trader, itemList);
        }

        private static void CreatePaymentPanel(StoreGui storeGui)
        {
            if (_paymentPanel)
            {
                return;
            }

            RectTransform buyButton = storeGui.m_buyButton.GetComponent<RectTransform>();
            TMP_Text sourceText = storeGui.m_buyButton.GetComponentInChildren<TMP_Text>();

            _paymentPanel = new GameObject("HexDvergrPaymentPanel", typeof(RectTransform), typeof(Image));
            _paymentPanel.transform.SetParent(buyButton.parent, false);

            RectTransform panelRect = _paymentPanel.GetComponent<RectTransform>();

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(220f, 90f);
            panelRect.position = buyButton.position;
            panelRect.anchoredPosition += new Vector2(0f, 48f);

            Image background = _paymentPanel.GetComponent<Image>();
            background.color = new Color(0.12f, 0.10f, 0.08f, 0.92f);
            background.raycastTarget = false;

            Outline outline = _paymentPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.42f, 0.25f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;

            GameObject paymentObject = Object.Instantiate(sourceText.gameObject, _paymentPanel.transform);
            paymentObject.name = "PaymentText";

            _paymentText = paymentObject.GetComponent<TMP_Text>();
            _paymentText.alignment = TextAlignmentOptions.Center;
            _paymentText.textWrappingMode = TextWrappingModes.Normal;
            _paymentText.fontSize = 15f;
            _paymentText.raycastTarget = false;

            RectTransform textRect = paymentObject.GetComponent<RectTransform>();

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, -8f);

            _lastSapCost = -1;
            _lastHasPrice = false;
            _lastBiome = Heightmap.Biome.None;
        }

        private static void UpdatePaymentPanel(Trader trader)
        {
            if (!_paymentText)
            {
                return;
            }

            Heightmap.Biome biome = Heightmap.FindBiome(trader.transform.position);
            _lastBiome = biome;

            if (Features.Progression.DvergrStarterVendorService.IsStarter(trader))
            {
                _paymentText.text = Features.Progression.DvergrStarterVendorService.Description(trader);
            }
            else if (biome == Heightmap.Biome.AshLands)
            {
                _paymentText.text = "<color=#D9A75D><b>" + HexLocalizationManager.Localize("$hex_dvergr_currency_heading") + "</b></color>\n<size=85%>" + HexLocalizationManager.Localize("$hex_dvergr_flametal_description", DvergrPricingConfig.AshlandsMoltenCoreCost, DvergrPricingConfig.AshlandsFlametalQuantity) + "</size>";
            }
            else
            {
                _paymentText.text = "<color=#D9A75D><b>" + HexLocalizationManager.Localize("$hex_dvergr_accepted_trophies") + "</b></color>\n<size=85%>" + HexLocalizationManager.Localize("$hex_dvergr_accepted_trophy_names") + "</size>";
            }
        }

        private static void UpdateItemPrices(Trader trader, List<GameObject> itemList)
        {
            _lastSapCost = DvergrTradeRpcManager.CurrentSapCost;
            _lastHasPrice = DvergrTradeRpcManager.HasCurrentPrice;

            List<Trader.TradeItem> tradeItems = trader.GetAvailableItems();

            for (int i = 0; i < tradeItems.Count && i < itemList.Count; i++)
            {
                Trader.TradeItem tradeItem = tradeItems[i];

                if (tradeItem == null || !tradeItem.m_prefab || !itemList[i])
                {
                    continue;
                }

                Transform item = itemList[i].transform;

                if (DvergrTraderManager.IsForgePact(tradeItem))
                {
                    tradeItem.m_tooltip = Features.Progression.DvergrStarterVendorService.PactTooltip(trader.GetComponent<Character>());
                    SetTradeItemName(item, tradeItem, "$hex_dvergr_forge_pact_name");
                    SetForgePactIcon(item);
                    Features.Progression.DvergrStarterVendorService.UpdatePactTooltip(trader, itemList[i]);

                    TMP_Text priceText = ConfigureCustomPrice(item);
                    priceText.text = Core.Features.Progression.DvergrStarterEncounterManager.PactPrice(trader.GetComponent<Character>());
                    continue;
                }

                if (DvergrMistlandsStoreManager.IsFallenBrother(tradeItem))
                {
                    SetTradeItemName(item, tradeItem, "$hex_dvergr_fallen_brother_name");
                    TMP_Text priceText = ConfigureCustomPrice(item);
                    priceText.text = DvergrPricingConfig.FallenBrotherCostDisplay;
                    continue;
                }

                if (DvergrTraderManager.IsAshlandsFlametal(tradeItem))
                {
                    TMP_Text priceText = ConfigureCustomPrice(item);
                    priceText.text = DvergrPricingConfig.AshlandsFlametalCostDisplay;
                    continue;
                }

                if (DvergrMistlandsStoreManager.IsCoinPurchase(tradeItem))
                {
                    continue;
                }

                if (tradeItem.m_prefab.gameObject.name == "DvergrNeedle")
                {
                    TMP_Text priceText = ConfigureCustomPrice(item);

                    priceText.text = !_lastHasPrice
                        ? HexLocalizationManager.Localize("$hex_dvergr_loading")
                        : _lastSapCost > 0
                            ? HexLocalizationManager.Localize("$hex_dvergr_trophy_sap_price", _lastSapCost)
                            : HexLocalizationManager.Localize("$hex_dvergr_trophy_price");
                }
            }
        }

        private static void SetTradeItemName(Transform item, Trader.TradeItem tradeItem, string localizationKey)
        {
            string sharedName = tradeItem.m_prefab.m_itemData.m_shared.m_name;
            string localizedName = Localization.instance.Localize(sharedName);

            TMP_Text[] textComponents = item.GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text textComponent in textComponents)
            {
                if (textComponent.gameObject.name == "price")
                {
                    continue;
                }

                if (textComponent.text == localizedName || textComponent.text == sharedName || textComponent.text == "Tasty Mead")
                {
                    textComponent.text = HexLocalizationManager.Localize(localizationKey);
                    return;
                }
            }

            foreach (TMP_Text textComponent in textComponents)
            {
                string objectName = textComponent.gameObject.name.ToLowerInvariant();

                if (objectName == "name" || objectName == "itemname" || objectName == "item_name")
                {
                    textComponent.text = HexLocalizationManager.Localize(localizationKey);
                    return;
                }
            }

            HexLog.LogWarning("[Dvergr Trader] Forge Pact name field not found.");
        }

        private static TMP_Text ConfigureCustomPrice(Transform item)
        {
            Transform coinBackground = item.Find("coin_bkg");

            if (coinBackground)
            {
                coinBackground.gameObject.SetActive(true);

                Image backgroundImage = coinBackground.GetComponent<Image>();

                if (backgroundImage)
                {
                    backgroundImage.enabled = false;
                }

                Transform coinIcon = coinBackground.Find("coin icon");

                if (coinIcon)
                {
                    coinIcon.gameObject.SetActive(false);
                }
            }

            Transform priceTransform = Utils.FindChild(item, "price", Utils.IterativeSearchType.DepthFirst);
            TMP_Text priceText = priceTransform.GetComponent<TMP_Text>();

            priceText.color = Color.white;
            priceText.enableAutoSizing = false;
            priceText.fontSizeMin = 15f;

            RectTransform priceRect = priceText.GetComponent<RectTransform>();

            priceRect.anchorMin = Vector2.zero;
            priceRect.anchorMax = Vector2.one;
            priceRect.offsetMin = Vector2.zero;
            priceRect.offsetMax = Vector2.zero;

            return priceText;
        }

        private static void UpdateAffordability(Trader trader, List<GameObject> itemList)
        {
            List<Trader.TradeItem> tradeItems = trader.GetAvailableItems();

            bool canAffordExtractor = DvergrTradeAffordability.CanAffordExtractor();
            bool canAffordFlametal = DvergrTraderManager.CanAffordAshlandsFlametal(Player.m_localPlayer);

            Character character = trader.GetComponent<Character>();
            bool canAffordForgePact = DvergrTraderManager.CanAffordForgePact(Player.m_localPlayer, character);

            for (int i = 0; i < tradeItems.Count && i < itemList.Count; i++)
            {
                Trader.TradeItem tradeItem = tradeItems[i];

                if (tradeItem == null || !tradeItem.m_prefab || !itemList[i])
                {
                    continue;
                }

                if (DvergrMistlandsStoreManager.IsCoinPurchase(tradeItem))
                {
                    continue;
                }

                bool canAfford;

                if (DvergrTraderManager.IsForgePact(tradeItem))
                {
                    canAfford = canAffordForgePact;
                }
                else if (DvergrMistlandsStoreManager.IsFallenBrother(tradeItem))
                {
                    canAfford = DvergrMistlandsStoreManager.CanAffordFallenBrother(Player.m_localPlayer);
                }
                else if (DvergrTraderManager.IsAshlandsFlametal(tradeItem))
                {
                    canAfford = canAffordFlametal;
                }
                else
                {
                    canAfford = canAffordExtractor;
                }

                GameObject item = itemList[i];
                CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();

                if (!canvasGroup)
                {
                    canvasGroup = item.AddComponent<CanvasGroup>();
                }

                canvasGroup.alpha = canAfford ? 1f : 0.5f;
            }
        }

        private static void SetForgePactIcon(Transform item)
        {
            if (!AssetHelper.DvergrPortrait)
            {
                return;
            }

            Transform iconTransform = Utils.FindChild(item, "icon", Utils.IterativeSearchType.DepthFirst);

            if (!iconTransform)
            {
                HexLog.LogWarning("[Dvergr Trader] Forge Pact icon not found.");
                return;
            }

            Image iconImage = iconTransform.GetComponent<Image>();

            if (iconImage)
            {
                iconImage.sprite = AssetHelper.DvergrPortrait;
                iconImage.preserveAspect = true;
            }
        }
    }
}
