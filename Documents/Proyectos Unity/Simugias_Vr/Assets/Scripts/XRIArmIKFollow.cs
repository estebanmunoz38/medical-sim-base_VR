using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRIArmIKFollow : MonoBehaviour
{
    [Header("XR Grab")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    [Tooltip("Punto default de agarre (tu 'Grabs').")]
    public Transform defaultAttach;

    [Tooltip("Agarre mano izquierda (tu 'LeftHandleAxis').")]
    public Transform leftAttach;

    [Tooltip("Agarre mano derecha (tu 'RightHandleAxis').")]
    public Transform rightAttach;

    [Header("IK Chain (CCD) - en este orden")]
    [Tooltip("Ej: Skin02Bone")]
    public Transform bone0;

    [Tooltip("Ej: Arm02Geo")]
    public Transform bone1;

    [Header("Effector / Target")]
    [Tooltip("Ej: AlignPlace (punto al final del brazo)")]
    public Transform endEffector;

    [Tooltip("Target que el brazo debe alcanzar. Por defecto: este monitor (transform).")]
    public Transform target;

    [Header("IK Settings")]
    [Range(1, 32)] public int iterations = 10;
    public float minDistance = 0.001f;
    [Range(0f, 1f)] public float rotationWeight = 1.0f;

    [Tooltip("Eje local sobre el que rota el hueso. Si tus pivots rotan distinto, cambiá esto.")]
    public Vector3 bone0Axis = Vector3.up;   // en tu screenshot era Y
    public Vector3 bone1Axis = Vector3.forward; // en tu screenshot era Z

    [Header("Hold / Physics")]
    public bool forceKinematic = true;
    public bool disableGravity = true;

    Rigidbody _rb;

    void Reset()
    {
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    void Awake()
    {
        if (!grab) grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();

        // XR settings para que NO agarre al centro por dynamic attach
        if (grab)
        {
            grab.useDynamicAttach = false;
            grab.selectEntered.AddListener(OnSelectEntered);
            grab.selectExited.AddListener(OnSelectExited);
        }
    }

    void Start()
    {
        if (target == null) target = transform;

        if (_rb && forceKinematic) _rb.isKinematic = true;
        if (_rb && disableGravity) _rb.useGravity = false;

        if (grab && defaultAttach) grab.attachTransform = defaultAttach;
    }

    void OnDestroy()
    {
        if (grab)
        {
            grab.selectEntered.RemoveListener(OnSelectEntered);
            grab.selectExited.RemoveListener(OnSelectExited);
        }
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Elegir attach por mano sin magia
        string n = args.interactorObject.transform.name.ToLowerInvariant();

        if (leftAttach && n.Contains("left")) grab.attachTransform = leftAttach;
        else if (rightAttach && n.Contains("right")) grab.attachTransform = rightAttach;
        else if (defaultAttach) grab.attachTransform = defaultAttach;

        if (_rb && forceKinematic) _rb.isKinematic = true;
        if (_rb && disableGravity) _rb.useGravity = false;
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        if (defaultAttach) grab.attachTransform = defaultAttach;

        // Se queda suspendido igual
        if (_rb && forceKinematic) _rb.isKinematic = true;
        if (_rb && disableGravity) _rb.useGravity = false;
    }

    void LateUpdate()
    {
        if (!bone0 || !bone1 || !endEffector || !target) return;

        SolveCCD();
    }

    void SolveCCD()
    {
        Vector3 targetPos = target.position;

        for (int it = 0; it < iterations; it++)
        {
            // Si ya llegamos, salimos
            float dist = Vector3.Distance(endEffector.position, targetPos);
            if (dist <= minDistance) break;

            // Iteramos huesos desde el último hacia el primero (bone1 -> bone0)
            RotateBoneTowards(bone1, bone1Axis, targetPos);
            RotateBoneTowards(bone0, bone0Axis, targetPos);
        }
    }

    void RotateBoneTowards(Transform bone, Vector3 localAxis, Vector3 targetPos)
    {
        Vector3 effPos = endEffector.position;
        Vector3 bonePos = bone.position;

        Vector3 toEff = (effPos - bonePos);
        Vector3 toTar = (targetPos - bonePos);

        if (toEff.sqrMagnitude < 1e-8f || toTar.sqrMagnitude < 1e-8f) return;

        // Eje de rotación en mundo (derivado del eje local que definís)
        Vector3 axisWorld = bone.TransformDirection(localAxis).normalized;

        // Proyectamos para rotar solo alrededor de ese eje (para imitar tus constraints)
        Vector3 toEffProj = Vector3.ProjectOnPlane(toEff, axisWorld).normalized;
        Vector3 toTarProj = Vector3.ProjectOnPlane(toTar, axisWorld).normalized;

        if (toEffProj.sqrMagnitude < 1e-8f || toTarProj.sqrMagnitude < 1e-8f) return;

        float angle = Vector3.SignedAngle(toEffProj, toTarProj, axisWorld);
        angle *= rotationWeight;

        bone.Rotate(axisWorld, angle, Space.World);
    }
}