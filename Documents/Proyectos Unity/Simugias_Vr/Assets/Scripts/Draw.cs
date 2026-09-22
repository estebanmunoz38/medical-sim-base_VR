using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Draw : MonoBehaviour
{
    [Header("Pen Properties")]
    public Transform tip;
    public Material drawingMaterial;
    public Material tipMaterial;
    public float penWidth = 0.005f;
    public Color penColors;

    [Header("Drawing Control")]
    public bool isDrawing = false;

    private LineRenderer currentDrawing;
    private int index;
    private int currentColorIndex;

    /// <summary>True si el usuario dibujó al menos un trazo usable.</summary>
    public bool HasPainted { get; private set; }

    XRGrabInteractable _grab;

    void Start()
    { Init(); }

    private void Init()
    {
        currentColorIndex = 0;
        _grab = GetComponent<XRGrabInteractable>();
        if (tipMaterial != null)
            tipMaterial.color = penColors;
        if (drawingMaterial == null || drawingMaterial.shader == null
            || drawingMaterial.shader.name.IndexOf("Error", System.StringComparison.OrdinalIgnoreCase) >= 0
            || drawingMaterial.shader.name == "Standard")
        {
            Shader urp = Shader.Find("Universal Render Pipeline/Unlit");
            if (urp != null)
            {
                drawingMaterial = new Material(urp);
                drawingMaterial.color = penColors;
            }
        }
    }

    void Update()
    {
        if (_grab != null && _grab.isSelected && PrimaryHeld())
            isDrawing = true;
        else if (_grab != null && _grab.isSelected && !PrimaryHeld())
            StopDrawing();

        if (isDrawing)
        { RenderDrawing(); }
    }

    static bool PrimaryHeld()
    {
        var composite = Object.FindFirstObjectByType<CompositeToolInputSource>();
        if (composite != null && composite.PrimaryHeld)
            return true;
        var hands = Object.FindFirstObjectByType<HandPinchToolInput>();
        return hands != null && hands.PrimaryHeld;
    }

    public void RenderDrawing()
    {
        if (tip == null)
            return;

        if (currentDrawing == null)
        {
            index = 0;
            currentDrawing = new GameObject().AddComponent<LineRenderer>();
            currentDrawing.material = drawingMaterial;
            currentDrawing.startColor = currentDrawing.endColor = penColors;
            currentDrawing.startWidth = currentDrawing.endWidth = penWidth;
            currentDrawing.positionCount = 1;
            currentDrawing.SetPosition(0, tip.position);
        }
        else
        {
            var currentPos = currentDrawing.GetPosition(index);
            if (Vector3.Distance(currentPos, tip.position) > 0.01f)
            {
                index++;
                currentDrawing.positionCount = index + 1;
                currentDrawing.SetPosition(index, tip.position);
                if (index >= 2)
                    HasPainted = true;
            }
        }
    }

    public void StartDrawing()
    { isDrawing = true; }

    public void StopDrawing()
    {
        isDrawing = false;
        currentDrawing = null;
    }

    public void ClearDrawing()
    {
        if (currentDrawing != null)
        {
            Destroy(currentDrawing.gameObject);
            currentDrawing = null;
        }
    }
}