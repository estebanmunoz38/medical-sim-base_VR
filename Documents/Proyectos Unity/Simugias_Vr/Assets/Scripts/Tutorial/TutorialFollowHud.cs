using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tarjeta compacta a distancia de lectura. Solo muestra lo necesario ahora.
/// No intercepta rays XR.
/// </summary>
public class TutorialFollowHud : MonoBehaviour
{
    [SerializeField] float followDistance = 1.18f;
    [SerializeField] float heightOffset = -0.28f;
    [SerializeField] float lateralOffset = 0.08f;
    [SerializeField] float catchUpAngle = 32f;
    [SerializeField] float positionLerp = 5f;

    Canvas _canvas;
    Text _kicker;
    Text _progress;
    Text _title;
    Text _instruction;
    Text _hint;
    Image _accentBar;
    RectTransform _progressFillRt;
    Image _panel;
    Camera _cam;
    bool _caughtUp;
    Color _accentDefault = new Color(0.52f, 0.76f, 0.78f, 1f);
    Color _accentOk = new Color(0.45f, 0.78f, 0.55f, 1f);
    Color _accentWarn = new Color(0.86f, 0.62f, 0.32f, 1f);
    float _accentUntil;

    public static TutorialFollowHud Create() => Create(null);

    public static TutorialFollowHud Create(Transform parent)
    {
        var go = new GameObject("TutorialFollowHud");
        if (parent != null)
            go.transform.SetParent(parent, false);
        var hud = go.AddComponent<TutorialFollowHud>();
        hud.Build();
        return hud;
    }

    public void BindCamera(Camera cam) => _cam = cam;

    public void SetVisible(bool visible)
    {
        if (_canvas != null)
            _canvas.enabled = visible;
    }

    public void SetCard(
        string kicker,
        string progress,
        string title,
        string instruction,
        string hint,
        float progress01,
        bool paused,
        bool successFlash,
        bool warnFlash)
    {
        if (_kicker != null) _kicker.text = kicker ?? string.Empty;
        if (_progress != null) _progress.text = progress ?? string.Empty;
        if (_title != null) _title.text = title ?? string.Empty;
        if (_instruction != null) _instruction.text = instruction ?? string.Empty;
        if (_hint != null) _hint.text = paused ? "Tutorial en pausa. Pulse el botón Menú para continuar." : (hint ?? string.Empty);

        progress01 = Mathf.Clamp01(progress01);
        if (_progressFillRt != null)
            _progressFillRt.anchorMax = new Vector2(progress01, 1f);

        if (successFlash)
        {
            SetAccent(_accentOk);
            _accentUntil = Time.time + 0.9f;
        }
        else if (warnFlash)
        {
            SetAccent(_accentWarn);
            _accentUntil = Time.time + 1.1f;
        }
    }

    void LateUpdate()
    {
        if (_accentUntil > 0f && Time.time > _accentUntil)
        {
            SetAccent(_accentDefault);
            _accentUntil = 0f;
        }

        if (_cam == null)
            _cam = Camera.main;
        if (_cam == null)
            return;

        Vector3 desired = _cam.transform.position
                          + _cam.transform.forward * followDistance
                          + _cam.transform.up * heightOffset
                          + _cam.transform.right * lateralOffset;

        float angle = Vector3.Angle(_cam.transform.forward, transform.position - _cam.transform.position);
        if (!_caughtUp || angle > catchUpAngle || Vector3.Distance(transform.position, desired) > 0.5f)
            _caughtUp = true;

        if (_caughtUp)
        {
            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * positionLerp);
            transform.rotation = Quaternion.Slerp(transform.rotation, _cam.transform.rotation, Time.deltaTime * positionLerp);
            if (angle < 8f && Vector3.Distance(transform.position, desired) < 0.04f)
                _caughtUp = false;
        }
    }

    void Build()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main;
        var rt = (RectTransform)transform;
        rt.sizeDelta = new Vector2(560f, 250f);
        transform.localScale = Vector3.one * 0.00095f;

        gameObject.AddComponent<CanvasScaler>();
        var raycaster = gameObject.AddComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        _panel = CreateImage("Panel", new Color(0.06f, 0.10f, 0.12f, 0.92f));
        Stretch(_panel.rectTransform);

        _accentBar = CreateImage("Accent", _accentDefault);
        var aRt = _accentBar.rectTransform;
        aRt.anchorMin = new Vector2(0f, 0f);
        aRt.anchorMax = new Vector2(0f, 1f);
        aRt.pivot = new Vector2(0f, 0.5f);
        aRt.sizeDelta = new Vector2(8f, 0f);

        _kicker = CreateText("Kicker", 16, FontStyle.Bold, new Color(0.62f, 0.82f, 0.84f, 1f));
        Place(_kicker.rectTransform, 0.82f, 1f);

        _progress = CreateText("Progress", 16, FontStyle.Normal, new Color(0.84f, 0.90f, 0.91f, 1f));
        Place(_progress.rectTransform, 0.72f, 0.84f);

        var track = CreateImage("ProgressTrack", new Color(1f, 1f, 1f, 0.10f));
        Place(track.rectTransform, 0.68f, 0.72f, 18f);

        var fill = CreateImage("ProgressFill", _accentDefault);
        _progressFillRt = fill.rectTransform;
        _progressFillRt.SetParent(track.rectTransform, false);
        Stretch(_progressFillRt);
        _progressFillRt.anchorMax = new Vector2(0f, 1f);

        _title = CreateText("Title", 22, FontStyle.Bold, Color.white);
        Place(_title.rectTransform, 0.54f, 0.68f);

        _instruction = CreateText("Instruction", 21, FontStyle.Bold, new Color(0.95f, 0.96f, 0.92f, 1f));
        Place(_instruction.rectTransform, 0.24f, 0.54f);

        _hint = CreateText("Hint", 17, FontStyle.Normal, new Color(0.72f, 0.86f, 0.88f, 1f));
        Place(_hint.rectTransform, 0.02f, 0.24f);
        _hint.alignment = TextAnchor.LowerLeft;
    }

    void Start()
    {
        _cam = Camera.main;
        if (_cam == null) return;
        transform.position = _cam.transform.position
                             + _cam.transform.forward * followDistance
                             + _cam.transform.up * heightOffset
                             + _cam.transform.right * lateralOffset;
        transform.rotation = _cam.transform.rotation;
    }

    void SetAccent(Color color)
    {
        if (_accentBar != null)
            _accentBar.color = color;
    }

    Image CreateImage(string name, Color color)
    {
        var img = CreateChild<Image>(name);
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    Text CreateText(string name, int size, FontStyle style, Color color)
    {
        var text = CreateChild<Text>(name);
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Font.CreateDynamicFontFromOSFont("Segoe UI", size);
        if (text.font == null)
            text.font = Font.CreateDynamicFontFromOSFont("Arial", size);
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    T CreateChild<T>(string name) where T : Component
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        return go.AddComponent<T>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform rt, float minY, float maxY, float left = 22f)
    {
        rt.anchorMin = new Vector2(0f, minY);
        rt.anchorMax = new Vector2(1f, maxY);
        rt.offsetMin = new Vector2(left, 4f);
        rt.offsetMax = new Vector2(-16f, -4f);
    }
}
