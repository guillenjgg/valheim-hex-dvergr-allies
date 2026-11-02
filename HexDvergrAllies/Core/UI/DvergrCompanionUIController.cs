
using HexDvergrAllies.Core.Companions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HexDvergrAllies.Core.UI
{
    internal static class DvergrCompanionUIController
    {
        private const string NameKey = "HexDvergr_Name";
        private const string ModeKey = "HexDvergr_Mode";

        private static GameObject _window;
        private static TextMeshProUGUI _descriptionText;
        private static Button _modeButton;
        private static TextMeshProUGUI _modeButtonText;

        internal static bool IsVisible => _window && _window.activeSelf;

        internal static void Toggle()
        {
            if (IsVisible)
            {
                Close();
                return;
            }

            if (Menu.IsVisible() || InventoryGui.IsVisible() || StoreGui.IsVisible() || TextInput.IsVisible() || global::Console.IsVisible() || Minimap.IsOpen())
            {
                return;
            }

            Open();
        }

        private static void Open()
        {
            if (!_window)
            {
                CreateWindow();
            }

            if (!_window)
            {
                return;
            }

            RefreshCompanionInfo();
            _window.SetActive(true);
        }

        internal static void Close()
        {
            if (_window)
            {
                _window.SetActive(false);
            }

            ZCursor.LockState = CursorLockMode.Locked;
        }

        private static void CreateWindow()
        {
            var inventory = InventoryGui.instance;

            if (!inventory)
            {
                return;
            }

            var crafting = inventory.transform.Find("root/Crafting");

            if (!crafting)
            {
                return;
            }

            var backgroundTemplate = crafting.Find("Bkg");
            var titleTemplate = crafting.Find("topic");

            if (!backgroundTemplate || !titleTemplate)
            {
                return;
            }

            _window = new GameObject("HexDvergrCompanionWindow", typeof(RectTransform), typeof(Image));
            _window.transform.SetParent(inventory.transform, false);

            var overlayRect = _window.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var overlayImage = _window.GetComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.55f);
            overlayImage.raycastTarget = true;

            var panel = Object.Instantiate(backgroundTemplate.gameObject, _window.transform, false);
            panel.name = "CompanionPanel";
            panel.SetActive(true);

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(520f, 360f);

            var title = Object.Instantiate(titleTemplate.gameObject, panel.transform, false);
            title.name = "CompanionTitle";
            title.SetActive(true);

            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -24f);
            titleRect.sizeDelta = new Vector2(400f, 40f);

            var titleText = title.GetComponent<TextMeshProUGUI>();
            titleText.text = "Dvergr Companion";
            titleText.alignment = TextAlignmentOptions.Center;

            var titleDragHandler = title.AddComponent<DvergrCompanionDragHandler>();
            titleDragHandler.Initialize(panelRect);
            titleText.raycastTarget = true;

            var description = Object.Instantiate(titleTemplate.gameObject, panel.transform, false);
            description.name = "CompanionDescription";
            description.SetActive(true);

            var descriptionRect = description.GetComponent<RectTransform>();
            descriptionRect.anchorMin = new Vector2(0.5f, 0.5f);
            descriptionRect.anchorMax = new Vector2(0.5f, 0.5f);
            descriptionRect.pivot = new Vector2(0.5f, 0.5f);
            descriptionRect.anchoredPosition = new Vector2(0f, 25f);
            descriptionRect.sizeDelta = new Vector2(440f, 100f);

            var descriptionText = description.GetComponent<TextMeshProUGUI>();
            descriptionText.alignment = TextAlignmentOptions.Center;
            descriptionText.fontSize = 20f;
            descriptionText.textWrappingMode = TextWrappingModes.Normal;

            _descriptionText = descriptionText;

            var buttonTemplate = crafting.GetComponentInChildren<Button>(true);

            var modeButtonObject = new GameObject("ModeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            modeButtonObject.transform.SetParent(panel.transform, false);

            var modeRect = modeButtonObject.GetComponent<RectTransform>();
            modeRect.anchorMin = new Vector2(0.5f, 0.5f);
            modeRect.anchorMax = new Vector2(0.5f, 0.5f);
            modeRect.pivot = new Vector2(0.5f, 0.5f);
            modeRect.anchoredPosition = new Vector2(0f, -85f);
            modeRect.sizeDelta = new Vector2(240f, 48f);

            var modeImage = modeButtonObject.GetComponent<Image>();

            if (buttonTemplate)
            {
                var templateImage = buttonTemplate.GetComponent<Image>();

                if (templateImage)
                {
                    modeImage.sprite = templateImage.sprite;
                    modeImage.type = templateImage.type;
                }
            }

            _modeButton = modeButtonObject.GetComponent<Button>();
            _modeButton.onClick.AddListener(ToggleCompanionMode);

            var modeTextObject = Object.Instantiate(titleTemplate.gameObject, modeButtonObject.transform, false);
            modeTextObject.name = "ModeButtonText";
            modeTextObject.SetActive(true);

            var modeTextRect = modeTextObject.GetComponent<RectTransform>();
            modeTextRect.anchorMin = Vector2.zero;
            modeTextRect.anchorMax = Vector2.one;
            modeTextRect.offsetMin = Vector2.zero;
            modeTextRect.offsetMax = Vector2.zero;

            _modeButtonText = modeTextObject.GetComponent<TextMeshProUGUI>();
            _modeButtonText.alignment = TextAlignmentOptions.Center;
            _modeButtonText.fontSize = 20f;
            _modeButtonText.raycastTarget = false;

            var closeButtonObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeButtonObject.transform.SetParent(panel.transform, false);

            var closeRect = closeButtonObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(0.5f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-32f, -32f);
            closeRect.sizeDelta = new Vector2(46f, 46f);

            var closeImage = closeButtonObject.GetComponent<Image>();

            if (buttonTemplate)
            {
                var templateImage = buttonTemplate.GetComponent<Image>();

                if (templateImage)
                {
                    closeImage.sprite = templateImage.sprite;
                    closeImage.type = templateImage.type;
                }
            }

            var closeButton = closeButtonObject.GetComponent<Button>();
            closeButton.onClick.AddListener(Close);

            var closeTextObject = Object.Instantiate(titleTemplate.gameObject, closeButtonObject.transform, false);
            closeTextObject.name = "CloseText";
            closeTextObject.SetActive(true);

            var closeTextRect = closeTextObject.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;

            var closeText = closeTextObject.GetComponent<TextMeshProUGUI>();
            closeText.text = "X";
            closeText.fontSize = 24f;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.raycastTarget = false;

            _window.SetActive(false);
        }

        private static void ToggleCompanionMode()
        {
            var player = Player.m_localPlayer;

            if (!DvergrCompanionManager.TryGetHiredCompanionZDO(player, out var companionZdo))
            {
                return;
            }

            DvergrCompanionManager.RequestRemoteToggleMode(player);
            _modeButton.interactable = false;
        }

        private static void RefreshCompanionInfo()
        {
            var player = Player.m_localPlayer;

            if (!DvergrCompanionManager.TryGetHiredCompanionZDO(player, out var companionZdo))
            {
                _descriptionText.text = "No hired Dvergr companion.";
                _modeButton.interactable = false;
                _modeButtonText.text = "No Companion";
                return;
            }

            var name = companionZdo.GetString(NameKey, "");
            var mode = (DvergrCompanionManager.CompanionMode)companionZdo.GetInt(ModeKey, 0);

            _descriptionText.text = $"Name: {(string.IsNullOrEmpty(name) ? "Dvergr" : name)}\nMode: {mode}";
            _modeButtonText.text = mode == DvergrCompanionManager.CompanionMode.Sentry ? "Switch to Companion" : "Switch to Sentry";
            _modeButton.interactable = true;
        }

        internal static void Update()
        {
            if (IsVisible)
            {
                RefreshCompanionInfo();
            }
        }
    }
}
