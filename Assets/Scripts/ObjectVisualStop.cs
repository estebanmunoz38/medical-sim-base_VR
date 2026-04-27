using UnityEngine;

public class ObjectVisualStop : MonoBehaviour
{
    [Header("Raycast Points")]
    public Transform pointA; // inicio del raycast
    public Transform pointB; // punta real

    [Header("Visual Object")]
    public Transform visualRoot;      // objeto visual que se va a mover
    public Transform visualTipAnchor; // punta del visualRoot

    [Header("Detection")]
    public LayerMask layers = Physics.DefaultRaycastLayers;
    public float offset = 0.002f;

    [Header("Debug Optional")]
    public Transform debugPoint;
    public LineRenderer debugLine;

    private float fraction = 1.0f;

    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    private void Awake()
    {
        if (visualRoot != null)
        {
            initialLocalPosition = visualRoot.localPosition;
            initialLocalRotation = visualRoot.localRotation;
        }
    }

    private void LateUpdate()
    {
        SubmitHit();
        UpdateVisualPoint();
    }

    private void SubmitHit()
    {
        if (pointA == null || pointB == null)
            return;

        Vector3 vector = pointB.position - pointA.position;
        float maxDistance = vector.magnitude;

        if (maxDistance <= 0.0001f)
        {
            fraction = 1.0f;
            return;
        }

        Ray ray = new Ray(pointA.position, vector);

        if (Physics.Raycast(ray, out RaycastHit hit3D, maxDistance, layers, QueryTriggerInteraction.Ignore))
        {
            fraction = Mathf.Clamp01((hit3D.distance + offset) / maxDistance);
        }
        else
        {
            fraction = 1.0f;
        }
    }

    private void UpdateVisualPoint()
    {
        if (pointA == null || pointB == null || visualRoot == null || visualTipAnchor == null)
            return;

        Vector3 a = pointA.position;
        Vector3 b = pointB.position;

        Vector3 clampedTipPosition = Vector3.Lerp(a, b, fraction);

        // Volvemos el visual a su pose normal antes de corregirlo
        visualRoot.localPosition = initialLocalPosition;
        visualRoot.localRotation = initialLocalRotation;

        // Calculamos cuánto hay que mover el visual para que su punta quede en el punto frenado
        Vector3 correction = clampedTipPosition - visualTipAnchor.position;

        visualRoot.position += correction;

        if (debugPoint != null)
        {
            debugPoint.position = clampedTipPosition;
        }

        if (debugLine != null)
        {
            debugLine.positionCount = 2;
            debugLine.SetPosition(0, a);
            debugLine.SetPosition(1, clampedTipPosition);
        }
    }

    [ContextMenu("Reset Visual")]
    public void ResetVisual()
    {
        if (visualRoot != null)
        {
            visualRoot.localPosition = initialLocalPosition;
            visualRoot.localRotation = initialLocalRotation;
        }

        fraction = 1.0f;
    }
}