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

    [Header("Superficie válida")]
    public XRGrabInteractable grab;
    public Collider[] surfaceColliders;
    public float contactDistance = 0.008f;

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
        if (grab == null || !grab.isSelected || !TryGetSurfacePoint(out Vector3 point))
        {
            if (isDrawing)
                StopDrawing();
            return;
        }

        isDrawing = true;
        RenderOnSurface(point);
    }

    bool TryGetSurfacePoint(out Vector3 point)
    {
        point = default;
        if (tip == null || surfaceColliders == null || surfaceColliders.Length == 0)
            return false;

        Vector3 axis = tip.position - transform.position;
        if (axis.sqrMagnitude < 0.0000001f)
            return false;
        axis.Normalize();

        const float behind = 0.002f;
        Vector3 origin = tip.position - axis * behind;
        RaycastHit[] hits = Physics.RaycastAll(origin, axis, behind + contactDistance, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        bool found = false;
        for (int i = 0; i < hits.Length; i++)
        {
            if (!IsValidSurface(hits[i].collider) || hits[i].distance >= best)
                continue;
            best = hits[i].distance;
            point = hits[i].point + hits[i].normal * 0.0004f;
            found = true;
        }

        return found;
    }

    bool IsValidSurface(Collider collider)
    {
        if (collider == null)
            return false;
        for (int i = 0; i < surfaceColliders.Length; i++)
        {
            if (surfaceColliders[i] == collider)
                return true;
        }

        return false;
    }

    void RenderOnSurface(Vector3 surfacePoint)
    {
        if (currentDrawing == null)
        {
            index = 0;
            currentDrawing = new GameObject("Trazo Fibrón").AddComponent<LineRenderer>();
            currentDrawing.material = drawingMaterial;
            currentDrawing.startColor = currentDrawing.endColor = penColors;
            currentDrawing.startWidth = currentDrawing.endWidth = penWidth;
            currentDrawing.positionCount = 1;
            currentDrawing.SetPosition(0, surfacePoint);
            return;
        }

        Vector3 currentPos = currentDrawing.GetPosition(index);
        float gap = Vector3.Distance(currentPos, surfacePoint);
        if (gap > 0.015f)
        {
            StopDrawing();
            isDrawing = true;
            RenderOnSurface(surfacePoint);
            return;
        }

        if (gap > 0.004f)
        {
            index++;
            currentDrawing.positionCount = index + 1;
            currentDrawing.SetPosition(index, surfacePoint);
        }
    }

    public void StartDrawing()
    { }

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
