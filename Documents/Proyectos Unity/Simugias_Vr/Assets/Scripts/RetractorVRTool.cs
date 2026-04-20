using UnityEngine;

public class RetractorVRTool : MonoBehaviour
{
    [Header("Input VR")]
    public MonoBehaviour inputSourceBehaviour;       
    private IToolInputSource input;

    [Header("Retractor")]
    public Transform retractorModel;               
    public Transform retractorTip;               

    [Header("Snap Points")]
    public Transform snapFrontal;
    public Transform snapTrasera;
    public float snapDistance = 0.08f;

    [Header("Bones Afectados")]
    public Transform boneFrontal;
    public Transform boneTrasera;

    [Header("VR Hand / Controlador")]
    public Transform controllerTransform;   // ***LA MANO / CONTROLADOR REAL***

    [Header("Settings")]
    public float rotationMultiplier = 80f;
    public float smooth = 10f;

    [Header("Estado para FinSuturectomiaVR")]
    public bool wasEverAttached = false;
    public bool wasDetachedAfterAttach = false;
    [Range(0f, 1f)] public float currentOpenNormalized = 0f;
    [Range(0f, 1f)] public float remainingOpenNormalized = 0f;

    [Header("Lectura de apertura")]
    public float maxOpenAngleForNormalization = 35f;
    public bool keepPartiallyOpenOnDetach = true;
    [Range(0f, 1f)] public float detachOpenRetention = 0.35f;

    private bool isAttached = false;
    private Transform activeSnap = null;
    private Transform activeBone = null;

    private float initialBoneAngle = 0f;
    private Vector3 initialControllerPos;

    public bool IsAttached => isAttached;
    public bool WasEverAttached => wasEverAttached;
    public bool WasDetachedAfterAttach => wasDetachedAfterAttach;
    public float RemainingOpenNormalized => remainingOpenNormalized;

    void Start()
    {
        input = inputSourceBehaviour as IToolInputSource;

        if (input == null)
        {
            Debug.LogError("❌ RetractorVRTool: inputSourceBehaviour NO implementa IToolInputSource.");
            enabled = false;
            return;
        }

        if (controllerTransform == null)
        {
            Debug.LogError("❌ RetractorVRTool: controllerTransform NO asignado. Debe ser la mano VR.");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (!isAttached)
        {
            TryAttach();
        }
        else
        {
            UpdateBoneMovement();

            // Soltar con botón secundario
            if (input.SecondaryDown)
                Detach();
        }
    }

    // =====================================================================================
    // INTENTAR ENGANCHAR (SNAP)
    // =====================================================================================
    void TryAttach()
    {
        if (snapFrontal == null || snapTrasera == null || retractorTip == null) return;

        float distFrontal = Vector3.Distance(retractorTip.position, snapFrontal.position);
        float distTrasera = Vector3.Distance(retractorTip.position, snapTrasera.position);

        if (input.PrimaryDown)
        {
            if (distFrontal <= snapDistance)
            {
                Attach(snapFrontal, boneFrontal);
            }
            else if (distTrasera <= snapDistance)
            {
                Attach(snapTrasera, boneTrasera);
            }
        }
    }

    // =====================================================================================
    // ENGANCHAR RETRACTOR
    // =====================================================================================
    void Attach(Transform snap, Transform bone)
    {
        isAttached = true;
        activeSnap = snap;
        activeBone = bone;

        wasEverAttached = true;
        wasDetachedAfterAttach = false;

        retractorModel.position = snap.position;
        retractorModel.rotation = snap.rotation;

        initialControllerPos = controllerTransform.position;

        if (activeBone != null)
            initialBoneAngle = activeBone.localEulerAngles.x;
    }

    // =====================================================================================
    // MOVER EL HUESO SEGÚN LA MANO VR
    // =====================================================================================
    void UpdateBoneMovement()
    {
        if (activeBone == null) return;

        float deltaY = controllerTransform.position.y - initialControllerPos.y;

        float targetAngle = initialBoneAngle - deltaY * rotationMultiplier;

        float angleDelta = Mathf.Abs(targetAngle - initialBoneAngle);
        currentOpenNormalized = Mathf.Clamp01(angleDelta / maxOpenAngleForNormalization);

        Vector3 e = activeBone.localEulerAngles;
        e.x = targetAngle;

        activeBone.localEulerAngles = Vector3.Lerp(
            activeBone.localEulerAngles,
            e,
            Time.deltaTime * smooth
        );
    }

    // =====================================================================================
    // DESENGANCHAR / SOLTAR
    // =====================================================================================
    void Detach()
    {
        isAttached = false;
        wasDetachedAfterAttach = true;

        if (keepPartiallyOpenOnDetach)
            remainingOpenNormalized = Mathf.Clamp01(currentOpenNormalized * detachOpenRetention);
        else
            remainingOpenNormalized = currentOpenNormalized;

        activeBone = null;
        activeSnap = null;
    }

    // =====================================================================================
    // PERMITIR QUE FinSuturectomiaVR ACTUALICE EL CIERRE PROGRESIVO
    // =====================================================================================
    public void SetRemainingOpenNormalized(float value)
    {
        remainingOpenNormalized = Mathf.Clamp01(value);
    }

    // =====================================================================================
    // DIBUJAR GIZMOS PARA DEPURAR
    // =====================================================================================
    void OnDrawGizmos()
    {
        if (retractorTip == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(retractorTip.position, snapDistance);
    }
}