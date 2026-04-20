using System.Collections.Generic;
using UnityEngine;

public class MarkerVRTool : MonoBehaviour
{
    [Header("➡ Entrada VR (botón / dirección / posición)")]
    public XRITToolInputSource inputSourceBehaviour;

    [Header("➡ Renderers del bebé (VARIOS)")]
    public List<Renderer> targetRenderers = new List<Renderer>();

    [Header("➡ RenderTexture donde se dibuja")]
    public RenderTexture paintTexture;

    [Header("➡ Capas que aceptan pintura")]
    public LayerMask paintLayers;

    [Header("➡ Grosor del trazo")]
    public float brushSize = 0.01f;

    private void Start()
    {
        // Asignar la textura de pintura a TODOS los materiales
        foreach (var rend in targetRenderers)
        {
            if (rend != null && rend.material != null)
            {
                rend.material.SetTexture("_MaskTex", paintTexture);
            }
        }
    }

    private void Update()
    {
        if (inputSourceBehaviour == null || inputSourceBehaviour.pointerOrigin == null)
            return;

        // SOLO si el botón está presionado
        if (inputSourceBehaviour.IsPrimaryPressed())
        {
            TryPaint();
        }
    }

    private void TryPaint()
    {
        Ray ray = new Ray(inputSourceBehaviour.pointerOrigin.position,
                          inputSourceBehaviour.pointerOrigin.forward);

        // Raycast contra cualquiera de las capas permitidas
        if (Physics.Raycast(ray, out RaycastHit hit, 0.2f, paintLayers))
        {
            PaintAtUV(hit.textureCoord);
        }
    }

    private void PaintAtUV(Vector2 uv)
    {
        if (paintTexture == null) return;

        RenderTexture.active = paintTexture;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, paintTexture.width, paintTexture.height, 0);

        Texture2D brush = Texture2D.whiteTexture;

        float sizeX = brushSize * paintTexture.width;
        float sizeY = brushSize * paintTexture.height;

        float x = uv.x * paintTexture.width - sizeX * 0.5f;
        float y = (1 - uv.y) * paintTexture.height - sizeY * 0.5f;

        Graphics.DrawTexture(new Rect(x, y, sizeX, sizeY), brush);

        GL.PopMatrix();

        RenderTexture.active = null;
    }
}
