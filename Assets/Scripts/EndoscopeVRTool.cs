using UnityEngine;
using System.Collections.Generic;

public class EndoscopeVRTool : MonoBehaviour
{
    [Header("Input VR")]
    public MonoBehaviour inputSourceBehaviour;      
    private IToolInputSource input;

    [Header("Cámara del endoscopio")]
    public Camera endoscopeCamera;
    public GameObject endoscopeScreen;

    [Header("Trayectoria / EndoPath")]
    public Transform[] waypoints;
    public bool useCurvedPath = true;
    public float curveSmoothness = 1f;
    public float pathSpeed = 0.7f;

    private List<Vector3> fusedPath = new List<Vector3>();
    private float t = 0f;
    private bool active = false;

    [Header("Detección de objetos")]
    public float detectionRadius = 0.25f;
    public LayerMask detectionLayers = -1;
    public Material highlightMaterial;
    public GameObject gripperObj;

    private List<Renderer> detected = new List<Renderer>();
    private Dictionary<Renderer, Material[]> originalMats = new Dictionary<Renderer, Material[]>();


    // =========================================================================
    // INIT VR
    // =========================================================================
    void Start()
    {
        input = inputSourceBehaviour as IToolInputSource;

        if (input == null)
        {
            Debug.LogError("❌ EndoscopeVRTool → inputSourceBehaviour NO implementa IToolInputSource.");
            enabled = false;
            return;
        }

        if (endoscopeCamera == null)
        {
            Debug.LogError("❌ EndoscopeVRTool → NO hay cámara asignada.");
            enabled = false;
            return;
        }

        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogError("❌ EndoscopeVRTool → Debés asignar al menos 2 waypoints.");
            enabled = false;
            return;
        }

        BuildPath();

        endoscopeCamera.gameObject.SetActive(false);
        if (endoscopeScreen != null) endoscopeScreen.SetActive(false);
    }


    // =========================================================================
    // ACTIVAR / DESACTIVAR
    // =========================================================================
    public void ActivateVR()
    {
        active = true;
        t = 0f;

        endoscopeCamera.gameObject.SetActive(true);
        if (endoscopeScreen != null) endoscopeScreen.SetActive(true);

        MoveCameraImmediate();
    }

    public void DeactivateVR()
    {
        active = false;

        endoscopeCamera.gameObject.SetActive(false);
        if (endoscopeScreen != null) endoscopeScreen.SetActive(false);

        ClearHighlights();
    }


    // =========================================================================
    // UPDATE VR
    // =========================================================================
    void Update()
    {
        if (!active)
            return;

        HandleMovement();
        DetectObjects();

        if (input.PrimaryDown)
            GrabNearest();
    }


    // =========================================================================
    // MOVIMIENTO A LO LARGO DEL PATH
    // =========================================================================
    void HandleMovement()
    {
        float scroll = input.ScrollDelta;

        if (Mathf.Abs(scroll) > 0.001f)
        {
            t = Mathf.Clamp01(t + scroll * pathSpeed * Time.deltaTime);
            MoveCamera();
        }
    }

    void MoveCameraImmediate()
    {
        Vector3 pos = GetPathPosition(t);
        Vector3 dir = GetPathDirection(t);

        endoscopeCamera.transform.position = pos;
        if (dir.sqrMagnitude > 0.0001f)
            endoscopeCamera.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    void MoveCamera()
    {
        Vector3 pos = GetPathPosition(t);
        endoscopeCamera.transform.position = pos;

        Vector3 dir = GetPathDirection(t);
        if (dir.sqrMagnitude > 0.0001f)
            endoscopeCamera.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }


    // =========================================================================
    // CONSTRUCCIÓN DEL PATH
    // =========================================================================
    void BuildPath()
    {
        fusedPath.Clear();

        if (!useCurvedPath)
        {
            foreach (var w in waypoints)
                fusedPath.Add(w.position);

            return;
        }

        int segs = Mathf.RoundToInt(waypoints.Length * 15 * curveSmoothness);
        segs = Mathf.Max(segs, waypoints.Length);

        for (int i = 0; i <= segs; i++)
        {
            float tt = i / (float)segs;
            fusedPath.Add(GetCurvePoint(tt));
        }
    }

    Vector3 GetCurvePoint(float t)
    {
        float count = waypoints.Length - 1;

        float scaled = t * count;
        int i = Mathf.FloorToInt(scaled);
        float localT = scaled - i;

        Vector3 p0 = waypoints[Mathf.Clamp(i - 1, 0, waypoints.Length - 1)].position;
        Vector3 p1 = waypoints[i].position;
        Vector3 p2 = waypoints[Mathf.Clamp(i + 1, 0, waypoints.Length - 1)].position;
        Vector3 p3 = waypoints[Mathf.Clamp(i + 2, 0, waypoints.Length - 1)].position;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * localT +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * (localT * localT) +
            (-p0 + 3f * p1 - 3f * p2 + p3) * (localT * localT * localT)
        );
    }


    Vector3 GetPathPosition(float t)
    {
        float scaled = t * (fusedPath.Count - 1);
        int index = Mathf.FloorToInt(scaled);
        int next = Mathf.Clamp(index + 1, 0, fusedPath.Count - 1);

        float localT = scaled - index;
        return Vector3.Lerp(fusedPath[index], fusedPath[next], localT);
    }

    Vector3 GetPathDirection(float t)
    {
        float scaled = t * (fusedPath.Count - 1);
        int index = Mathf.FloorToInt(scaled);
        int next = Mathf.Clamp(index + 1, 0, fusedPath.Count - 1);

        return (fusedPath[next] - fusedPath[index]).normalized;
    }


    // =========================================================================
    // DETECCIÓN DE OBJETOS
    // =========================================================================
    void DetectObjects()
    {
        ClearHighlights();
        detected.Clear();

        Collider[] hits = Physics.OverlapSphere(
            endoscopeCamera.transform.position,
            detectionRadius,
            detectionLayers
        );

        foreach (var hit in hits)
        {
            Renderer r = hit.GetComponent<Renderer>();
            if (r == null) continue;

            detected.Add(r);
            Highlight(r);
        }
    }

    void Highlight(Renderer r)
    {
        if (!originalMats.ContainsKey(r))
            originalMats[r] = r.materials;

        Material[] m = new Material[r.materials.Length];
        for (int i = 0; i < m.Length; i++)
            m[i] = highlightMaterial;

        r.materials = m;
    }

    void ClearHighlights()
    {
        foreach (var kv in originalMats)
        {
            if (kv.Key != null)
                kv.Key.materials = kv.Value;
        }

        originalMats.Clear();
    }


    // =========================================================================
    // AGARRAR OBJETO MÁS CERCANO
    // =========================================================================
    void GrabNearest()
    {
        if (gripperObj == null)
            return;

        if (detected.Count == 0)
            return;

        Renderer nearest = null;
        float best = float.MaxValue;
        Vector3 origin = endoscopeCamera.transform.position;

        foreach (var r in detected)
        {
            float d = Vector3.Distance(origin, r.transform.position);
            if (d < best)
            {
                best = d;
                nearest = r;
            }
        }

        if (nearest != null)
        {
            nearest.transform.SetParent(gripperObj.transform);
        }
    }


    // =========================================================================
    // DEBUG GIZMOS
    // =========================================================================
    void OnDrawGizmosSelected()
    {
        if (endoscopeCamera == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(endoscopeCamera.transform.position, detectionRadius);
    }
}
