using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

// コードのみでuGUI/TMP階層を組み立てるためのヘルパー．
// このプロジェクトはEditor上でのシーン編集を前提とせず，
// Bootstrap.csが実行時に全UIを構築する方式を取る．
public static class UIFactory
{
    public static Canvas CreateRootCanvas(string name = "RootCanvas")
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static EventSystem EnsureEventSystem()
    {
        var existing = Object.FindObjectOfType<EventSystem>();
        if (existing != null) return existing;

        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        return go.GetComponent<EventSystem>();
    }

    public static GameObject CreatePanel(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        go.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 1f);
        return go;
    }

    public static TextMeshProUGUI CreateText(Transform parent, string content, int fontSize, TMP_FontAsset font, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color ?? Color.white;
        tmp.enableWordWrapping = true;
        if (font != null) tmp.font = font;

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 900;

        return tmp;
    }

    public static Button CreateButton(Transform parent, string label, TMP_FontAsset font, UnityAction onClick, Vector2 size, Color? bg = null, Color? textColor = null, int fontSize = 28, bool showLabel = true)
    {
        var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = size.x;
        le.preferredHeight = size.y;

        var img = go.GetComponent<Image>();
        img.color = bg ?? new Color(0.2f, 0.4f, 0.8f, 1f);

        var btn = go.GetComponent<Button>();
        if (onClick != null) btn.onClick.AddListener(onClick);

        if (showLabel)
        {
            var text = CreateText(go.transform, label, fontSize, font, textColor ?? Color.white);
            var textLe = text.GetComponent<LayoutElement>();
            if (textLe != null) Object.Destroy(textLe);
            var trect = text.GetComponent<RectTransform>();
            trect.anchorMin = Vector2.zero;
            trect.anchorMax = Vector2.one;
            trect.offsetMin = Vector2.zero;
            trect.offsetMax = Vector2.zero;
        }

        return btn;
    }

    public static TMP_InputField CreateInputField(Transform parent, string placeholder, TMP_FontAsset font, Vector2 size)
    {
        var go = new GameObject("InputField", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = size.x;
        le.preferredHeight = size.y;

        go.GetComponent<Image>().color = Color.white;

        var textArea = new GameObject("TextArea", typeof(RectTransform));
        textArea.transform.SetParent(go.transform, false);
        var textAreaRect = textArea.GetComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 6);
        textAreaRect.offsetMax = new Vector2(-10, -6);
        textArea.AddComponent<RectMask2D>();

        var placeholderText = CreateText(textArea.transform, placeholder, 24, font, new Color(0.5f, 0.5f, 0.5f), TextAlignmentOptions.MidlineLeft);
        Object.Destroy(placeholderText.GetComponent<LayoutElement>());
        placeholderText.fontStyle = FontStyles.Italic;
        var placeholderRect = placeholderText.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;

        var valueText = CreateText(textArea.transform, "", 24, font, Color.black, TextAlignmentOptions.MidlineLeft);
        Object.Destroy(valueText.GetComponent<LayoutElement>());
        var valueRect = valueText.GetComponent<RectTransform>();
        valueRect.anchorMin = Vector2.zero;
        valueRect.anchorMax = Vector2.one;
        valueRect.offsetMin = Vector2.zero;
        valueRect.offsetMax = Vector2.zero;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport = textAreaRect;
        input.textComponent = valueText;
        input.placeholder = placeholderText;
        if (font != null) input.fontAsset = font;

        return input;
    }
}
