using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Estabiliza el agarre con manos: volúmenes de mango modestos, sin throw,
/// kinematic al soltar y recuperación si la herramienta cae fuera de alcance.
/// No infla las zonas: el radio extra está acotado.
/// </summary>
[DefaultExecutionOrder(-40)]
public class HandGrabStability : MonoBehaviour
{
    const float MinGrabRadius = 0.038f;
    const float MaxGrabRadius = 0.055f;
    const float DropRecoverDelay = 1.6f;
    const float FarFromHome = 1.35f;
    const float FloorMargin = 0.35f;

    class TrackedTool
    {
        public XRGrabInteractable grab;
        public Rigidbody body;
        public Vector3 homePos;
        public Quaternion homeRot;
        public Transform parent;
        public float lostTimer;
        public bool recovering;
        public Vector3 recoverFrom;
        public Quaternion recoverFromRot;
        public float recoverT;
    }

    readonly System.Collections.Generic.List<TrackedTool> _tools = new System.Collections.Generic.List<TrackedTool>();
    float _floorY = float.NaN;

    void Start()
    {
        HardenAll();
    }

    void LateUpdate()
    {
        if (_tools.Count == 0) return;
        float dt = Time.deltaTime;
        for (int i = 0; i < _tools.Count; i++)
            TickTool(_tools[i], dt);
    }

    [ContextMenu("Harden Grabbables Now")]
    public void HardenAll()
    {
        _tools.Clear();
        InferFloor();
        var grabs = FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < grabs.Length; i++)
            Harden(grabs[i]);
    }

    void Harden(XRGrabInteractable grab)
    {
        if (grab == null) return;
        if (HandXriGrabAdapter.IsScreenOrUi(grab.transform))
            return;
        if (grab.GetComponentInParent<MechanicalCeilingArm>() != null
            || grab.GetComponentInParent<CeilingArm2DOF_HandleDriven>() != null
            || grab.GetComponent<XRIArmIKFollow>() != null)
            return;

        grab.throwOnDetach = false;
        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.useDynamicAttach = !HandXriGrabAdapter.HasDesignedAttach(grab);

        var rb = grab.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        if (!HandXriGrabAdapter.HasDesignedAttach(grab)
            && (grab.colliders == null || grab.colliders.Count == 0))
            EnsureGrabVolume(grab);

        _tools.Add(new TrackedTool
        {
            grab = grab,
            body = rb,
            homePos = grab.transform.position,
            homeRot = grab.transform.rotation,
            parent = grab.transform.parent
        });
    }

    static void EnsureGrabVolume(XRGrabInteractable grab)
    {
        if (grab.transform.Find("HandGrabVolume") != null)
            return;

        var cols = grab.GetComponentsInChildren<Collider>(true);
        float maxExtent = 0f;
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null || !cols[i].enabled) continue;
            Vector3 e = cols[i].bounds.extents;
            maxExtent = Mathf.Max(maxExtent, e.x, e.y, e.z);
        }

        if (maxExtent >= MinGrabRadius)
            return;

        float radius = Mathf.Clamp(MinGrabRadius, MinGrabRadius, MaxGrabRadius);
        var vol = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vol.name = "HandGrabVolume";
        Transform pivot = HandXriGrabAdapter.FindGrabPoint(grab.transform);
        vol.transform.SetParent(pivot != null ? pivot : grab.transform, false);
        vol.transform.localPosition = Vector3.zero;
        vol.transform.localScale = Vector3.one * (radius * 2f);
        Object.Destroy(vol.GetComponent<MeshRenderer>());
        var col = vol.GetComponent<SphereCollider>();
        col.isTrigger = false;
        col.radius = 0.5f;
    }

    void InferFloor()
    {
        var table = GameObject.Find("Mesa de operaciones");
        if (table != null)
            _floorY = table.transform.position.y - FloorMargin;
        else if (Camera.main != null)
            _floorY = Camera.main.transform.position.y - 1.4f;
        else
            _floorY = 0f;
    }

    void TickTool(TrackedTool tool, float dt)
    {
        if (tool.grab == null) return;

        if (tool.grab.isSelected)
        {
            tool.lostTimer = 0f;
            tool.recovering = false;
            return;
        }

        Vector3 pos = tool.grab.transform.position;
        bool far = Vector3.Distance(pos, tool.homePos) > FarFromHome;
        bool below = !float.IsNaN(_floorY) && pos.y < _floorY;
        if (far || below)
            tool.lostTimer += dt;
        else
            tool.lostTimer = 0f;

        if (!tool.recovering && tool.lostTimer >= DropRecoverDelay)
        {
            tool.recovering = true;
            tool.recoverT = 0f;
            tool.recoverFrom = pos;
            tool.recoverFromRot = tool.grab.transform.rotation;
            if (tool.body != null)
            {
                tool.body.linearVelocity = Vector3.zero;
                tool.body.angularVelocity = Vector3.zero;
                tool.body.isKinematic = true;
            }
        }

        if (!tool.recovering) return;

        tool.recoverT += dt / 0.45f;
        float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tool.recoverT));
        tool.grab.transform.position = Vector3.Lerp(tool.recoverFrom, tool.homePos, u);
        tool.grab.transform.rotation = Quaternion.Slerp(tool.recoverFromRot, tool.homeRot, u);
        if (u >= 1f)
        {
            tool.recovering = false;
            tool.lostTimer = 0f;
            if (tool.body != null)
            {
                tool.body.linearVelocity = Vector3.zero;
                tool.body.angularVelocity = Vector3.zero;
                tool.body.isKinematic = false;
            }
        }
    }
}
