using UnityEngine;

public class MarkerVRTool : MonoBehaviour
{
    [Header("Input VR")]
    public MonoBehaviour inputSourceBehaviour;      
    private IToolInputSource input;

    [Header("Punta / Raycast")]
    public float maxDistance = 0.5f;
    public LayerMask paintLayers = -1;

    [Header("Textura donde pintamos")]
    public RenderTexture paintTexture;
    public Material painterMaterial;

    [Header("Pincel")]
    public Color paintColor = Color.blue;
    public float brushSize = 0.03f;

    private Camera cam;


    void Start()
    {
        input = inputSourceBehaviour as IToolInputSource;

        if (input == null)
        {
            Debug.LogError("❌ MarkerVRTool: inputSourceBehaviour NO implementa IToolInputSource.");
            enabled = false;
            return;
        }

        if (painterMaterial == null || paintTexture == null)
        {
            Debug.LogError("❌ MarkerVRTool: falta asignar paintTexture o painterMaterial.");
            enabled = false;
            return;
        }

        painterMaterial.SetTexture("_PaintTex", paintTexture);
    }


    void Update()
    {
        if (!input.PrimaryHeld) 
            return;

        DoPaint();
    }


    void DoPaint()
    {
        Ray r = input.PointerRay;
        RaycastHit hit;

        if (Physics.Raycast(r, out hit, maxDistance, paintLayers))
        {
            Vector2 uv;
            Renderer rend = hit.collider.GetComponent<Renderer>();

            if (rend != null && rend.material.mainTexture != null)
            {
                if (TryGetUV(rend, hit.point, out uv))
                {
                    PaintAtUV(uv);
                }
            }
        }
    }


    // ======================================================================
    // Obtención de UV — necesario para saber dónde pintar
    // ======================================================================
    bool TryGetUV(Renderer rend, Vector3 worldPos, out Vector2 uv)
    {
        Ray ray = new Ray(worldPos + (Vector3.up * 0.001f), -Vector3.up);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 0.01f, paintLayers))
        {
            uv = hit.textureCoord;
            return true;
        }

        uv = Vector2.zero;
        return false;
    }


    // ======================================================================
    // PINTAR EN LA TEXTURA
    // ======================================================================
    void PaintAtUV(Vector2 uv)
    {
        painterMaterial.SetVector("_UV", new Vector4(uv.x, uv.y, brushSize, 0f));
        painterMaterial.SetColor("_Color", paintColor);

        // Aplica el dibujo a la textura
        Graphics.Blit(null, paintTexture, painterMaterial);
    }
}
