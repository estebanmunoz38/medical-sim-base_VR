using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Indicador mundial: flecha, anillo, etiqueta y línea hacia el objetivo.
/// Si el objetivo queda fuera de vista, muestra una chevron frente a la cámara.
/// </summary>
public class TutorialWorldMarker : MonoBehaviour
{
    Transform _target;
    LineRenderer _line;
    Transform _ring;
    Transform _arrow;
    Transform _lookCue;
    Text _label;
    Text _lookLabel;
    Camera _cam;
    bool _visible;
    bool _strongPulse;
    string _labelText;

    public static TutorialWorldMarker Create() => Create(null);

    public static TutorialWorldMarker Create(Transform parent)
    {
        var go = new GameObject("TutorialWorldMarker");
        if (parent != null)
            go.transform.SetParent(parent, false);
        var marker = go.AddComponent<TutorialWorldMarker>();
        marker.Build();
        return marker;
    }

    public void SetTarget(Transform target, string label)
    {
        _target = target;
        _labelText = label ?? string.Empty;
        if (_label != null)
            _label.text = _labelText;
        SetVisible(target != null);
    }

    public void SetStrongPulse(bool strong) => _strongPulse = strong;

    public void SetVisible(bool visible)
    {
        _visible = visible && _target != null;
        if (_ring != null)
            _ring.gameObject.SetActive(_visible);
        if (_arrow != null)
            _arrow.gameObject.SetActive(_visible);
        if (_line != null)
            _line.enabled = false;
        if (_lookCue != null && !_visible)
            _lookCue.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (!_visible || _target == null)
            return;

        if (_cam == null)
            _cam = Camera.main;

        Vector3 pos = _target.position + Vector3.up * 0.025f;
        float bob = Mathf.Sin(Time.time * (_strongPulse ? 5.2f : 3.2f)) * (_strongPulse ? 0.018f : 0.01f);

        if (_ring != null)
        {
            _ring.position = pos;
            float pulse = (_strongPulse ? 0.055f : 0.042f) + Mathf.Sin(Time.time * 3.2f) * 0.01f;
            _ring.localScale = Vector3.one * pulse;
            if (_cam != null)
                _ring.rotation = _cam.transform.rotation;
        }

        if (_arrow != null)
        {
            _arrow.position = pos + Vector3.up * (0.11f + bob);
            _arrow.rotation = Quaternion.LookRotation(Vector3.down, _cam != null ? _cam.transform.right : Vector3.right);
        }

        UpdateLookCue(pos);
    }

    void UpdateLookCue(Vector3 targetPos)
    {
        if (_lookCue == null || _cam == null)
            return;

        Vector3 toTarget = targetPos - _cam.transform.position;
        float angle = Vector3.Angle(_cam.transform.forward, toTarget);
        bool offView = angle > 38f;
        _lookCue.gameObject.SetActive(offView);
        if (!offView)
            return;

        Vector3 planar = Vector3.ProjectOnPlane(toTarget.normalized, _cam.transform.forward);
        if (planar.sqrMagnitude < 0.0001f)
            planar = _cam.transform.right;
        planar.Normalize();

        _lookCue.position = _cam.transform.position
                            + _cam.transform.forward * 0.95f
                            + planar * 0.34f
                            + _cam.transform.up * -0.12f;
        _lookCue.rotation = Quaternion.LookRotation(_cam.transform.forward, _cam.transform.up);

        if (_lookLabel != null)
            _lookLabel.text = string.IsNullOrEmpty(_labelText) ? "Mire hacia acá" : "Mire: " + _labelText;
    }

    void Build()
    {
        Material mat = MakeUnlit(new Color(0.55f, 0.82f, 0.84f, 0.9f));

        _ring = new GameObject("Ring").transform;
        _ring.SetParent(transform, false);

        var canvasGo = new GameObject("LabelCanvas");
        canvasGo.transform.SetParent(_ring, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var crt = (RectTransform)canvasGo.transform;
        crt.sizeDelta = new Vector2(520f, 96f);
        canvasGo.transform.localScale = Vector3.one * 0.00145f;
        canvasGo.transform.localPosition = new Vector3(0f, 0.12f, 0f);

        var bg = canvasGo.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.12f, 0.14f, 0.92f);
        bg.raycastTarget = false;

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(canvasGo.transform, false);
        _label = textGo.AddComponent<Text>();
        _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_label.font == null)
            _label.font = Font.CreateDynamicFontFromOSFont("Segoe UI", 24);
        _label.fontSize = 32;
        _label.fontStyle = FontStyle.Bold;
        _label.color = Color.white;
        _label.alignment = TextAnchor.MiddleCenter;
        _label.horizontalOverflow = HorizontalWrapMode.Wrap;
        _label.raycastTarget = false;
        var trt = _label.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8f, 4f);
        trt.offsetMax = new Vector2(-8f, -4f);

        var ringVis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringVis.name = "Pulse";
        ringVis.transform.SetParent(_ring, false);
        ringVis.transform.localScale = new Vector3(1f, 0.035f, 1f);
        Object.Destroy(ringVis.GetComponent<Collider>());
        ringVis.GetComponent<Renderer>().sharedMaterial = mat;

        _arrow = new GameObject("Arrow").transform;
        _arrow.SetParent(transform, false);

        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Shaft";
        shaft.transform.SetParent(_arrow, false);
        shaft.transform.localPosition = new Vector3(0f, 0f, -0.045f);
        shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shaft.transform.localScale = new Vector3(0.012f, 0.04f, 0.012f);
        Object.Destroy(shaft.GetComponent<Collider>());
        shaft.GetComponent<Renderer>().sharedMaterial = mat;

        var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        head.transform.SetParent(_arrow, false);
        head.transform.localPosition = new Vector3(0f, 0f, 0.012f);
        head.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
        head.transform.localScale = new Vector3(0.038f, 0.038f, 0.038f);
        Object.Destroy(head.GetComponent<Collider>());
        head.GetComponent<Renderer>().sharedMaterial = mat;

        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.startWidth = 0.004f;
        _line.endWidth = 0.0018f;
        _line.sharedMaterial = mat;
        _line.startColor = new Color(0.55f, 0.82f, 0.84f, 0.9f);
        _line.endColor = new Color(0.55f, 0.82f, 0.84f, 0.25f);
        _line.useWorldSpace = true;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;

        BuildLookCue();
    }

    void BuildLookCue()
    {
        _lookCue = new GameObject("LookCue").transform;
        _lookCue.SetParent(transform, false);

        var canvasGo = new GameObject("LookCanvas");
        canvasGo.transform.SetParent(_lookCue, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var crt = (RectTransform)canvasGo.transform;
        crt.sizeDelta = new Vector2(280f, 64f);
        canvasGo.transform.localScale = Vector3.one * 0.001f;

        var bg = canvasGo.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.14f, 0.16f, 0.94f);
        bg.raycastTarget = false;

        var textGo = new GameObject("LookLabel");
        textGo.transform.SetParent(canvasGo.transform, false);
        _lookLabel = textGo.AddComponent<Text>();
        _lookLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_lookLabel.font == null)
            _lookLabel.font = Font.CreateDynamicFontFromOSFont("Segoe UI", 22);
        _lookLabel.fontSize = 22;
        _lookLabel.fontStyle = FontStyle.Bold;
        _lookLabel.color = new Color(0.85f, 0.94f, 0.95f, 1f);
        _lookLabel.alignment = TextAnchor.MiddleCenter;
        _lookLabel.raycastTarget = false;
        var trt = _lookLabel.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        _lookCue.gameObject.SetActive(false);
    }

    static Material MakeUnlit(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default")
                        ?? Shader.Find("Unlit/Color")
                        ?? Shader.Find("Universal Render Pipeline/Unlit")
                        ?? Shader.Find("Standard");
        var mat = new Material(shader);
        mat.color = color;
        return mat;
    }
}
