using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CeilingArm2DOF_HandleDriven : MonoBehaviour
{
    [Header("Assign these in Inspector (MANDATORY)")]
    [Tooltip("Pivot del monitor (tu LabDisplayGeo). NO debe tener XRGrabInteractable si querés solo manijas.")]
    public Transform monitorPivot;

    [Tooltip("Joint que mueve ARRIBA/ABAJO (tu Arm02Geo).")]
    public Transform pitchJoint;

    [Tooltip("Joint que rota TODO 360 (tu Skin01Bone).")]
    public Transform yawJoint;

    [Header("Grab Handles (ONLY these are grabbable)")]
    [Tooltip("Interactable de la manija izquierda (XRSimpleInteractable recomendado).")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable leftHandle;

    [Tooltip("Interactable de la manija derecha (XRSimpleInteractable recomendado).")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable rightHandle;

    [Header("Mechanical Limits")]
    [Tooltip("Distancia máxima permitida desde el yawJoint al target. Evita 'estirar infinito'.")]
    public float maxReachMeters = 0.55f;

    [Tooltip("Límite de pitch (+/- grados) en pitchJoint.")]
    public float maxPitchAbsDeg = 75f;

    [Header("Feel / Responsiveness")]
    [Tooltip("Velocidad máxima de yaw por frame (grados). Más bajo = más 'pesado'.")]
    public float yawStepDeg = 18f;

    [Tooltip("Velocidad máxima de pitch por frame (grados). Más bajo = más 'pesado'.")]
    public float pitchStepDeg = 10f;

    [Tooltip("Cuánto acompaña la rotación del monitor a la mano (0 a 1).")]
    [Range(0f, 1f)] public float monitorRotationFollow = 0.25f;

    // runtime
    private UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor _interactor;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable _activeHandle;
    private Transform _attach;
    private bool _selected;

    // NUEVO: estado para rotación alrededor de la manija (delta frame a frame)
    private Quaternion _lastAttachRot;
    private bool _hasLastAttachRot;

    void OnEnable()
    {
        if (monitorPivot == null || pitchJoint == null || yawJoint == null)
        {
            Debug.LogError("[CeilingArm2DOF] Asigná monitorPivot, pitchJoint y yawJoint en el Inspector.");
            enabled = false;
            return;
        }
        if (leftHandle == null && rightHandle == null)
        {
            Debug.LogError("[CeilingArm2DOF] Asigná leftHandle y/o rightHandle (XRBaseInteractable) en el Inspector.");
            enabled = false;
            return;
        }

        if (leftHandle != null)
        {
            leftHandle.selectEntered.AddListener(OnHandleSelectEntered);
            leftHandle.selectExited.AddListener(OnHandleSelectExited);
        }

        if (rightHandle != null)
        {
            rightHandle.selectEntered.AddListener(OnHandleSelectEntered);
            rightHandle.selectExited.AddListener(OnHandleSelectExited);
        }
    }

    void OnDisable()
    {
        if (leftHandle != null)
        {
            leftHandle.selectEntered.RemoveListener(OnHandleSelectEntered);
            leftHandle.selectExited.RemoveListener(OnHandleSelectExited);
        }

        if (rightHandle != null)
        {
            rightHandle.selectEntered.RemoveListener(OnHandleSelectEntered);
            rightHandle.selectExited.RemoveListener(OnHandleSelectExited);
        }
    }

    private void OnHandleSelectEntered(SelectEnterEventArgs args)
    {
        _selected = true;
        _interactor = args.interactorObject;
        _activeHandle = args.interactableObject as UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable;

        _attach = null;
        if (_interactor != null && _activeHandle != null)
            _attach = _interactor.GetAttachTransform(_activeHandle);

        if (_attach == null && _interactor != null)
            _attach = _interactor.transform;

        // NUEVO: inicializamos el delta de rotación
        if (_attach != null)
        {
            _lastAttachRot = _attach.rotation;
            _hasLastAttachRot = true;
        }
        else
        {
            _hasLastAttachRot = false;
        }
    }

    private void OnHandleSelectExited(SelectExitEventArgs args)
    {
        if (args.interactableObject == _activeHandle)
        {
            _selected = false;
            _interactor = null;
            _activeHandle = null;
            _attach = null;

            // NUEVO: reset estado
            _hasLastAttachRot = false;
        }
    }

    void LateUpdate()
    {
        if (!_selected || _attach == null) return;

        Vector3 targetPos = _attach.position;
        Quaternion targetRot = _attach.rotation;

        // 1) Limit reach (evita “estirar infinito”)
        targetPos = ClampTargetToReach(targetPos);

        // 2) Resolver yaw + pitch (solo 2 DOF como tu rig real)
        SolveYaw(targetPos);
        SolvePitch(targetPos);

        // 3) Rotación del monitor ALREDEDOR del punto de agarre (no desde el pivot del monitor)
        ApplyMonitorRotationAroundGrip(targetRot);
    }

    private void ApplyMonitorRotationAroundGrip(Quaternion currentAttachRot)
    {
        if (monitorRotationFollow <= 0f) return;
        if (!_hasLastAttachRot) { _lastAttachRot = currentAttachRot; _hasLastAttachRot = true; return; }

        // delta rotación mano (frame a frame)
        Quaternion delta = currentAttachRot * Quaternion.Inverse(_lastAttachRot);

        // Filtrado "pesado": acercamos delta a identity
        delta = Quaternion.Slerp(Quaternion.identity, delta, monitorRotationFollow);

        // Punto de giro: preferimos la manija activa (lo más lógico)
        Vector3 pivotPoint = (_activeHandle != null) ? _activeHandle.transform.position : _attach.position;

        // Aplicar rotación alrededor del pivotPoint
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;

        if (axis.sqrMagnitude > 1e-8f && Mathf.Abs(angle) > 0.0001f)
        {
            monitorPivot.RotateAround(pivotPoint, axis, angle);
        }

        _lastAttachRot = currentAttachRot;
    }

    private Vector3 ClampTargetToReach(Vector3 targetWorld)
    {
        if (maxReachMeters <= 0f) return targetWorld;

        Vector3 origin = yawJoint.position;
        Vector3 v = targetWorld - origin;
        float d = v.magnitude;

        if (d <= maxReachMeters) return targetWorld;
        if (d < 1e-6f) return targetWorld;

        return origin + (v / d) * maxReachMeters;
    }

    private void SolveYaw(Vector3 targetWorld)
    {
        Vector3 axis = yawJoint.up;
        Vector3 origin = yawJoint.position;

        Vector3 toTarget = Vector3.ProjectOnPlane(targetWorld - origin, axis);
        Vector3 toEff = Vector3.ProjectOnPlane(monitorPivot.position - origin, axis);

        if (toTarget.sqrMagnitude < 1e-10f || toEff.sqrMagnitude < 1e-10f) return;

        float yaw = Vector3.SignedAngle(toEff, toTarget, axis);
        yaw = Mathf.Clamp(yaw, -yawStepDeg, yawStepDeg);

        yawJoint.rotation = Quaternion.AngleAxis(yaw, axis) * yawJoint.rotation;
    }

    private void SolvePitch(Vector3 targetWorld)
    {
        Vector3 axis = pitchJoint.right;
        Vector3 origin = pitchJoint.position;

        Vector3 toTarget = targetWorld - origin;
        Vector3 toEff = monitorPivot.position - origin;

        Vector3 toTargetProj = Vector3.ProjectOnPlane(toTarget, axis).normalized;
        Vector3 toEffProj = Vector3.ProjectOnPlane(toEff, axis).normalized;

        if (toTargetProj.sqrMagnitude < 1e-10f || toEffProj.sqrMagnitude < 1e-10f) return;

        float pitch = Vector3.SignedAngle(toEffProj, toTargetProj, axis);
        pitch = Mathf.Clamp(pitch, -pitchStepDeg, pitchStepDeg);

        pitchJoint.rotation = Quaternion.AngleAxis(pitch, axis) * pitchJoint.rotation;

        Vector3 e = pitchJoint.localEulerAngles;
        e.x = Normalize180(e.x);
        e.x = Mathf.Clamp(e.x, -maxPitchAbsDeg, maxPitchAbsDeg);
        pitchJoint.localEulerAngles = new Vector3(e.x, pitchJoint.localEulerAngles.y, pitchJoint.localEulerAngles.z);
    }

    private static float Normalize180(float a)
    {
        a %= 360f;
        if (a > 180f) a -= 360f;
        return a;
    }
}