using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

    public float Depth01 => t;
    public bool IsActive => active;

    [Header("Control (predecible)")]
    [Tooltip("Gatillo primario avanza; secundario retrocede. Scroll queda como respaldo.")]
    public float holdAdvanceSpeed = 0.18f;
    public bool keepViewOnScreen = true;

    [Header("Tutorial")]
    public bool showTutorial = true;
    [TextArea] public string tutorialText =
        "Introduzca el endoscopio por el acceso. Mantenga el gatillo para avanzar a lo largo del canal. Gatillo secundario para retirar. La imagen permanece en el monitor. Oriente la óptica; no extraiga hueso con esta herramienta.";

    [Header("Detección de objetos")]
    public float detectionRadius = 0.25f;
    public LayerMask detectionLayers = -1;
    public Material highlightMaterial;
    public GameObject gripperObj;
    [Tooltip("Desactivado: la extracción de hueso la hace el Kerrison, no el endoscopio.")]
    public bool allowGrabFromView = false;

    private List<Renderer> detected = new List<Renderer>();
    private Dictionary<Renderer, Material[]> originalMats = new Dictionary<Renderer, Material[]>();
    SurgicalGuideBeacon _guide;


    // =========================================================================
    // INIT VR
    // =========================================================================
    void Start()
    {
        input = inputSourceBehaviour as IToolInputSource;
        if (input == null)
            input = FindFirstObjectByType<CompositeToolInputSource>();

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

        if (showTutorial)
        {
            if (HandsOnlySession.Active)
                tutorialText = "Introducí el endoscopio por el acceso. Pellizco = avanzar. Pellizco con la otra mano = retirar. La imagen queda en el monitor.";
            Transform guideAnchor = endoscopeScreen != null ? endoscopeScreen.transform : transform;
            _guide = SurgicalGuideBeacon.Attach(
                guideAnchor,
                "Endoscopio",
                tutorialText,
                new Vector3(0f, 0.12f, 0f));
            if (_guide != null)
                _guide.SetVisible(false);
        }
    }


    // =========================================================================
    // ACTIVAR / DESACTIVAR
    // =========================================================================
    public void ActivateVR()
    {
        active = true;
        t = 0f;

        endoscopeCamera.gameObject.SetActive(true);
        if (endoscopeScreen != null)
            endoscopeScreen.SetActive(true);

        if (_guide != null)
            _guide.SetVisible(true);

        MoveCameraImmediate();
    }

    public void DeactivateVR()
    {
        active = false;

        endoscopeCamera.gameObject.SetActive(keepViewOnScreen);
        if (endoscopeScreen != null)
            endoscopeScreen.SetActive(keepViewOnScreen);

        if (_guide != null)
            _guide.SetVisible(false);

        ClearHighlights();
    }


    // =========================================================================
    // UPDATE VR
    // =========================================================================
    void Update()
    {
        if (!active)
            return;

        if (input == null)
            input = FindFirstObjectByType<CompositeToolInputSource>();
        if (input == null)
            return;

        HandleMovement();
        DetectObjects();
        UpdateTutorial();

        if (allowGrabFromView && input.PrimaryDown)
            GrabNearest();
    }


    // =========================================================================
    // MOVIMIENTO A LO LARGO DEL PATH
    // =========================================================================
    void HandleMovement()
    {
        if (TryProjectHeldHand())
            return;

        float axis = 0f;
        if (input.PrimaryHeld) axis += 1f;
        if (input.SecondaryHeld) axis -= 1f;
        axis += input.ScrollDelta;

        if (Mathf.Abs(axis) < 0.001f)
            return;

        t = Mathf.Clamp01(t + axis * holdAdvanceSpeed * Time.deltaTime);
        MoveCamera();
    }

    bool TryProjectHeldHand()
    {
        var grab = GetComponent<XRGrabInteractable>();
        if (grab == null)
            grab = GetComponentInParent<XRGrabInteractable>();
        if (grab == null || !grab.isSelected || fusedPath.Count < 2)
            return false;

        Transform hand = grab.interactorsSelecting.Count > 0
            ? grab.interactorsSelecting[0].transform
            : grab.transform;

        float best = t;
        float bestDist = float.MaxValue;
        int samples = fusedPath.Count;
        for (int i = 0; i < samples; i++)
        {
            float d = (fusedPath[i] - hand.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = samples <= 1 ? 0f : i / (float)(samples - 1);
            }
        }

        t = Mathf.MoveTowards(t, best, 1.8f * Time.deltaTime);
        MoveCamera();

        Vector3 pos = GetPathPosition(t);
        Vector3 dir = GetPathDirection(t);
        Vector3 up = hand.up;
        if (dir.sqrMagnitude < 0.0001f)
            dir = grab.transform.forward;
        if (Mathf.Abs(Vector3.Dot(dir.normalized, up.normalized)) > 0.96f)
            up = hand.right;
        grab.transform.position = pos;
        grab.transform.rotation = Quaternion.Slerp(
            grab.transform.rotation,
            Quaternion.LookRotation(dir, up),
            1f - Mathf.Exp(-18f * Time.deltaTime));
        return true;
    }

    void UpdateTutorial()
    {
        if (_guide == null)
            return;

        int depthPct = Mathf.RoundToInt(t * 100f);
        string depth = depthPct <= 2
            ? (HandsOnlySession.Active ? "En el acceso. Mantené el pellizco para introducir la óptica." : "En el acceso. Mantenga gatillo para introducir la óptica.")
            : depthPct >= 98
                ? (HandsOnlySession.Active ? "Profundidad máxima. Pellizco con la otra mano para retirar." : "Profundidad máxima. Gatillo secundario para retirar.")
                : (HandsOnlySession.Active
                    ? $"Profundidad {depthPct} %. Pellizco: avanzar. Otra mano: retirar. Imagen fija en el monitor."
                    : $"Profundidad {depthPct} %. Gatillo: avanzar. Secundario: retirar. Imagen fija en el monitor.");

        _guide.SetText("Endoscopio", depth + "\n" + tutorialText);
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
        if (highlightMaterial == null || r == null)
            return;

        if (!originalMats.ContainsKey(r))
            originalMats[r] = r.sharedMaterials;

        Material[] m = new Material[r.materials.Length];
        for (int i = 0; i < m.Length; i++)
            m[i] = highlightMaterial;

        r.sharedMaterials = m;
    }

    void ClearHighlights()
    {
        foreach (var kv in originalMats)
        {
            if (kv.Key != null)
                kv.Key.sharedMaterials = kv.Value;
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
