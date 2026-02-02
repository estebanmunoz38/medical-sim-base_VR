using System;
using UnityEngine;

public class FontanellePainter : MonoBehaviour
{
    [Header("Mask")]
    public RenderTexture maskRT;
    public Material targetMaterial;

    [Header("Brush")]
    public Material brushMaterial;
    public float brushRadius = 0.04f;

    Camera paintCam;

    void Awake()
    {
        //brushMaterial.shader = Shader.Find("Hidden/FontanelleBrush");
    }

    private void Start()
    {
        Graphics.SetRenderTarget(maskRT);
        GL.Clear(true, true, Color.black);
        Graphics.SetRenderTarget(null);
    }

    public void Paint(Vector2 uv, float strength)
    {
        if (!maskRT || !brushMaterial)
            return;

        brushMaterial.SetVector("_BrushUV", new Vector4(uv.x, uv.y, 0, 0));
        brushMaterial.SetFloat("_BrushRadius", brushRadius);
        brushMaterial.SetFloat("_BrushStrength", strength);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = maskRT;

        GL.PushMatrix();
        GL.LoadOrtho();

        brushMaterial.SetPass(0);

        GL.Begin(GL.QUADS);
        GL.TexCoord2(0, 0); GL.Vertex3(0, 0, 0);
        GL.TexCoord2(1, 0); GL.Vertex3(1, 0, 0);
        GL.TexCoord2(1, 1); GL.Vertex3(1, 1, 0);
        GL.TexCoord2(0, 1); GL.Vertex3(0, 1, 0);
        GL.End();

        GL.PopMatrix();
        RenderTexture.active = prev;
    }
}