using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tarjeta compacta a distancia de lectura. Solo muestra lo necesario ahora.
/// No intercepta rays XR.
/// </summary>
public class TutorialFollowHud : MonoBehaviour
{
    [SerializeField] float followDistance = 1.42f;
    [SerializeField] float heightOffset = -0.42f;
    [SerializeField] float lateralOffset = 0.38f;
    [SerializeField] float catchUpAngle = 62f;
    [SerializeField] float positionLerp = 3.2f;
    [SerializeField] bool pinBesidePatient = true;

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
    Text _finishBanner;
    GameObject _finishRoot;

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
        if (_hint != null) _hint.text = paused
            ? (HandsOnlySession.Active
                ? "Tutorial en pausa. Tocá CONTINUAR con el dedo."
                : "Tutorial en pausa. Pulse el botón Menú para continuar.")
            : (hint ?? string.Empty);

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

    public void ShowFinishBanner(bool show)
    {
        if (_finishRoot != null)
            _finishRoot.SetActive(show);
        if (show && _finishBanner != null)
            _finishBanner.text = "PROCEDIMIENTO TERMINADO";
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

        if (pinBesidePatient)
        {
            PinBesidePatient();
            return;
        }

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

    void PinBesidePatient()
    {
        Vector3 pos = ResolveBoardPosition();
        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * 4f);
        if (_cam != null)
        {
            Vector3 toCam = _cam.transform.position - transform.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude < 0.0001f)
                toCam = -_cam.transform.forward;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(-toCam.normalized, Vector3.up),
                Time.deltaTime * 4f);
        }
    }

    Vector3 ResolveBoardPosition()
    {
        var baby = GameObject.Find("Bebe_Operaciones");
        if (baby != null)
            return baby.transform.position + Vector3.up * 0.32f + Vector3.right * 0.48f;
        var table = GameObject.Find("Mesa de operaciones");
        if (table != null)
            return table.transform.position + Vector3.up * 1.15f + Vector3.right * 0.4f;
        if (_cam != null)
            return _cam.transform.position
                   + _cam.transform.forward * 0.9f
                   + _cam.transform.right * 0.45f
                   + _cam.transform.up * -0.15f;
        return new Vector3(0.45f, 1.25f, 1.2f);
    }

    void Build()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main;
        var rt = (RectTransform)transform;
        rt.sizeDelta = new Vector2(480f, 200f);
        transform.localScale = Vector3.one * 0.00078f;

        gameObject.AddComponent<CanvasScaler>();
        var raycaster = gameObject.AddComponent<GraphicRaycaster>();
        raycaster.enabled = false;

        _panel = CreateImage("Panel", new Color(0.06f, 0.10f, 0.12f, 0.55f));
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

        _title = CreateText("Title", 26, FontStyle.Bold, Color.white);
        Place(_title.rectTransform, 0.54f, 0.68f);

        _instruction = CreateText("Instruction", 22, FontStyle.Bold, new Color(0.95f, 0.96f, 0.92f, 1f));
        Place(_instruction.rectTransform, 0.24f, 0.54f);

        _hint = CreateText("Hint", 17, FontStyle.Normal, new Color(0.72f, 0.86f, 0.88f, 1f));
        Place(_hint.rectTransform, 0.02f, 0.24f);
        _hint.alignment = TextAnchor.LowerLeft;

        _finishRoot = new GameObject("FinishBanner");
        _finishRoot.transform.SetParent(transform, false);
        var finishImg = _finishRoot.AddComponent<Image>();
        finishImg.color = new Color(0.12f, 0.45f, 0.28f, 0.95f);
        finishImg.raycastTarget = false;
        var frt = finishImg.rectTransform;
        frt.anchorMin = new Vector2(0.08f, 0.38f);
        frt.anchorMax = new Vector2(0.92f, 0.62f);
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        _finishBanner = CreateText("FinishText", 26, FontStyle.Bold, Color.white);
        _finishBanner.transform.SetParent(_finishRoot.transform, false);
        Stretch(_finishBanner.rectTransform);
        _finishBanner.alignment = TextAnchor.MiddleCenter;
        _finishBanner.text = "PROCEDIMIENTO TERMINADO";
        _finishRoot.SetActive(false);
    }

    void Start()
    {
        _cam = Camera.main;
        if (pinBesidePatient)
        {
            transform.position = ResolveBoardPosition();
            if (_cam != null)
            {
                Vector3 toCam = _cam.transform.position - transform.position;
                toCam.y = 0f;
                if (toCam.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
            }
            return;
        }

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
