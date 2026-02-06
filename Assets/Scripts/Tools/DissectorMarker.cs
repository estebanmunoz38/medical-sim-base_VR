using UnityEngine;

public class DissectorMarker : MonoBehaviour
{
    private Outline _outline;
    private MeshRenderer _meshRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _outline = GetComponent<Outline>();
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SetActiveMarker()
    {
        
    }
    
    public void SetBaseColor(Color color)
    {
        _meshRenderer.material.color = color;
        _outline.OutlineColor = color;
    }
}
