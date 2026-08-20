using UnityEngine;

public class MarkerVRTool : MonoBehaviour
{
    [Header("Input Source (XR ONLY)")]
    public MonoBehaviour inputSourceBehaviour;   // Debe ser XRToolInputSource
    private IToolInputSource input;

    [Header("Painting Settings")]
    public float brushSize = 12f;
    public Color brushColor = Color.green;

    [Header("LayerMask donde pintar (piel / cabeza)")]
    public LayerMask paintLayers;

    [Header("Textura destino (la asigna PaintingTool o tu sistema actual)")]
    public Renderer targetRenderer;
    public RenderTexture paintTexture;

    [Header("Rasurado previo")]
    [Tooltip("Si está asignado, no se puede demarcar hasta completar el rasurado.")]
    public ShaveVRTool requiredShave;
    public bool paintingEnabled = true;

    [Header("Tutorial")]
    public bool showTutorial = true;
    [TextArea] public string tutorialWaitShave =
        "Demarcación bloqueada. Complete el rasurado del campo antes de pintar la incisión.";
    [TextArea] public string tutorialPaint =
        "Demarcación. Trace la línea de incisión sobre la piel rasurada. Gatillo para pintar.";

    private Material paintMaterial;
    SurgicalGuideBeacon _guide;
    bool _paintingEnabled;
    bool _hasPainted;
    int _strokeCount;

    public bool HasPainted => _hasPainted;
    public int StrokeCount => _strokeCount;

    public void SetPaintingEnabled(bool enabledPainting)
    {
        _paintingEnabled = enabledPainting;
        paintingEnabled = enabledPainting;
    }

    void Awake()
    {
        input = inputSourceBehaviour as IToolInputSource;

        if (input == null)
        {
            Debug.LogError("❌ MarkerVRTool: inputSourceBehaviour NO implementa IToolInputSource.");
            enabled = false;
            return;
        }

        if (targetRenderer == null)
        {
            Debug.LogError("❌ MarkerVRTool: falta asignar targetRenderer.");
            enabled = false;
            return;
        }

        if (requiredShave == null)
            requiredShave = FindFirstObjectByType<ShaveVRTool>();

        _paintingEnabled = paintingEnabled && (requiredShave == null || requiredShave.IsComplete);

        Shader shader = Shader.Find("Hidden/MarkerPainter");
        if (shader != null)
        {
            paintMaterial = new Material(shader);
            paintMaterial.SetColor("_Color", brushColor);
            paintMaterial.SetFloat("_Size", brushSize);
        }

        targetRenderer.material.SetTexture("_PaintTex", paintTexture);

        if (showTutorial)
            _guide = SurgicalGuideBeacon.Attach(transform, "Demarcación", tutorialPaint, new Vector3(0f, 0.05f, 0f));
    }

    void Update()
    {
        bool shaveOk = requiredShave == null || requiredShave.IsComplete;
        bool canPaint = _paintingEnabled && shaveOk;

        if (_guide != null)
        {
            _guide.SetText("Demarcación", canPaint ? tutorialPaint : tutorialWaitShave);
        }

        if (!canPaint)
            return;

        if (!input.PrimaryHeld) return;

        Ray ray = input.PointerRay;

        if (Physics.Raycast(ray, out RaycastHit hit, 1.5f, paintLayers))
        {
            Vector2 uv = hit.textureCoord;

            PaintAtUV(uv);
            _hasPainted = true;
            _strokeCount++;
        }
    }

    void PaintAtUV(Vector2 uv)
    {
        // Set uniform values
        if (paintMaterial == null || paintTexture == null)
            return;

        paintMaterial.SetVector("_UV", new Vector4(uv.x, uv.y, 0, 0));

        RenderTexture active = RenderTexture.active;

        RenderTexture.active = paintTexture;

        // Dibujamos el pincel en la textura
        Graphics.Blit(null, paintTexture, paintMaterial);

        RenderTexture.active = active;
    }

    public void SetBrushSize(float size)
    {
        brushSize = size;
        if (paintMaterial != null) paintMaterial.SetFloat("_Size", size);
    }

    public void SetBrushColor(Color c)
    {
        brushColor = c;
        if (paintMaterial != null) paintMaterial.SetColor("_Color", c);
    }
}
