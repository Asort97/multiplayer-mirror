using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsUIBindings
{
    public GameObject panelObject;
    public GameObject windowObject;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Button closeButton;
    public Button quitButton;
    public GameObject exitConfirmPanel;
    public Button exitYesButton;
    public Button exitNoButton;
}

public static class SettingsUIFactory
{
    public static SettingsUIBindings Ensure(GameObject panelObject)
    {
        if (panelObject == null)
            return null;

        var panelRect = panelObject.GetComponent<RectTransform>();
        if (panelRect == null)
            panelRect = panelObject.AddComponent<RectTransform>();

        var panelImage = panelObject.GetComponent<Image>();
        if (panelImage == null)
            panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);
        panelImage.raycastTarget = true;

        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var window = FindOrCreateChild(panelObject.transform, "Window");
        var windowRect = window.GetComponent<RectTransform>();
        if (windowRect == null)
            windowRect = window.AddComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.anchoredPosition = Vector2.zero;
        windowRect.sizeDelta = new Vector2(560f, 380f);

        var windowImage = window.GetComponent<Image>();
        if (windowImage == null)
            windowImage = window.AddComponent<Image>();
        windowImage.color = new Color(0.16f, 0.18f, 0.2f, 0.98f);
        windowImage.raycastTarget = true;

        var title = EnsureText(window.transform, "Title", "НАСТРОЙКИ", new Vector2(0f, -32f), new Vector2(320f, 46f), 30f, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;

        var musicLabel = EnsureText(window.transform, "MusicLabel", "Музыка", new Vector2(-160f, -120f), new Vector2(170f, 30f), 18f, TextAlignmentOptions.Left);
        var sfxLabel = EnsureText(window.transform, "SfxLabel", "Звуки", new Vector2(-160f, -190f), new Vector2(170f, 30f), 18f, TextAlignmentOptions.Left);
        musicLabel.raycastTarget = false;
        sfxLabel.raycastTarget = false;

        var musicSlider = EnsureSlider(window.transform, "MusicSlider", new Vector2(55f, -120f));
        var sfxSlider = EnsureSlider(window.transform, "SfxSlider", new Vector2(55f, -190f));

        var quitButton = EnsureButton(window.transform, "QuitButton", "Выйти", new Vector2(0f, -300f), new Vector2(220f, 46f));
        var closeButton = EnsureButton(window.transform, "CloseButton", "X", new Vector2(244f, -24f), new Vector2(42f, 42f));

        var exitConfirm = FindOrCreateChild(window.transform, "ExitConfirmPanel");
        var exitRect = exitConfirm.GetComponent<RectTransform>();
        if (exitRect == null)
            exitRect = exitConfirm.AddComponent<RectTransform>();
        exitRect.anchorMin = new Vector2(0.5f, 0.5f);
        exitRect.anchorMax = new Vector2(0.5f, 0.5f);
        exitRect.pivot = new Vector2(0.5f, 0.5f);
        exitRect.anchoredPosition = new Vector2(0f, -20f);
        exitRect.sizeDelta = new Vector2(420f, 170f);

        var exitImage = exitConfirm.GetComponent<Image>();
        if (exitImage == null)
            exitImage = exitConfirm.AddComponent<Image>();
        exitImage.color = new Color(0.1f, 0.1f, 0.12f, 0.98f);
        exitConfirm.SetActive(false);

        var exitText = EnsureText(exitConfirm.transform, "ExitText", "Вы уверены, что хотите выйти?", new Vector2(0f, -44f), new Vector2(340f, 60f), 18f, TextAlignmentOptions.Center);
        exitText.textWrappingMode = TextWrappingModes.Normal;
        var yesButton = EnsureButton(exitConfirm.transform, "YesButton", "Да", new Vector2(-90f, -118f), new Vector2(130f, 42f));
        var noButton = EnsureButton(exitConfirm.transform, "NoButton", "Нет", new Vector2(90f, -118f), new Vector2(130f, 42f));

        return new SettingsUIBindings
        {
            panelObject = panelObject,
            windowObject = window,
            musicSlider = musicSlider,
            sfxSlider = sfxSlider,
            closeButton = closeButton,
            quitButton = quitButton,
            exitConfirmPanel = exitConfirm,
            exitYesButton = yesButton,
            exitNoButton = noButton
        };
    }

    public static Button CloneButton(Button template, string name, string text, Transform parent, Vector2 anchoredPosition)
    {
        if (template == null || parent == null)
            return null;

        var existing = parent.Find(name);
        if (existing != null)
        {
            var existingButton = existing.GetComponent<Button>();
            if (existingButton != null)
                return existingButton;
        }

        var clone = Object.Instantiate(template.gameObject, parent);
        clone.name = name;
        var rect = clone.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        var buttonText = clone.GetComponentInChildren<TextMeshProUGUI>(true);
        if (buttonText != null)
            buttonText.text = text;
        return clone.GetComponent<Button>();
    }

    private static GameObject FindOrCreateChild(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI EnsureText(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        var go = FindOrCreateChild(parent, name);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var textComponent = go.GetComponent<TextMeshProUGUI>();
        if (textComponent == null)
            textComponent = go.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    private static Button EnsureButton(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 size)
    {
        var go = FindOrCreateChild(parent, name);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var image = go.GetComponent<Image>();
        if (image == null)
            image = go.AddComponent<Image>();
        image.color = new Color(0.26f, 0.33f, 0.4f, 1f);

        var button = go.GetComponent<Button>();
        if (button == null)
            button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.targetGraphic = image;

        var label = EnsureText(go.transform, "Text", text, new Vector2(0f, 0f), size, 18f, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    private static Slider EnsureSlider(Transform parent, string name, Vector2 anchoredPosition)
    {
        var go = FindOrCreateChild(parent, name);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(280f, 20f);

        var slider = go.GetComponent<Slider>();
        if (slider == null)
            slider = go.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;

        var background = FindOrCreateChild(go.transform, "Background");
        var bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.25f);
        bgRect.anchorMax = new Vector2(1f, 0.75f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = background.GetComponent<Image>();
        if (bgImage == null)
            bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.18f, 1f);

        var fillArea = FindOrCreateChild(go.transform, "Fill Area");
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(10f, 0f);
        fillAreaRect.offsetMax = new Vector2(-10f, 0f);

        var fill = FindOrCreateChild(fillArea.transform, "Fill");
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = fill.GetComponent<Image>();
        if (fillImage == null)
            fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.95f, 0.75f, 0.22f, 1f);

        var handleArea = FindOrCreateChild(go.transform, "Handle Slide Area");
        var handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = new Vector2(0f, 0f);
        handleAreaRect.anchorMax = new Vector2(1f, 1f);
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        var handle = FindOrCreateChild(handleArea.transform, "Handle");
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(22f, 22f);
        var handleImage = handle.GetComponent<Image>();
        if (handleImage == null)
            handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;

        slider.targetGraphic = handleImage;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        return slider;
    }
}
