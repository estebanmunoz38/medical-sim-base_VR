using UnityEngine;
using UnityEngine.Splines;

public class SplineSegmentVisualizer : MonoBehaviour
{
    [Header("Spline")]
    public SplineContainer spline;
    public int segmentCount = 14;

    [Header("Visual")]
    public Material lineMaterial;
    public float lineWidth = 0.002f;

    public Color completedColor = new Color(0.3f, 1f, 0.8f);
    public Color activeColor = new Color(1f, 0.9f, 0.2f);
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);

    LineRenderer[] segments;

    void Awake()
    {
        BuildSegments();
    }

    void BuildSegments()
    {
        segments = new LineRenderer[segmentCount];

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject go = new GameObject($"Segment_{i}");
            go.transform.SetParent(transform, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.positionCount = 2;

            float t0 = (float)i / segmentCount;
            float t1 = (float)(i + 1) / segmentCount;

            Vector3 p0 = spline.EvaluatePosition(t0);
            Vector3 p1 = spline.EvaluatePosition(t1);

            lr.SetPosition(0, p0);
            lr.SetPosition(1, p1);

            segments[i] = lr;
        }
    }

    public void UpdateVisual(int activeSegment)
    {
        for (int i = 0; i < segments.Length; i++)
        {
            if (i < activeSegment)
                segments[i].material.color = completedColor;
            else if (i == activeSegment)
                segments[i].material.color = activeColor;
            else
                segments[i].material.color = lockedColor;
        }
    }
}
