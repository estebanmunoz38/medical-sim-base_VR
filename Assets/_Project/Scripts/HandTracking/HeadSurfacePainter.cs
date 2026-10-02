using PaintCore;
using PaintIn3D;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class HeadPaintHands
{
    public static float BrushRadius = 0.008f;

    public static void Ensure()
    {
        Renderer head = FindHead();
        if (head == null)
        {
            HandTrackingLog.Write("Paint", "No encontré la cabeza pintable (Asset corte y marcado).");
            return;
        }

        Bounds bounds = head.bounds;
        BrushRadius = Mathf.Clamp(bounds.size.magnitude * 0.08f, 0.003f, 0.02f);
        Vector3 side = head.transform.right.sqrMagnitude > 0.01f ? head.transform.right : Vector3.right;
        side.y = 0f;
        if (side.sqrMagnitude < 0.01f)
            side = Vector3.right;
        side.Normalize();

        float gap = Mathf.Max(bounds.extents.magnitude, 0.06f);
        Vector3 beside = bounds.center + side * (bounds.extents.x + gap) + Vector3.up * Mathf.Max(0.02f, bounds.extents.y * 0.2f);

        GameObject shaver = FindOrSpawn("Cortadora", "ShowableHands/Cortadora", beside);
        GameObject marker = FindExistingMarker();

        if (shaver != null)
            Prepare(shaver, shave: true);
        if (marker != null && marker.GetComponent<Draw>() == null)
            Prepare(marker, shave: false);

        HandTrackingLog.Write("Paint",
            $"Cabeza lista. Rasuradora={(shaver != null ? shaver.name : "ausente")}. Marcador={(marker != null ? marker.name : "ausente")}. Pincel={BrushRadius:0.000}m.");
    }

    static void Prepare(GameObject tool, bool shave)
    {
        var painter = tool.GetComponent<HeadSurfacePainter>();
        if (painter == null)
            painter = tool.AddComponent<HeadSurfacePainter>();
        painter.Configure(shave);
    }

    static GameObject FindExistingMarker()
    {
        XRGrabInteractable[] grabs = Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        GameObject fallback = null;
        for (int i = 0; i < grabs.Length; i++)
        {
            if (grabs[i].name.IndexOf("Marker Blue", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                grabs[i].gameObject.SetActive(true);
                return grabs[i].gameObject;
            }

            if (fallback == null && IsMarkerName(grabs[i].name))
                fallback = grabs[i].gameObject;
        }

        if (fallback != null)
        {
            fallback.SetActive(true);
            return fallback;
        }

        HandTrackingLog.Write("Paint", "No está el marcador original de la escena.");
        return null;
    }

    static bool IsMarkerName(string objectName)
    {
        return objectName.IndexOf("Fibron", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Fibrón", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Marcador", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Marker", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static GameObject FindOrSpawn(string nameToken, string resourcePath, Vector3 position)
    {
        XRGrabInteractable[] grabs = Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < grabs.Length; i++)
        {
            if (grabs[i].name.IndexOf(nameToken, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            grabs[i].gameObject.SetActive(true);
            return grabs[i].gameObject;
        }

        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name.IndexOf(nameToken, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            transforms[i].gameObject.SetActive(true);
            return transforms[i].gameObject;
        }

        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            HandTrackingLog.Write("Paint", $"Falta Resources/{resourcePath}.");
            return null;
        }

        GameObject instance = Object.Instantiate(prefab);
        instance.name = nameToken == "Cortadora" ? "Cortadora de pelo" : "Fibron";
        instance.transform.position = position;
        instance.SetActive(true);
        return instance;
    }

    static Renderer FindHead()
    {
        var paintables = Object.FindObjectsByType<CwPaintableMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Renderer best = null;
        float bestScore = -1f;
        for (int i = 0; i < paintables.Length; i++)
        {
            if (paintables[i] == null)
                continue;
            Renderer renderer = paintables[i].GetComponent<Renderer>();
            if (renderer == null)
                continue;

            string name = paintables[i].name.ToLowerInvariant();
            float score = 0f;
            if (name.Contains("cabeza"))
                score += 5f;
            if (name.Contains("craneo") || name.Contains("cráneo"))
                score += 3f;
            Transform cursor = paintables[i].transform;
            while (cursor != null)
            {
                if (cursor.name.ToLowerInvariant().Contains("corte"))
                    score += 2f;
                cursor = cursor.parent;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = renderer;
            }
        }

        return best;
    }
}

[DefaultExecutionOrder(21000)]
public class HeadSurfacePainter : MonoBehaviour
{
    XRGrabInteractable grab;
    CwHitBetween hit;
    CwPaintDecal decal;
    Transform pointA;
    Transform pointB;
    Collider[] ownColliders;
    Vector3 localA;
    Vector3 localB;
    bool hasLocalTip;
    bool shave;
    bool configured;

    public void Configure(bool shaveHair)
    {
        shave = shaveHair;
        if (configured)
            return;

        grab = GetComponent<XRGrabInteractable>();
        hit = GetComponentInChildren<CwHitBetween>(true);
        if (hit == null)
            BuildMarkerHit();

        if (hit == null)
            return;

        decal = hit.GetComponent<CwPaintDecal>();
        if (decal == null)
            decal = hit.gameObject.AddComponent<CwPaintDecal>();

        hit.Interval = -1f;
        hit.Preview = false;
        hit.Pressure = 1f;
        hit.Layers = ~0;
        hit.enabled = false;
        hit.ClearHitCache();

        if (shave)
            decal.Opacity = 0.85f;
        else
        {
            decal.Color = new Color(0.05f, 0.25f, 0.95f, 1f);
            decal.Opacity = 1f;
            decal.BlendMode = CwBlendMode.AlphaBlend(Vector4.one);
        }

        decal.Radius = HeadPaintHands.BrushRadius;
        pointA = hit.PointA;
        pointB = hit.PointB;
        if (shave && pointA != null && pointB != null)
        {
            localA = transform.InverseTransformPoint(pointA.position);
            localB = transform.InverseTransformPoint(pointB.position);
            hasLocalTip = true;
        }

        ownColliders = GetComponentsInChildren<Collider>(true);
        configured = true;
    }

    void BuildMarkerHit()
    {
        var paintObject = new GameObject("Paint");
        paintObject.transform.SetParent(transform, false);
        pointA = new GameObject("Point A").transform;
        pointB = new GameObject("Point B").transform;
        pointA.SetParent(paintObject.transform, false);
        pointB.SetParent(paintObject.transform, false);

        hit = paintObject.AddComponent<CwHitBetween>();
        hit.PointA = pointA;
        hit.PointB = pointB;
        decal = paintObject.AddComponent<CwPaintDecal>();
    }

    void LateUpdate()
    {
        if (!configured || hit == null || grab == null || !grab.isSelected)
            return;
        if (pointA == null || pointB == null)
            return;

        PlaceTipSegment();
        SetOwnColliders(false);
        try
        {
            hit.ManuallyHitNow();
        }
        finally
        {
            SetOwnColliders(true);
        }
    }

    void PlaceTipSegment()
    {
        Vector3 tip;
        Vector3 forward;
        if (shave && hasLocalTip)
        {
            Vector3 worldA = transform.TransformPoint(localA);
            Vector3 worldB = transform.TransformPoint(localB);
            tip = worldB;
            forward = worldB - worldA;
        }
        else
        {
            tip = transform.TransformPoint(new Vector3(0.12f, 0f, 0f));
            forward = transform.right;
        }

        if (forward.sqrMagnitude < 0.000001f)
            forward = transform.forward;
        forward.Normalize();

        pointA.position = tip - forward * 0.012f;
        pointB.position = tip + forward * 0.02f;
    }

    void SetOwnColliders(bool enabled)
    {
        if (ownColliders == null)
            return;
        for (int i = 0; i < ownColliders.Length; i++)
        {
            if (ownColliders[i] != null)
                ownColliders[i].enabled = enabled;
        }
    }
}
