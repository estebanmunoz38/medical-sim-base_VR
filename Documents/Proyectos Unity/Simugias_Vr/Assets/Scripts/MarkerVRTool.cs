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

    private Material paintMaterial;

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

        // Crear material para pintar
        paintMaterial = new Material(Shader.Find("Hidden/MarkerPainter"));
        paintMaterial.SetColor("_Color", brushColor);
        paintMaterial.SetFloat("_Size", brushSize);

        // Asignar RenderTexture al material de la piel
        targetRenderer.material.SetTexture("_PaintTex", paintTexture);
    }

    void Update()
    {
        // No pintamos nada si el gatillo NO está apretado
        if (!input.PrimaryHeld) return;

        Ray ray = input.PointerRay;

        if (Physics.Raycast(ray, out RaycastHit hit, 1.5f, paintLayers))
        {
            Vector2 uv = hit.textureCoord;

            // Pintar en textura usando Blit
            PaintAtUV(uv);
        }
    }

    void PaintAtUV(Vector2 uv)
    {
        // Set uniform values
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
