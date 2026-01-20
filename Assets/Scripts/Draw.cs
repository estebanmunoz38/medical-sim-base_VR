using UnityEngine;

public class Draw : SurgicalTool
{
    [Header("Pen Properties")]
    public Transform tip;
    public Material drawingMaterial;
    public Material tipMaterial;
    public float penWidth = 0.005f;
    public Color penColors;

    [Header("Drawing Control")]
    public bool isDrawing = false;
    [Range(0.1f,1f)] public float minPressure = .5f;

    private LineRenderer currentDrawing;
    private int index;
    private int currentColorIndex;

    void Start()
    { Init(); }

    private void Init()
    {
        currentColorIndex = 0;
        tipMaterial.color = penColors;
    }

    void Update()
    {
        if (activeGestures == null)
            return;
        

        if (activeGestures.IsPinching &&
            activeGestures.Pressure > minPressure)
        {
            RenderDrawing();
        }
        else
        {
            StopDrawing();
        }
    }

    public void RenderDrawing()
    {
        if (!isDrawing) isDrawing = true;
        
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