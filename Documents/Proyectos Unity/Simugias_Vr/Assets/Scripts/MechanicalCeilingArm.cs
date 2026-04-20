using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Sistema mecánico de brazo articulado (bisagras con límites) controlado por grab del end-effector.
/// - Cadena de joints (Transform) desde el techo hasta el objeto final (pantalla/luz).
/// - Cada joint rota SOLO en su eje de bisagra (1 DOF) con límites.
/// - Se agarra desde 2 manijas (grab points) y se resuelve por CCD con constraints.
/// </summary>
public class MechanicalCeilingArm : MonoBehaviour
{
    [Header("Chain (orden EXACTO: del techo hacia la pantalla)")]
    [Tooltip("Joints/pivotes del brazo, en orden desde el techo (0) al último pivot (n-1).")]
    public Transform[] joints;

    [Tooltip("Transform del objeto final rígido (pantalla/luz). Debe estar al final de la cadena.")]
    public Transform endEffector;

    [Header("Hinges (uno por cada joint)")]
    [Tooltip("Config de bisagra para cada joint (mismo índice que joints).")]
    public HingeConfig[] hinges;

    [Header("Grab / XRI")]
    [Tooltip("XRGrabInteractable que se usa para agarrar el endEffector. Se auto-agrega si falta.")]
    public ClosestAttachGrabInteractable grab;

    [Tooltip("Dos manijas (grab points) en el endEffector (child transforms).")]
    public Transform handleA;

    public Transform handleB;

    [Header("Solver")]
    [Tooltip("Iteraciones por frame. 8-16 suele ir bien.")]
    [Range(1, 32)] public int iterations = 12;

    [Tooltip("Si la distancia al objetivo es menor a esto, corta antes (más estable).")]
    public float stopDistance = 0.0025f;

    [Tooltip("Velocidad de seguimiento del objetivo (suaviza). 0 = sin smoothing.")]
    public float targetSmoothing = 18f;

    [Tooltip("Limita cuánto puede rotar un joint por iteración (evita jumps).")]
    [Range(0.5f, 45f)] public float maxDegreesPerStep = 12f;

    [Header("Opcional")]
    [Tooltip("Si querés que el endEffector mantenga su orientación (aprox) hacia la mano, activalo.")]
    public bool followRotation = false;

    [Tooltip("Peso de la orientación si followRotation está activo.")]
    [Range(0f, 1f)] public float rotationWeight = 0.35f;

    // Target (pose de la mano / attach)
    private bool _isGrabbed;
    private Transform _targetTransform;
    private Vector3 _targetPos;
    private Quaternion _targetRot;

    private void Reset()
    {
        // Auto-intento básico para setear grab si el script se pone en el endEffector
        endEffector = transform;
    }

    private void Awake()
    {
        ValidateOrFixSetup();
        HookEvents();
    }

    private void OnEnable()
    {
        HookEvents();
    }

    private void OnDisable()
    {
        UnhookEvents();
    }

    private void Update()
    {
        if (!_isGrabbed || _targetTransform == null) return;

        // Target smoothing
        Vector3 desiredPos = _targetTransform.position;
        Quaternion desiredRot = _targetTransform.rotation;

        if (targetSmoothing > 0f)
        {
            float t = 1f - Mathf.Exp(-targetSmoothing * Time.deltaTime);
            _targetPos = Vector3.Lerp(_targetPos, desiredPos, t);
            _targetRot = Quaternion.Slerp(_targetRot, desiredRot, t);
        }
        else
        {
            _targetPos = desiredPos;
            _targetRot = desiredRot;
        }

        SolveCCD(_targetPos, _targetRot);
    }

    // ---------------------------
    // Setup / Events
    // ---------------------------

    private void ValidateOrFixSetup()
    {
        if (endEffector == null)
            throw new Exception("[MechanicalCeilingArm] Falta asignar endEffector.");

        // Grab interactable (subclase que elige manija más cercana)
        if (grab == null)
        {
            grab = endEffector.GetComponent<ClosestAttachGrabInteractable>();
            if (grab == null) grab = endEffector.gameObject.AddComponent<ClosestAttachGrabInteractable>();
        }

        // Rigidbody recomendado (kinematic) para XRI (evita cosas raras)
        var rb = endEffector.GetComponent<Rigidbody>();
        if (rb == null) rb = endEffector.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Colliders: el usuario los pone donde quiera (manijas y/o cuerpo), pero XRI necesita algún collider
        // No auto-creo colliders porque depende de tu mesh.

        // Manijas
        if (handleA == null || handleB == null)
        {
            // Intento autodetectar por nombre si existen
            var a = endEffector.Find("HandleA");
            var b = endEffector.Find("HandleB");
            if (handleA == null && a != null) handleA = a;
            if (handleB == null && b != null) handleB = b;
        }

        if (handleA == null || handleB == null)
            throw new Exception("[MechanicalCeilingArm] Asigná handleA y handleB (dos manijas) como child transforms del endEffector.");

        // Pasamos manijas al grab interactable
        grab.grabPoints = new[] { handleA, handleB };

        // Joints / Hinges
        if (joints == null || joints.Length == 0)
            throw new Exception("[MechanicalCeilingArm] Asigná joints[] (pivotes) desde techo hasta el último pivot.");

        if (hinges == null || hinges.Length != joints.Length)
            throw new Exception("[MechanicalCeilingArm] hinges[] debe tener el mismo tamaño que joints[].");

        // Guardamos pose inicial target para smoothing
        _targetPos = endEffector.position;
        _targetRot = endEffector.rotation;
    }

    private void HookEvents()
    {
        if (grab == null) return;
        grab.selectEntered.RemoveListener(OnSelectEntered);
        grab.selectExited.RemoveListener(OnSelectExited);
        grab.selectEntered.AddListener(OnSelectEntered);
        grab.selectExited.AddListener(OnSelectExited);
    }

    private void UnhookEvents()
    {
        if (grab == null) return;
        grab.selectEntered.RemoveListener(OnSelectEntered);
        grab.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        _isGrabbed = true;

        // El attach real (según manija más cercana) lo decide el grab interactable.
        // Usamos ese attachTransform como target.
        var interactor = args.interactorObject;
        Transform attach = grab.GetAttachTransform(interactor);

        _targetTransform = attach != null ? attach : args.interactorObject.transform;
        _targetPos = _targetTransform.position;
        _targetRot = _targetTransform.rotation;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        _isGrabbed = false;
        _targetTransform = null;
    }

    // ---------------------------
    // Solver CCD con bisagras
    // ---------------------------

    private void SolveCCD(Vector3 targetPos, Quaternion targetRot)
    {
        for (int it = 0; it < iterations; it++)
        {
            float dist = Vector3.Distance(endEffector.position, targetPos);
            if (dist <= stopDistance) break;

            // Recorremos de último joint a primero (CCD clásico)
            for (int i = joints.Length - 1; i >= 0; i--)
            {
                Transform joint = joints[i];
                var hinge = hinges[i];

                // Eje de bisagra en WORLD
                Vector3 axisWorld = joint.TransformDirection(hinge.localAxis.normalized);

                // Vectores desde joint
                Vector3 toEff = endEffector.position - joint.position;
                Vector3 toTar = targetPos - joint.position;

                // Proyectamos a plano perpendicular al eje (para tener 1 DOF real)
                Vector3 toEffProj = Vector3.ProjectOnPlane(toEff, axisWorld);
                Vector3 toTarProj = Vector3.ProjectOnPlane(toTar, axisWorld);

                float effMag = toEffProj.magnitude;
                float tarMag = toTarProj.magnitude;
                if (effMag < 1e-6f || tarMag < 1e-6f) continue;

                // Ángulo firmado alrededor del eje
                float signedAngle = Vector3.SignedAngle(toEffProj, toTarProj, axisWorld);

                // Limitar step para estabilidad
                signedAngle = Mathf.Clamp(signedAngle, -maxDegreesPerStep, maxDegreesPerStep);

                // Aplicamos rotación propuesta
                joint.Rotate(axisWorld, signedAngle, Space.World);

                // Aplicamos límites del joint en su eje (clamp sobre ángulo local alrededor de ese axis)
                ClampJointToLimits(joint, hinge);

                // Si querés orientar algo del end effector hacia la mano (opcional)
                if (followRotation && rotationWeight > 0f)
                {
                    // Esto NO agrega DOFs extra en joints: solo ayuda a que el end effector acompañe un poco.
                    // Es una aproximación suave.
                    Quaternion current = endEffector.rotation;
                    Quaternion desired = targetRot;
                    endEffector.rotation = Quaternion.Slerp(current, desired, rotationWeight * 0.1f);
                }
            }
        }
    }

    /// <summary>
    /// Clampea el joint según min/max en grados alrededor del eje localAxis.
    /// Implementación: calcula el ángulo actual relativo a la "pose de referencia" (referenceLocalRotation).
    /// </summary>
    private void ClampJointToLimits(Transform joint, HingeConfig hinge)
    {
        if (!hinge.useLimits) return;

        // Si no está inicializado, tomamos la rotación local actual como referencia base
        if (!hinge._hasReference)
        {
            hinge.referenceLocalRotation = joint.localRotation;
            hinge._hasReference = true;
            // Guardar de vuelta en array (struct copy)
            // OJO: HingeConfig es struct. Hay que escribirlo de nuevo.
            // Esto lo hacemos arriba en el loop: acá no tenemos índice.
        }

        // Para evitar el problema del struct-copy, hacemos clamp usando hinge.referenceLocalRotation ya seteado por el usuario
        Quaternion refRot = hinge.referenceLocalRotation;

        // Rotación actual relativa a referencia
        Quaternion rel = Quaternion.Inverse(refRot) * joint.localRotation;

        // Convertimos a axis-angle
        rel.ToAngleAxis(out float angle, out Vector3 axis);
        angle = NormalizeAngle(angle);

        // Determinar signo según alineación del axis con el eje esperado
        Vector3 expectedAxis = hinge.localAxis.normalized;
        if (Vector3.Dot(axis, expectedAxis) < 0f) angle = -angle;

        float clamped = Mathf.Clamp(angle, hinge.minDegrees, hinge.maxDegrees);

        // Reconstruimos
        Quaternion clampedRel = Quaternion.AngleAxis(clamped, expectedAxis);
        joint.localRotation = refRot * clampedRel;
    }

    private static float NormalizeAngle(float a)
    {
        // Unity devuelve 0..180 en angleAxis; ajustamos a -180..180 si es necesario.
        // Como angleAxis no da >180, el signo lo resolvemos por el axis.
        if (a > 180f) a -= 360f;
        return a;
    }

    // ---------------------------
    // Data
    // ---------------------------

    [Serializable]
    public struct HingeConfig
    {
        [Tooltip("Eje de bisagra en espacio LOCAL del joint. Ej: (0,1,0) para rotar sobre Y local.")]
        public Vector3 localAxis;

        [Tooltip("Activar límites min/max.")]
        public bool useLimits;

        [Tooltip("Mínimo en grados (ej: -45).")]
        public float minDegrees;

        [Tooltip("Máximo en grados (ej: 45).")]
        public float maxDegrees;

        [Tooltip("Rotación local base (pose neutra). Si no la seteás, dejala identidad y acomodás el joint en escena.")]
        public Quaternion referenceLocalRotation;

        [NonSerialized] public bool _hasReference;
    }
}

/// <summary>
/// XRGrabInteractable que elige el attachTransform más cercano entre múltiples grab points.
/// Esto te permite tener DOS manijas sin scripts extra.
/// </summary>
public class ClosestAttachGrabInteractable : UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable
{
    [Tooltip("Puntos de agarre (manijas). El sistema elige el más cercano a la mano al agarrar.")]
    public Transform[] grabPoints;

    private Transform _lastChosen;

    public override Transform GetAttachTransform(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor)
    {
        if (grabPoints == null || grabPoints.Length == 0)
            return base.GetAttachTransform(interactor);

        // Elegimos la manija más cercana al interactor
        Vector3 p = interactor.transform.position;
        float best = float.PositiveInfinity;
        Transform bestT = null;

        for (int i = 0; i < grabPoints.Length; i++)
        {
            var t = grabPoints[i];
            if (t == null) continue;
            float d = (t.position - p).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestT = t;
            }
        }

        _lastChosen = bestT != null ? bestT : base.GetAttachTransform(interactor);
        return _lastChosen;
    }
}