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

    private bool isAttached = false;
    private Transform activeSnap = null;
    private Transform activeBone = null;

    private float initialBoneAngle = 0f;
    private Vector3 initialControllerPos;


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
        // Distancias
        float distFrontal = Vector3.Distance(retractorTip.position, snapFrontal.position);
        float distTrasera = Vector3.Distance(retractorTip.position, snapTrasera.position);

        // Enganchar
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
    // ENGANCHAR REACTOR
    // =====================================================================================
    void Attach(Transform snap, Transform bone)
    {
        isAttached = true;
        activeSnap = snap;
        activeBone = bone;

        // Pegar la herramienta físicamente al snap
        retractorModel.position = snap.position;
        retractorModel.rotation = snap.rotation;

        // Guardamos la posición inicial de la mano
        initialControllerPos = controllerTransform.position;

        // Guardamos ángulo inicial del hueso
        if (activeBone != null)
            initialBoneAngle = activeBone.localEulerAngles.x;
    }

    // =====================================================================================
    // MOVER EL HUESO SEGÚN LA MANO VR
    // =====================================================================================
    void UpdateBoneMovement()
    {
        if (activeBone == null) return;

        // Cuánto subió o bajó la mano desde el momento de enganchar
        float deltaY = controllerTransform.position.y - initialControllerPos.y;

        // Convertimos movimiento vertical del control → rotación del hueso
        float targetAngle = initialBoneAngle - deltaY * rotationMultiplier;

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
        activeBone = null;
        activeSnap = null;
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
