using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rasurado del cuero cabelludo. Debe completarse antes de la demarcación con marcador.
/// </summary>
public class ShaveVRTool : MonoBehaviour
{
    [Header("Input VR")]
    public MonoBehaviour inputSourceBehaviour;
    IToolInputSource input;

    [Header("Piel / rasurado")]
    public Transform clipperTip;
    public LayerMask scalpLayers = ~0;
    public float contactDistance = 0.02f;
    public Renderer targetRenderer;
    public RenderTexture shaveTexture;
    public Color shavedColor = new Color(0.82f, 0.64f, 0.54f, 1f);
    public float brushSize = 18f;
    [Range(0.1f, 1f)] public float requiredCoverage = 0.55f;
    public int coverageGrid = 24;

    [Header("Tutorial")]
    public bool showTutorial = true;
    [TextArea] public string tutorialText =
        "Rasurado. Pase la máquina en contacto con el cuero cabelludo del campo quirúrgico hasta dejar la piel expuesta. Luego podrá demarcar la incisión.";

    [Header("Desbloqueo")]
    public MarkerVRTool markerToUnlock;

    readonly HashSet<int> _shavedCells = new HashSet<int>();
    Material _paintMaterial;
    SurgicalGuideBeacon _guide;
    bool _complete;

    public bool IsComplete => _complete;
    public float Coverage01 { get; private set; }

    void Start()
    {
        input = inputSourceBehaviour as IToolInputSource;
        if (input == null)
        {
            Debug.LogError("ShaveVRTool: inputSourceBehaviour no implementa IToolInputSource.");
            enabled = false;
            return;
        }

        if (targetRenderer != null && shaveTexture != null)
            targetRenderer.material.SetTexture("_ShaveTex", shaveTexture);

        Shader shader = Shader.Find("Hidden/MarkerPainter");
        if (shader != null)
        {
            _paintMaterial = new Material(shader);
            _paintMaterial.SetColor("_Color", shavedColor);
            _paintMaterial.SetFloat("_Size", brushSize);
        }

        if (showTutorial)
        {
            Transform anchor = clipperTip != null ? clipperTip : transform;
            _guide = SurgicalGuideBeacon.Attach(anchor, "Rasurado", tutorialText, new Vector3(0f, 0.05f, 0f));
        }

        if (markerToUnlock == null)
            markerToUnlock = FindFirstObjectByType<MarkerVRTool>();

        if (markerToUnlock != null)
            markerToUnlock.SetPaintingEnabled(false);
    }

    void Update()
    {
        if (_complete)
        {
            _guide?.SetText("Rasurado", "Campo rasurado. Continúe con la demarcación de piel.");
            return;
        }

        if (!input.PrimaryHeld)
        {
            _guide?.SetText("Rasurado", tutorialText);
            return;
        }

        Transform origin = clipperTip != null ? clipperTip : transform;
        if (!Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, contactDistance + 0.03f, scalpLayers)
            && !Physics.SphereCast(origin.position, contactDistance, origin.forward, out hit, 0.01f, scalpLayers))
        {
            _guide?.SetText("Rasurado", "Acerque la máquina a la piel del cuero cabelludo.");
            return;
        }

        Vector2 uv = hit.textureCoord;
        PaintAtUv(uv);
        RegisterCell(uv);
        UpdateCoverage();
    }

    void PaintAtUv(Vector2 uv)
    {
        if (_paintMaterial == null || shaveTexture == null)
            return;

        _paintMaterial.SetVector("_UV", new Vector4(uv.x, uv.y, 0f, 0f));
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = shaveTexture;
        Graphics.Blit(null, shaveTexture, _paintMaterial);
        RenderTexture.active = prev;
    }

    void RegisterCell(Vector2 uv)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * coverageGrid), 0, coverageGrid - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * coverageGrid), 0, coverageGrid - 1);
        _shavedCells.Add(x + y * coverageGrid);
    }

    void UpdateCoverage()
    {
        int total = coverageGrid * coverageGrid;
        Coverage01 = total <= 0 ? 0f : _shavedCells.Count / (float)total;

        _guide?.SetText("Rasurado", $"Cobertura {Mathf.RoundToInt(Coverage01 * 100f)} %.\n{tutorialText}");

        if (Coverage01 < requiredCoverage)
            return;

        _complete = true;
        if (markerToUnlock != null)
            markerToUnlock.SetPaintingEnabled(true);
    }
}
