using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public enum ToolGripPreset
{
    Custom,
    Marker,
    Shaver
}

/// <summary>
/// Define cómo una herramienta se acomoda en la palma.
/// El dibujo del fibrón y el pintado de la rasuradora no se tocan: solo cambia la pose del objeto entero.
/// Marco de la mano, una vez calibrado: +Y hacia los dedos, +X hacia el pulgar, +Z hacia el dorso.
/// La palma mira hacia -Z. Un desplazamiento Z negativo apoya el mango del lado de la palma.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Simugias/Agarre en mano")]
public class ToolHandGrip : MonoBehaviour
{
    public enum GripMode
    {
        [InspectorName("Anclado a la mano")]
        Anchored,
        [InspectorName("Libre (como antes)")]
        Free
    }

    [Header("Detección")]
    [InspectorName("Modo de agarre")]
    public GripMode mode = GripMode.Anchored;

    [InspectorName("Semiextensión de la zona (m)")]
    [Tooltip("Caja del mango, en metros, sobre los ejes del objeto. X rojo, Y verde, Z azul.")]
    public Vector3 grabHalfExtents = new Vector3(0.026f, 0.014f, 0.014f);

    [InspectorName("Holgura (m)")]
    [Tooltip("Margen para no exigir el centro exacto. No agranda la zona hasta la punta.")]
    [Range(0f, 0.03f)]
    public float grabSlack = 0.008f;

    [InspectorName("Confirmar agarre (s)")]
    [Tooltip("El pellizco tiene que permanecer dentro de la zona este tiempo. Corta los roces al pasar la mano.")]
    [Range(0f, 0.2f)]
    public float grabConfirmSeconds = 0.045f;

    [Header("Punto de agarre")]
    [InspectorName("Punto de agarre (m)")]
    [Tooltip("Dónde se apoya la mano, en metros desde el pivote, sobre los ejes del objeto. En el fibrón, X negativo acerca el agarre a la punta.")]
    public Vector3 gripPositionMeters = new Vector3(-0.012f, 0f, 0f);

    [InspectorName("Eje de la punta")]
    [Tooltip("Eje local que debe apuntar hacia el trabajo. Fibrón: (-1, 0, 0). Rasuradora: (0, 0, 1).")]
    public Vector3 tipAxis = new Vector3(-1f, 0f, 0f);

    [InspectorName("Eje de arriba")]
    [Tooltip("Eje local que queda hacia el dorso o el pulgar, para trabar el rolido.")]
    public Vector3 upAxis = new Vector3(0f, 1f, 0f);

    [Header("Pose")]
    [InspectorName("Punta en la mano")]
    [Tooltip("Hacia dónde mira la punta, en el marco de la palma. Y = dedos, X = pulgar, Z = dorso. Z negativo mira hacia donde mira la palma.")]
    public Vector3 tipInHand = new Vector3(0f, 0.35f, -0.94f);

    [InspectorName("Arriba en la mano")]
    public Vector3 upInHand = new Vector3(1f, 0f, 0f);

    [InspectorName("Desplazamiento en la palma (m)")]
    [Tooltip("Corre el mango respecto del centro de la palma. Z negativo lo saca hacia los dedos, del lado de la palma.")]
    public Vector3 palmOffset = new Vector3(0.004f, 0.016f, -0.02f);

    [InspectorName("Rotación extra")]
    [Tooltip("Grados en el marco de la palma. Es el ajuste fino si la orientación general ya es la correcta.")]
    public Vector3 rotationEuler;

    [InspectorName("Fijar pose al tomar")]
    [Tooltip("La relación mano-herramienta queda fija al confirmar el agarre. Los dedos dejan de mover el mango.")]
    public bool lockPoseWhileHeld = true;

    [InspectorName("Mezcla con la pinza")]
    [Tooltip("Solo si Fijar pose al tomar está apagado. 0 queda en la palma. 1 sigue el pellizco y tiembla más.")]
    [Range(0f, 1f)]
    public float pinchBlend = 0.42f;

    [InspectorName("Acompañar al índice")]
    [Tooltip("Copia la dirección del índice. En herramientas largas mete temblor de rotación. 0 lo deja en la palma.")]
    [Range(0f, 0.45f)]
    public float indexAim = 0f;

    [InspectorName("Tope de la pinza (m)")]
    [Tooltip("Si el tracking dispara el pellizco lejos de la palma, se recorta a esta distancia.")]
    public float maxPinchFromPalm = 0.08f;

    [Header("Mano izquierda")]
    [InspectorName("Usar valores propios")]
    [Tooltip("La izquierda no hereda el desplazamiento ni la rotación extra de la derecha.")]
    public bool separateLeftHand = true;

    [InspectorName("Espejar desplazamiento y rotación")]
    [Tooltip("Solo si la izquierda queda al revés. Con el marco calibrado, pulgar = +X en las dos manos y no hace falta espejar.")]
    public bool mirrorLeftHand;

    [InspectorName("Desplazamiento palma izquierda")]
    public Vector3 leftPalmOffset;

    [InspectorName("Rotación extra izquierda")]
    public Vector3 leftRotationEuler;

    [Header("Entrada")]
    [InspectorName("Segundos de entrada")]
    [Tooltip("Tiempo para terminar de tomar el objeto. Corto: se siente tomado. Largo: se siente blando.")]
    [Range(0f, 0.25f)]
    public float snapSeconds = 0.08f;

    [Header("Suavizado")]
    [InspectorName("Corte mínimo de posición")]
    [Tooltip("Hz en reposo. Más bajo = más quieto y más retraso. El fibrón conviene más alto que la rasuradora.")]
    [Range(0.5f, 8f)]
    public float positionMinCutoff = 3.1f;

    [InspectorName("Respuesta de posición")]
    [Tooltip("Afloja el filtro cuando la mano se mueve rápido, para que el trazo no llegue tarde.")]
    [Range(0f, 40f)]
    public float positionBeta = 12f;

    [InspectorName("Corte mínimo de rotación")]
    [Tooltip("En un objeto largo, el ruido angular se ve más que el de posición. En reposo conviene más bajo que el de posición.")]
    [Range(0.4f, 6f)]
    public float rotationMinCutoff = 1.7f;

    [InspectorName("Respuesta de rotación")]
    [Range(0f, 0.3f)]
    public float rotationBeta = 0.06f;

    [InspectorName("Zona muerta de posición (m)")]
    [Tooltip("Solo ruido menor a esto. En el fibrón dejala en 0.")]
    [Range(0f, 0.002f)]
    public float positionDeadzone = 0f;

    [InspectorName("Zona muerta de rotación (grados)")]
    [Range(0f, 1.5f)]
    public float rotationDeadzone = 0.12f;

    [Header("Contacto")]
    [InspectorName("Estabilizar en contacto")]
    [Tooltip("Solo con la punta ya apoyada. Atenúa el rebote contra la superficie. No atrae desde lejos.")]
    public bool stabilizeContact = true;

    [InspectorName("Segundos del apoyo")]
    [Tooltip("Cuánto tarda la profundidad en seguir un cambio chico. Un levantamiento claro lo ignora.")]
    [Range(0.02f, 0.12f)]
    public float contactNormalSeconds = 0.045f;

    [InspectorName("Corrección máxima (m)")]
    [Tooltip("Tope de la corrección. Por encima de esto la punta sigue la mano, no la superficie.")]
    [Range(0f, 0.003f)]
    public float maxContactCorrection = 0.0012f;

    [InspectorName("Segundos de rotación en contacto")]
    [Tooltip("Frena el balanceo del mango alrededor de la punta. Un giro de muñeca lo atraviesa.")]
    [Range(0f, 0.08f)]
    public float contactRotationSeconds = 0.03f;

    [Header("Tracking")]
    [InspectorName("Gracia de tracking (s)")]
    [Tooltip("Mantiene la última pose válida si se oculta un dedo o se pierde la mano. Después suelta.")]
    [Range(0.05f, 0.45f)]
    public float trackingGraceSeconds = 0.16f;

    [InspectorName("Velocidad lineal máxima (m/s)")]
    [Tooltip("Un salto mayor en un frame se descarta. No limita un movimiento rápido de la mano.")]
    [Range(1f, 12f)]
    public float maxLinearSpeed = 6.5f;

    [InspectorName("Velocidad angular máxima (grados/s)")]
    [Range(180f, 1440f)]
    public float maxAngularSpeed = 800f;

    [Header("Soltar")]
    [InspectorName("Segundos para soltar")]
    [Tooltip("El pellizco tiene que seguir abierto este tiempo. Evita que un frame ruidoso suelte la herramienta.")]
    [Range(0f, 0.3f)]
    public float releaseSeconds = 0.07f;

    [InspectorName("Cuadros para soltar")]
    [Tooltip("Respaldo si Segundos para soltar está en 0.")]
    public int releaseFrames = 4;

    [Header("Guía")]
    [InspectorName("Mostrar zona en el visor")]
    public bool showGuide = true;

    [InspectorName("Radio de la guía (m)")]
    public float guideRadius = 0.011f;

    [InspectorName("Color de la guía")]
    public Color guideColor = new Color(0.35f, 0.9f, 1f, 0.75f);

    [InspectorName("Ocultar guía más allá de (m)")]
    public float guideFarMeters = 0.14f;

    [InspectorName("Guía clara dentro de (m)")]
    public float guideNearMeters = 0.055f;

    [Header("Editor")]
    [InspectorName("Previsualizar mano izquierda")]
    [Tooltip("El handle de la escena mueve la palma izquierda. Las dos tarjetas se dibujan igual.")]
    public bool previewLeftHand;

    [Header("Diagnóstico")]
    [InspectorName("Depurar pose")]
    [Tooltip("Muestra estado, error y gracia en el Inspector durante Play. No escribe cada frame.")]
    public bool debugPose;

    [InspectorName("Registrar sesión")]
    [Tooltip("Apagado no hace nada. Encendido guarda un resumen al salir de Play. Dejalo apagado en la demo.")]
    public bool recordSession;

    public enum GripPhase
    {
        Libre,
        Cerca,
        Lista,
        Tomada,
        Gracia,
        Soltando
    }

    XRGrabInteractable grab;
    LineRenderer guide;
    Material guideMaterial;
    int guideLook = -1;

    public GripPhase Phase { get; private set; }
    public bool TrackingValid { get; private set; }
    public float GraceRemaining { get; private set; }
    public float PositionErrorMeters { get; private set; }
    public float AngleErrorDegrees { get; private set; }
    public float QuietPositionMm { get; private set; }
    public float QuietAngleDegrees { get; private set; }
    public int TrackingLosses { get; private set; }
    public int FingerOcclusions { get; private set; }
    public int OutliersRejected { get; private set; }
    public int UnexpectedReleases { get; private set; }
    public string OwnerHand { get; private set; }

    Vector3 quietAnchorPosition;
    Quaternion quietAnchorRotation;
    float quietTime;
    float quietPosition;
    float quietAngle;
    bool quietReady;
    bool sessionAttached;

    public void Apply(ToolGripPreset preset)
    {
        if (preset == ToolGripPreset.Shaver)
            ApplyShaver();
        else if (preset == ToolGripPreset.Marker)
            ApplyMarker();
    }

    public void ApplyMarker()
    {
        mode = GripMode.Anchored;
        grabHalfExtents = new Vector3(0.026f, 0.014f, 0.014f);
        grabSlack = 0.008f;
        gripPositionMeters = new Vector3(-0.012f, 0f, 0f);
        tipAxis = new Vector3(-1f, 0f, 0f);
        upAxis = new Vector3(0f, 1f, 0f);
        tipInHand = new Vector3(0f, 0.45f, -0.89f);
        upInHand = new Vector3(1f, 0f, 0f);
        palmOffset = new Vector3(0.006f, 0.02f, -0.018f);
        rotationEuler = Vector3.zero;
        lockPoseWhileHeld = true;
        pinchBlend = 0.42f;
        indexAim = 0f;
        maxPinchFromPalm = 0.08f;
        separateLeftHand = true;
        mirrorLeftHand = false;
        leftPalmOffset = new Vector3(0.006f, 0.02f, -0.018f);
        leftRotationEuler = Vector3.zero;
        snapSeconds = 0.08f;
        positionMinCutoff = 3.1f;
        positionBeta = 12f;
        rotationMinCutoff = 1.7f;
        rotationBeta = 0.06f;
        positionDeadzone = 0f;
        rotationDeadzone = 0.12f;
        stabilizeContact = true;
        contactNormalSeconds = 0.045f;
        maxContactCorrection = 0.0012f;
        contactRotationSeconds = 0.03f;
        trackingGraceSeconds = 0.16f;
        maxLinearSpeed = 6.5f;
        maxAngularSpeed = 800f;
        releaseSeconds = 0.07f;
        releaseFrames = 4;
        grabConfirmSeconds = 0.045f;
        showGuide = true;
        guideRadius = 0.011f;
        guideColor = new Color(0.35f, 0.9f, 1f, 0.75f);
        guideFarMeters = 0.14f;
        guideNearMeters = 0.055f;
    }

    public void ApplyShaver()
    {
        mode = GripMode.Anchored;
        grabHalfExtents = new Vector3(0.02f, 0.018f, 0.024f);
        grabSlack = 0.008f;
        gripPositionMeters = new Vector3(0f, 0.006f, -0.05f);
        tipAxis = new Vector3(0f, 0f, 1f);
        upAxis = new Vector3(0f, 1f, 0f);
        tipInHand = new Vector3(0f, 0.82f, -0.57f);
        upInHand = new Vector3(0f, 0.57f, 0.82f);
        palmOffset = new Vector3(0f, 0.014f, -0.026f);
        rotationEuler = Vector3.zero;
        lockPoseWhileHeld = true;
        pinchBlend = 0.3f;
        indexAim = 0f;
        maxPinchFromPalm = 0.08f;
        separateLeftHand = true;
        mirrorLeftHand = false;
        leftPalmOffset = new Vector3(0f, 0.014f, -0.026f);
        leftRotationEuler = Vector3.zero;
        snapSeconds = 0.1f;
        positionMinCutoff = 2.4f;
        positionBeta = 8f;
        rotationMinCutoff = 1.5f;
        rotationBeta = 0.04f;
        positionDeadzone = 0.00015f;
        rotationDeadzone = 0.2f;
        stabilizeContact = true;
        contactNormalSeconds = 0.06f;
        maxContactCorrection = 0.0015f;
        contactRotationSeconds = 0.05f;
        trackingGraceSeconds = 0.2f;
        maxLinearSpeed = 6.5f;
        maxAngularSpeed = 800f;
        releaseSeconds = 0.08f;
        releaseFrames = 5;
        grabConfirmSeconds = 0.04f;
        showGuide = true;
        guideRadius = 0.016f;
        guideColor = new Color(1f, 0.72f, 0.32f, 0.78f);
        guideFarMeters = 0.16f;
        guideNearMeters = 0.06f;
    }

    public static void Ensure(XRGrabInteractable target, ToolGripPreset preset)
    {
        if (target == null || target.GetComponent<ToolHandGrip>() != null)
            return;

        ToolHandGrip grip = target.gameObject.AddComponent<ToolHandGrip>();
        grip.Apply(preset);
    }

    public bool AcceptsPinch(Vector3 worldPinch)
    {
        if (mode != GripMode.Anchored)
            return true;

        Vector3 delta = worldPinch - GripWorld(transform.position, transform.rotation);
        Quaternion rot = transform.rotation;
        float x = Mathf.Abs(Vector3.Dot(delta, rot * Vector3.right));
        float y = Mathf.Abs(Vector3.Dot(delta, rot * Vector3.up));
        float z = Mathf.Abs(Vector3.Dot(delta, rot * Vector3.forward));
        float slack = Mathf.Max(0f, grabSlack);
        return x <= grabHalfExtents.x + slack &&
               y <= grabHalfExtents.y + slack &&
               z <= grabHalfExtents.z + slack;
    }

    public void Evaluate(Pose hand, Vector3 pinch, Vector3 indexDirection, bool left, out Vector3 position, out Quaternion rotation)
    {
        Vector3 offset = PalmOffsetFor(left);
        Vector3 extra = EulerFor(left);
        bool locked = lockPoseWhileHeld && mode == GripMode.Anchored;
        float blend = locked ? 0f : Mathf.Clamp01(pinchBlend);
        float aim = locked ? 0f : indexAim;
        Quaternion handRotation = hand.rotation;
        if (aim > 0.001f && indexDirection.sqrMagnitude > 0.25f)
        {
            Vector3 dorsal = handRotation * Vector3.forward;
            Vector3 aimUp = indexDirection.normalized;
            if (Mathf.Abs(Vector3.Dot(dorsal, aimUp)) < 0.92f)
            {
                Quaternion aimed = Quaternion.LookRotation(dorsal, aimUp);
                handRotation = Quaternion.Slerp(handRotation, aimed, aim);
            }
        }

        Quaternion adjust = handRotation * Quaternion.Euler(extra);
        Vector3 tipWorld = adjust * Safe(tipInHand, Vector3.forward);
        Vector3 upWorld = adjust * Safe(upInHand, Vector3.up);
        rotation = MapRotation(Safe(tipAxis, Vector3.forward), Safe(upAxis, Vector3.up), tipWorld, upWorld);

        Vector3 fromPalm = pinch - hand.position;
        float limit = Mathf.Max(0.02f, maxPinchFromPalm);
        if (fromPalm.sqrMagnitude > limit * limit)
            pinch = hand.position + fromPalm.normalized * limit;

        Vector3 anchor = Vector3.Lerp(hand.position, pinch, blend);
        position = anchor + adjust * offset - rotation * gripPositionMeters;
    }

    public Vector3 GripWorld(Vector3 toolPosition, Quaternion toolRotation)
    {
        return toolPosition + toolRotation * gripPositionMeters;
    }

    public void PreviewPalm(bool left, out Vector3 palmPosition, out Quaternion palmRotation)
    {
        Quaternion align = MapRotation(Safe(tipAxis, Vector3.forward), Safe(upAxis, Vector3.up), Safe(tipInHand, Vector3.forward), Safe(upInHand, Vector3.up));
        Quaternion adjust = transform.rotation * Quaternion.Inverse(align);
        palmRotation = transform.rotation * Quaternion.Inverse(Quaternion.Euler(EulerFor(left)) * align);
        Vector3 grip = GripWorld(transform.position, transform.rotation);
        palmPosition = grip - adjust * PalmOffsetFor(left);
    }

    public Vector3 PalmOffsetFor(bool left)
    {
        if (left && separateLeftHand)
            return leftPalmOffset;
        Vector3 offset = palmOffset;
        if (left && mirrorLeftHand)
            offset.x = -offset.x;
        return offset;
    }

    public float ReleaseDelaySeconds()
    {
        if (releaseSeconds > 0.001f)
            return releaseSeconds;
        return Mathf.Max(1, releaseFrames) * (1f / 72f);
    }

    public int TrackingRecoveries { get; private set; }
    public int Reacquisitions { get; private set; }
    public float ContactGapMm { get; private set; }
    public float ContactCorrectionMm { get; private set; }

    public void BeginHold(bool leftHand)
    {
        TrackingLosses = 0;
        FingerOcclusions = 0;
        OutliersRejected = 0;
        UnexpectedReleases = 0;
        TrackingRecoveries = 0;
        Reacquisitions = 0;
        QuietPositionMm = 0f;
        QuietAngleDegrees = 0f;
        quietReady = false;
        quietTime = 0f;
        if (recordSession)
            GripSessionLog.Mark(this, GripMark.Grab, leftHand);
    }

    public void NoteTrackingLoss(bool leftHand)
    {
        TrackingLosses++;
        if (recordSession)
            GripSessionLog.Mark(this, GripMark.TrackingLoss, leftHand);
    }

    public void NoteTrackingRecovered(bool leftHand)
    {
        TrackingRecoveries++;
        if (recordSession)
            GripSessionLog.Mark(this, GripMark.TrackingRecovered, leftHand);
    }

    public void NoteReacquisition(bool leftHand)
    {
        Reacquisitions++;
        if (recordSession)
            GripSessionLog.Mark(this, GripMark.Reacquisition, leftHand);
    }

    public void NoteFingerOcclusion(bool leftHand)
    {
        FingerOcclusions++;
        if (recordSession)
            GripSessionLog.Mark(this, GripMark.FingerOcclusion, leftHand);
    }

    public void NoteOutlier(bool leftHand)
    {
        OutliersRejected++;
        if (recordSession)
            GripSessionLog.Mark(this, GripMark.Outlier, leftHand);
    }

    public void ReportContact(float gapMillimeters, float correctionMillimeters)
    {
        ContactGapMm = gapMillimeters;
        ContactCorrectionMm = correctionMillimeters;
    }

    public void NoteUserRelease(bool leftHand)
    {
        if (recordSession)
            GripSessionLog.Mark(this, GripMark.UserRelease, leftHand);
    }

    public void NoteUnexpectedRelease(bool leftHand)
    {
        UnexpectedReleases++;
        if (recordSession)
            GripSessionLog.Mark(this, GripMark.UnexpectedRelease, leftHand);
    }

    public void NoteQuiet(Vector3 outputPosition, Quaternion outputRotation, Vector3 targetPosition, Quaternion targetRotation, float dt)
    {
        if (!debugPose)
            return;

        if (!quietReady)
        {
            quietReady = true;
            quietAnchorPosition = targetPosition;
            quietAnchorRotation = targetRotation;
            return;
        }

        bool still = Vector3.Distance(targetPosition, quietAnchorPosition) < 0.003f &&
                     Quaternion.Angle(targetRotation, quietAnchorRotation) < 1.5f;
        if (!still)
        {
            quietAnchorPosition = targetPosition;
            quietAnchorRotation = targetRotation;
            quietTime = 0f;
            quietPosition = 0f;
            quietAngle = 0f;
            return;
        }

        quietTime += dt;
        quietPosition = Mathf.Max(quietPosition, Vector3.Distance(outputPosition, quietAnchorPosition));
        quietAngle = Mathf.Max(quietAngle, Quaternion.Angle(outputRotation, quietAnchorRotation));
        if (quietTime < 0.35f)
            return;

        QuietPositionMm = quietPosition * 1000f;
        QuietAngleDegrees = quietAngle;
    }

    public void Report(GripPhase phase, string owner, bool trackingValid, float graceRemaining, float positionError, float angleError)
    {
        if (!debugPose)
            return;
        Phase = phase;
        OwnerHand = owner;
        TrackingValid = trackingValid;
        GraceRemaining = graceRemaining;
        PositionErrorMeters = positionError;
        AngleErrorDegrees = angleError;
    }

    public bool TryGetFunctionalTip(out Vector3 worldTip)
    {
        worldTip = default;
        Draw draw = GetComponent<Draw>();
        if (draw != null && draw.tip != null)
        {
            worldTip = draw.tip.position;
            return true;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string childName = children[i].name;
            if (childName != "Point B" && childName != "Tip")
                continue;
            worldTip = children[i].position;
            return true;
        }

        return false;
    }

    public Vector3 EulerFor(bool left)
    {
        if (left && separateLeftHand)
            return leftRotationEuler;
        Vector3 euler = rotationEuler;
        if (left && mirrorLeftHand)
        {
            euler.y = -euler.y;
            euler.z = -euler.z;
        }

        return euler;
    }

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        if (!Application.isPlaying || !showGuide || guide != null)
            return;

        BuildGuide();
    }

    void OnDisable()
    {
        if (sessionAttached)
        {
            GripSessionLog.Detach(this);
            sessionAttached = false;
        }

        if (guide != null)
            guide.enabled = false;
        Phase = GripPhase.Libre;
    }

    void BuildGuide()
    {
        Transform existing = transform.Find("GuiaAgarre");
        if (existing != null)
        {
            guide = existing.GetComponent<LineRenderer>();
            if (guide != null)
            {
                guideMaterial = guide.sharedMaterial;
                guideLook = -1;
                return;
            }
        }

        var guideObject = new GameObject("GuiaAgarre");
        guideObject.transform.SetParent(transform, false);
        guide = guideObject.AddComponent<LineRenderer>();
        guide.useWorldSpace = true;
        guide.loop = true;
        guide.positionCount = 28;
        guide.startWidth = guide.endWidth = 0.0022f;
        guide.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        guide.receiveShadows = false;
        guideMaterial = HandVisualMaterial.Create(guideColor, guideColor.a);
        guide.sharedMaterial = guideMaterial;
        guide.numCapVertices = 4;
        guideLook = -1;
        RefreshGuide();
    }

    void Update()
    {
        if (Application.isPlaying && recordSession != sessionAttached)
        {
            if (recordSession)
                GripSessionLog.Attach(this);
            else
                GripSessionLog.Detach(this);
            sessionAttached = recordSession;
        }

        if (!Application.isPlaying || guide == null)
            return;

        bool held = grab != null && grab.isSelected;
        if (!showGuide || held)
        {
            guide.enabled = false;
            if (!held)
                Phase = GripPhase.Libre;
            else if (!debugPose)
                Phase = GripPhase.Tomada;
            return;
        }

        float distance = HandGestureGrabber.NearestPinch(GripWorld(transform.position, transform.rotation), guideNearMeters, out bool pinchingNear);
        bool visible = distance <= guideFarMeters;
        guide.enabled = visible;
        if (!visible)
            Phase = GripPhase.Libre;
        else if (pinchingNear)
            Phase = GripPhase.Lista;
        else
            Phase = GripPhase.Cerca;

        if (!visible)
            return;

        int look = pinchingNear ? 2 : (distance <= guideNearMeters ? 1 : 0);
        if (look != guideLook)
        {
            guideLook = look;
            float alpha = look == 2 ? 0.95f : (look == 1 ? 0.62f : 0.3f);
            float width = look == 2 ? 0.003f : (look == 1 ? 0.0022f : 0.0015f);
            ApplyGuideLook(alpha, width);
        }

        RefreshGuide();
    }

    void ApplyGuideLook(float alpha, float width)
    {
        Color color = guideColor;
        color.a = guideColor.a * alpha;
        if (guideMaterial != null)
        {
            guideMaterial.color = color;
            if (guideMaterial.HasProperty("_BaseColor"))
                guideMaterial.SetColor("_BaseColor", color);
        }

        guide.startWidth = guide.endWidth = width;
    }

    void RefreshGuide()
    {
        Vector3 center = GripWorld(transform.position, transform.rotation);
        Vector3 normal = transform.rotation * Safe(tipAxis, Vector3.forward);
        Vector3 tangent = Vector3.Cross(normal, transform.rotation * Vector3.up);
        if (tangent.sqrMagnitude < 0.0001f)
            tangent = Vector3.Cross(normal, transform.rotation * Vector3.right);
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(normal.normalized, tangent);
        float radius = Mathf.Max(0.004f, guideRadius);
        int count = guide.positionCount;
        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * Mathf.PI * 2f;
            guide.SetPosition(i, center + (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * radius);
        }
    }

    public static Quaternion MapRotation(Vector3 fromTip, Vector3 fromUp, Vector3 toTip, Vector3 toUp)
    {
        fromTip = Safe(fromTip, Vector3.forward);
        toTip = Safe(toTip, Vector3.forward);
        Quaternion alignTip = Quaternion.FromToRotation(fromTip, toTip);
        Vector3 upCurrent = Vector3.ProjectOnPlane(alignTip * Safe(fromUp, Vector3.up), toTip);
        Vector3 upTarget = Vector3.ProjectOnPlane(toUp, toTip);
        if (upCurrent.sqrMagnitude < 0.000001f || upTarget.sqrMagnitude < 0.000001f)
            return alignTip;

        return Quaternion.FromToRotation(upCurrent.normalized, upTarget.normalized) * alignTip;
    }

    static Vector3 Safe(Vector3 value, Vector3 fallback)
    {
        return value.sqrMagnitude < 0.000001f ? fallback : value.normalized;
    }

    void OnDrawGizmos()
    {
        DrawGizmos(false);
    }

    void OnDrawGizmosSelected()
    {
        DrawGizmos(true);
    }

    void DrawGizmos(bool selected)
    {
        Quaternion rot = transform.rotation;
        Vector3 grip = GripWorld(transform.position, rot);
        Color zone = selected ? new Color(1f, 0.85f, 0.15f, 0.95f) : new Color(1f, 0.85f, 0.15f, 0.35f);
        Gizmos.color = zone;
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(grip, rot, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, grabHalfExtents * 2f);
        if (selected)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.15f, 0.25f);
            Vector3 slack = grabHalfExtents + Vector3.one * Mathf.Max(0f, grabSlack);
            Gizmos.DrawWireCube(Vector3.zero, slack * 2f);
        }

        Gizmos.matrix = previous;

        Gizmos.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        Gizmos.DrawSphere(grip, selected ? 0.006f : 0.004f);

        if (selected)
        {
            DrawAxis(grip, rot * Vector3.right, new Color(1f, 0.3f, 0.25f, 0.9f), 0.03f);
            DrawAxis(grip, rot * Vector3.up, new Color(0.25f, 0.9f, 0.35f, 0.9f), 0.03f);
            DrawAxis(grip, rot * Vector3.forward, new Color(0.3f, 0.55f, 1f, 0.9f), 0.03f);
        }

        Vector3 tip = rot * Safe(tipAxis, Vector3.forward);
        Gizmos.color = new Color(0.2f, 0.85f, 1f, selected ? 1f : 0.4f);
        Gizmos.DrawLine(grip, grip + tip * 0.07f);
        Gizmos.DrawSphere(grip + tip * 0.07f, 0.004f);

        if (TryGetFunctionalTip(out Vector3 functionalTip))
        {
            Gizmos.color = new Color(1f, 0.2f, 0.75f, selected ? 1f : 0.45f);
            Gizmos.DrawSphere(functionalTip, 0.005f);
            Gizmos.DrawLine(grip, functionalTip);
        }

        if (!selected)
            return;

        DrawPalmGizmo(false, new Color(0.75f, 0.85f, 1f, 0.45f));
        DrawPalmGizmo(true, new Color(1f, 0.8f, 0.55f, 0.4f));
    }

    void DrawPalmGizmo(bool left, Color card)
    {
        PreviewPalm(left, out Vector3 palmPos, out Quaternion palmRot);
        Vector3 fingers = palmRot * Vector3.up;
        Vector3 thumb = palmRot * Vector3.right;
        Vector3 dorsal = palmRot * Vector3.forward;
        Gizmos.color = new Color(0.2f, 0.95f, 0.45f, 0.9f);
        Gizmos.DrawLine(palmPos, palmPos + fingers * 0.08f);
        Gizmos.color = new Color(1f, 0.35f, 0.3f, 0.9f);
        Gizmos.DrawLine(palmPos, palmPos + thumb * 0.045f);
        Gizmos.color = new Color(0.35f, 0.55f, 1f, 0.9f);
        Gizmos.DrawLine(palmPos, palmPos + dorsal * 0.035f);
        Matrix4x4 previous = Gizmos.matrix;
        Gizmos.color = card;
        Gizmos.matrix = Matrix4x4.TRS(palmPos + fingers * 0.015f, palmRot, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.08f, 0.1f, 0.018f));
        Gizmos.matrix = previous;
    }

    static void DrawAxis(Vector3 origin, Vector3 direction, Color color, float length)
    {
        if (direction.sqrMagnitude < 0.000001f)
            return;
        Gizmos.color = color;
        Gizmos.DrawLine(origin, origin + direction.normalized * length);
    }
}

public struct OneEuroVector3
{
    Vector3 last;
    Vector3 lastDerivative;
    bool ready;

    public void Reset(Vector3 value)
    {
        last = value;
        lastDerivative = Vector3.zero;
        ready = true;
    }

    public Vector3 Filter(Vector3 value, float dt, float minCutoff, float beta, float deadzone)
    {
        if (!ready || dt <= 0.0001f)
        {
            Reset(value);
            return value;
        }

        if (deadzone > 0f && (value - last).sqrMagnitude <= deadzone * deadzone)
            return last;

        float rate = 1f / dt;
        Vector3 derivative = (value - last) * rate;
        float derivativeAlpha = Alpha(rate, 1f);
        lastDerivative = Vector3.Lerp(lastDerivative, derivative, derivativeAlpha);
        float cutoff = Mathf.Max(0.01f, minCutoff) + Mathf.Max(0f, beta) * lastDerivative.magnitude;
        last = Vector3.Lerp(last, value, Alpha(rate, cutoff));
        return last;
    }

    public static float Alpha(float rate, float cutoff)
    {
        float tau = 1f / (2f * Mathf.PI * Mathf.Max(0.01f, cutoff));
        float te = 1f / Mathf.Max(0.01f, rate);
        return 1f / (1f + tau / te);
    }
}

public struct OneEuroQuaternion
{
    Quaternion last;
    float lastSpeed;
    bool ready;

    public void Reset(Quaternion value)
    {
        last = value;
        lastSpeed = 0f;
        ready = true;
    }

    public Quaternion Filter(Quaternion value, float dt, float minCutoff, float beta, float deadzoneDegrees)
    {
        if (!ready || dt <= 0.0001f)
        {
            Reset(value);
            return value;
        }

        if (Quaternion.Dot(last, value) < 0f)
            value = new Quaternion(-value.x, -value.y, -value.z, -value.w);

        float angle = Quaternion.Angle(last, value);
        if (angle <= Mathf.Max(0f, deadzoneDegrees))
            return last;

        float rate = 1f / dt;
        float speed = angle * rate;
        lastSpeed = Mathf.Lerp(lastSpeed, speed, OneEuroVector3.Alpha(rate, 1f));
        float cutoff = Mathf.Max(0.01f, minCutoff) + Mathf.Max(0f, beta) * lastSpeed;
        last = Quaternion.Slerp(last, value, OneEuroVector3.Alpha(rate, cutoff));
        return last;
    }
}
