using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

/// <summary>
/// Kerrison (rongeur). El bocado depende de la rotación de las mandíbulas
/// respecto al borde óseo. Los fragmentos permanecen en escena (no se ocultan).
/// </summary>
public class Kerrison : MonoBehaviour
{
    [Header("Variables requeridas")]
    [Tooltip("Punto de sujeción del fragmento dentro de las mandíbulas")]
    [SerializeField] Transform targetPosition;
    [SerializeField] bool isDetecting;
    [SerializeField] string targetTag = "Removable";
    [SerializeField] bool hasOne;
    [SerializeField] bool detectOnStart = true;

    public bool IsHoldingFragment => hasOne;
    public int DepositCount { get; private set; }
    public bool HasDeposited => DepositCount > 0;

    [Header("Bocado / rotación")]
    [Tooltip("Transform de la boca del Kerrison. Forward = dirección de corte.")]
    [SerializeField] Transform biteMouth;
    [Tooltip("Dot mínimo entre la boca y la dirección al fragmento")]
    [SerializeField] [Range(0.1f, 0.95f)] float biteAlignDot = 0.55f;
    [Tooltip("Dot mínimo entre el plano de corte (up de la boca) y la normal del fragmento")]
    [SerializeField] [Range(0.1f, 0.95f)] float biteRollDot = 0.35f;
    [SerializeField] bool requireTriggerForBite = true;
    [SerializeField] MonoBehaviour inputSourceBehaviour;

    [Header("Tutorial")]
    [SerializeField] bool showTutorial = true;
    [TextArea] [SerializeField] string tutorialIdle =
        "Aproxime las mandíbulas al borde óseo. Rote el Kerrison hasta alinear el bocado. Gatillo para tomar el fragmento. No extraiga fuera del campo: deposite el hueso a la vista.";
    [TextArea] [SerializeField] string tutorialMisaligned =
        "Rotación incorrecta. Gire la herramienta hasta que las mandíbulas miren el borde y el plano de corte coincida con el hueso.";
    [TextArea] [SerializeField] string tutorialHolding =
        "Fragmento retenido. Deposite el bocado en el campo (no lo descarte). El hueso debe permanecer visible.";

    InputDevice rightHand;
    InputDevice leftHand;
    IToolInputSource input;

    Transform heldObj;
    SkullPieces skullPiece;
    SurgicalGuideBeacon _guide;
    Collider _candidate;

    void Start()
    {
        Init();
    }

    void Init()
    {
        rightHand = InputSystem.GetDevice<XRController>(CommonUsages.RightHand);
        leftHand = InputSystem.GetDevice<XRController>(CommonUsages.LeftHand);

        if (biteMouth == null)
            biteMouth = targetPosition != null ? targetPosition : transform;

        if (detectOnStart && !hasOne)
            isDetecting = true;

        input = inputSourceBehaviour as IToolInputSource;

        if (showTutorial)
        {
            _guide = SurgicalGuideBeacon.Attach(
                biteMouth,
                "Kerrison",
                tutorialIdle,
                new Vector3(0f, 0.06f, 0.02f));
        }
    }

    void Update()
    {
        UpdateTutorial();
    }

    public void DetectionActive()
    {
        if (!hasOne)
            isDetecting = true;
    }

    public void DetectionDisabled()
    {
        isDetecting = false;
    }

    public void DropPiece()
    {
        if (!hasOne || heldObj == null)
            return;

        heldObj.SetParent(null, true);

        var rb = heldObj.GetComponent<Rigidbody>();
        if (rb == null)
            rb = heldObj.gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        heldObj.gameObject.SetActive(true);

        if (skullPiece != null)
            skullPiece.SetOutlineColor(new Color(0.85f, 0.75f, 0.45f));

        hasOne = false;
        heldObj = null;
        skullPiece = null;
        isDetecting = true;
        DepositCount++;
    }

    void OnTriggerStay(Collider other)
    {
        if (hasOne)
        {
            if (other.gameObject.name == "ClearCol")
                DropPiece();
            return;
        }

        if (!isDetecting)
            return;

        if (!other.CompareTag(targetTag))
            return;

        _candidate = other;

        if (!IsBiteAligned(other.transform))
            return;

        bool triggerOk = !requireTriggerForBite || input == null || input.PrimaryHeld || input.PrimaryDown;
        if (!triggerOk)
            return;

        TakeBite(other.transform);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "ClearCol" && hasOne)
            DropPiece();
    }

    void OnTriggerExit(Collider other)
    {
        if (_candidate == other)
            _candidate = null;
    }

    void TakeBite(Transform piece)
    {
        if (piece == null || targetPosition == null)
            return;

        piece.gameObject.SetActive(true);

        heldObj = piece;
        heldObj.SetParent(targetPosition, true);
        heldObj.localPosition = Vector3.zero;
        heldObj.localRotation = Quaternion.identity;

        var rb = heldObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        skullPiece = heldObj.GetComponent<SkullPieces>();
        if (skullPiece != null)
            skullPiece.SetOutlineColor(new Color(0.45f, 0.78f, 0.55f));

        isDetecting = false;
        hasOne = true;
        _candidate = null;
    }

    /// <summary>
    /// El bocado solo es válido si la boca mira al fragmento y el plano de corte
    /// (rotación del Kerrison) coincide con la orientación del borde óseo.
    /// </summary>
    bool IsBiteAligned(Transform piece)
    {
        if (biteMouth == null || piece == null)
            return false;

        Vector3 mouthFwd = biteMouth.forward;
        Vector3 toPiece = piece.position - biteMouth.position;
        if (toPiece.sqrMagnitude < 0.000001f)
            return true;

        toPiece.Normalize();
        if (Vector3.Dot(mouthFwd, toPiece) < biteAlignDot)
            return false;

        Vector3 pieceAxis = piece.up;
        float rollVsUp = Mathf.Abs(Vector3.Dot(biteMouth.up, pieceAxis));
        float rollVsRight = Mathf.Abs(Vector3.Dot(biteMouth.right, pieceAxis));
        return Mathf.Max(rollVsUp, rollVsRight) >= biteRollDot;
    }

    void UpdateTutorial()
    {
        if (_guide == null)
            return;

        if (hasOne)
        {
            _guide.SetText("Kerrison", tutorialHolding);
            return;
        }

        if (_candidate != null && !IsBiteAligned(_candidate.transform))
        {
            _guide.SetText("Kerrison — rotación", tutorialMisaligned);
            return;
        }

        _guide.SetText("Kerrison", tutorialIdle);
    }
}
