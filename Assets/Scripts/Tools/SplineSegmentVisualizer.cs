using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Splines;

public class SplineSegmentVisualizer : MonoBehaviour
{
    [Header("Visual")]
    public GameObject prefab;

    public GameObject markerArrow;
    public Material lineMaterial;
    public float lineWidth = 0.002f;

    public Color completedColor = new Color(0.3f, 1f, 0.8f);
    public Color activeColor = new Color(1f, 0.9f, 0.2f);
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

    DissectorMarker[] segments;
    
    public void Initialize(SplineContainer spline, int segmentCount)
    {
        segments = new DissectorMarker[segmentCount];

        Quaternion averageRotation = GetAverageKnotRotation(spline);
        
        for (int i = 0; i < segmentCount; i++)
        {
            GameObject go = Instantiate(prefab);//new GameObject($"Segment_{i}");
            go.name = $"Segment_{i}";
            go.transform.SetParent(transform, false);
            go.transform.rotation = averageRotation;
            
            /*LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            lr.alignment = LineAlignment.TransformZ;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.positionCount = 2;*/
            
            float t0 = (float)i / segmentCount;
            float t1 = (float)(i + 1) / segmentCount;

            Vector3 p0 = spline.EvaluatePosition(t0);
            Vector3 p1 = spline.EvaluatePosition(t1);

            go.transform.position = p0;

            segments[i] = go.GetComponent<DissectorMarker>();
            segments[i].SetBaseColor(lockedColor);
        }
        
        UpdateVisual(0);
        
    }

    public void ClearCurrentSegments()
    {
        foreach (var segment in segments)
        {
            Destroy(segment.gameObject);
        }
    }
    public void UpdateVisual(int activeSegment)
    {
        for (int i = 0; i < segments.Length; i++)
        {
            if (i < activeSegment)
                segments[i].SetBaseColor(completedColor);
            else if (i == activeSegment)
                segments[i].SetBaseColor(activeColor);
            else
                segments[i].SetBaseColor(lockedColor);
        }

        if (activeSegment < segments.Length)
        {
            markerArrow.transform.position = segments[activeSegment].transform.position + (Vector3.up*0.02f);
        }
        
    }
    
    private Quaternion GetAverageKnotRotation(SplineContainer container)
    {
        var spline = container.Spline;
        int knotCount = spline.Count;

        if (knotCount == 0) return Quaternion.identity;

        float x = 0, y = 0, z = 0, w = 0;

        foreach (var knot in spline)
        {
            // Obtenemos la rotación en el espacio del mundo
            Quaternion worldRot = container.transform.rotation * knot.Rotation;

            // Acumulamos los componentes (Asegurando que los cuaterniones no se cancelen)
            float dot = x * worldRot.x + y * worldRot.y + z * worldRot.z + w * worldRot.w;
            float multiplier = dot < 0 ? -1.0f : 1.0f;

            x += worldRot.x * multiplier;
            y += worldRot.y * multiplier;
            z += worldRot.z * multiplier;
            w += worldRot.w * multiplier;
        }

        // Normalizamos para obtener un cuaternión válido
        float k = 1.0f / Mathf.Sqrt(x * x + y * y + z * z + w * w);
        return new Quaternion(x * k, y * k, z * k, w * k);
    }
}
