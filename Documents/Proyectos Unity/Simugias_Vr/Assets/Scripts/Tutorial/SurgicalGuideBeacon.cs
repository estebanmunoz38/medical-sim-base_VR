using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Indicación de entrenamiento en espacio mundial.
/// Estética clínica (sin look de videojuego): panel oscuro, tipografía neutra.
/// </summary>
public class SurgicalGuideBeacon : MonoBehaviour
{
    [SerializeField] Transform lookTarget;
    [SerializeField] Vector3 worldOffset = new Vector3(0f, 0.045f, 0f);
    [SerializeField] float canvasScale = 0.00045f;

    Canvas _canvas;
    Text _title;
    Text _body;
    Image _panel;
    bool _visible = true;

    /// <summary>
    /// El tutorial unificado reemplaza los carteles locales de cada herramienta.
    /// </summary>
    public static bool SuppressAll { get; set; }

    public static SurgicalGuideBeacon Attach(Transform anchor, string title, string body, Vector3 localOffset)
    {
        if (anchor == null) return null;

        var existing = anchor.GetComponentInChildren<SurgicalGuideBeacon>(true);
        if (existing != null)
        {
            existing.SetText(title, body);
            existing.SetVisible(true);
            return existing;
        }

        var go = new GameObject("GuideBeacon");
        go.transform.SetParent(anchor, false);
        go.transform.localPosition = localOffset;
        go.transform.localRotation = Quaternion.identity;

        var beacon = go.AddComponent<SurgicalGuideBeacon>();
        beacon.BuildUi();
        beacon.SetText(title, body);
        return beacon;
    }

    void Awake()
    {
        if (_canvas == null)
            BuildUi();
    }

    void LateUpdate()
    {
        if (SuppressAll)
        {
            _visible = false;
            if (_canvas != null)
                _canvas.enabled = false;
            return;
        }

        if (!_visible) return;

        Camera cam = Camera.main;
        if (lookTarget != null)
            transform.LookAt(lookTarget.position, Vector3.up);
        else if (cam != null)
            transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward, cam.transform.rotation * Vector3.up);
    }

    public void SetText(string title, string body)
    {
        if (_title != null) _title.text = title ?? string.Empty;
        if (_body != null) _body.text = body ?? string.Empty;
    }

    public void SetVisible(bool visible)
    {
        _visible = visible && !SuppressAll;
        if (_canvas != null)
            _canvas.enabled = _visible;
    }

    void BuildUi()
    {
        _canvas = gameObject.GetComponent<Canvas>();
        if (_canvas == null)
            _canvas = gameObject.AddComponent<Canvas>();

        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main;

        var rt = (RectTransform)_canvas.transform;
        rt.sizeDelta = new Vector2(520f, 180f);
        transform.localScale = Vector3.one * canvasScale;
        transform.localPosition += worldOffset;

        if (GetComponent<CanvasScaler>() == null)
            gameObject.AddComponent<CanvasScaler>();
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        _panel = CreateChild<Image>("Panel");
        _panel.color = new Color(0.07f, 0.12f, 0.14f, 0.94f);
        Stretch(_panel.rectTransform);

        var accent = CreateChild<Image>("Accent");
        accent.color = new Color(0.55f, 0.78f, 0.80f, 1f);
        var accentRt = accent.rectTransform;
        accentRt.anchorMin = new Vector2(0f, 0f);
        accentRt.anchorMax = new Vector2(0f, 1f);
        accentRt.pivot = new Vector2(0f, 0.5f);
        accentRt.sizeDelta = new Vector2(8f, 0f);
        accentRt.anchoredPosition = Vector2.zero;

        _title = CreateText("Title", 28, FontStyle.Bold);
        var titleRt = _title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 0.58f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = new Vector2(24f, 0f);
        titleRt.offsetMax = new Vector2(-16f, -10f);

        _body = CreateText("Body", 22, FontStyle.Normal);
        var bodyRt = _body.rectTransform;
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 0.62f);
        bodyRt.offsetMin = new Vector2(24f, 12f);
        bodyRt.offsetMax = new Vector2(-16f, 0f);
        _body.color = new Color(0.88f, 0.92f, 0.93f, 1f);
    }

    T CreateChild<T>(string name) where T : Component
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        return go.AddComponent<T>();
    }

    Text CreateText(string name, int size, FontStyle style)
    {
        var text = CreateChild<Text>(name);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Font.CreateDynamicFontFromOSFont("Segoe UI", size);
        if (text.font == null)
            text.font = Font.CreateDynamicFontFromOSFont("Arial", size);
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
