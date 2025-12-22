using UnityEngine;

public class ScalpelVRTool : MonoBehaviour
{
    [Header("Input VR")]
    public MonoBehaviour inputSourceBehaviour;       // Debe implementar IToolInputSource
    private IToolInputSource input;

    [Header("Scalpel")]
    public Transform scalpelModel;                   // Modelo del bisturí en mano
    public Transform scalpelTip;                     // Punta del bisturí

    [Header("Path (línea de corte)")]
    public Transform[] pathPoints;                   // Puntos del recorrido del corte
    public float movementSpeed = 0.7f;               // Velocidad de avance
    public float snapStartDistance = 0.08f;          // Distancia para enganchar al inicio

    private float t = 0f;                            // Normalizado [0..1]
    private bool cutting = false;
    private bool reachedEnd = false;

    [Header("Rotación / ajuste")]
    public float smoothPosition = 12f;
    public float smoothRotation = 12f;

    [Header("Animación de cierre / bone final")]
    public Transform endBone;
    public float finalRotation = -20f;
    public float boneSpeed = 4f;


    // =====================================================================
    // INIT VR
    // =====================================================================
    void Start()
    {
        input = inputSourceBehaviour as IToolInputSource;

        if (input == null)
        {
            Debug.LogError("❌ ScalpelVRTool: inputSourceBehaviour NO implementa IToolInputSource.");
            enabled = false;
            return;
        }

        if (scalpelModel == null)
        {
            Debug.LogError("❌ ScalpelVRTool: scalpelModel no asignado.");
            enabled = false;
            return;
        }

        if (pathPoints == null || pathPoints.Length < 2)
        {
            Debug.LogError("❌ ScalpelVRTool: pathPoints insuficientes.");
            enabled = false;
            return;
        }
    }

    // =====================================================================
    // UPDATE VR
    // =====================================================================
    void Update()
    {
        if (reachedEnd)
            return;

        if (!cutting)
        {
            TrySnapStart();
        }
        else
        {
            AdvanceCut();
        }
    }


    // =====================================================================
    // INTENTAR ENGANCHAR AL PUNTO INICIAL
    // =====================================================================
    void TrySnapStart()
    {
        Vector3 start = pathPoints[0].position;
        float dist = Vector3.Distance(scalpelTip.position, start);

        if (dist <= snapStartDistance && input.PrimaryDown)
        {
            cutting = true;
            t = 0f;
        }
    }


    // =====================================================================
    // AVANZAR EL CORTE
    // =====================================================================
    void AdvanceCut()
    {
        if (input.PrimaryHeld)
        {
            t += movementSpeed * Time.deltaTime;
        }

        t = Mathf.Clamp01(t);

        // Posición a lo largo del path
        Vector3 targetPos = GetPositionOnPath(t);
        scalpelModel.position = Vector3.Lerp(scalpelModel.position, targetPos, Time.deltaTime * smoothPosition);

        // Rotación orientada hacia próximo punto
        Quaternion targetRot = GetRotationOnPath(t);
        scalpelModel.rotation = Quaternion.Slerp(scalpelModel.rotation, targetRot, Time.deltaTime * smoothRotation);

        // Si llegó al final
        if (t >= 0.99f)
        {
            reachedEnd = true;
            cutting = false;
            StartCoroutine(CloseCut());
        }
    }


    // =====================================================================
    // ANIMACIÓN FINAL DEL HUESO / PIEL
    // =====================================================================
    System.Collections.IEnumerator CloseCut()
    {
        if (endBone == null)
            yield break;

        float angle = endBone.localEulerAngles.x;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            angle = Mathf.Lerp(angle, finalRotation, Time.deltaTime * boneSpeed);
            Vector3 e = endBone.localEulerAngles;
            e.x = angle;
            endBone.localEulerAngles = e;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }


    // =====================================================================
    // PATH UTILS
    // =====================================================================
    Vector3 GetPositionOnPath(float t)
    {
        float scaled = t * (pathPoints.Length - 1);
        int idx = Mathf.FloorToInt(scaled);
        int next = Mathf.Clamp(idx + 1, 0, pathPoints.Length - 1);

        float localT = scaled - idx;

        return Vector3.Lerp(pathPoints[idx].position, pathPoints[next].position, localT);
    }

    Quaternion GetRotationOnPath(float t)
    {
        float scaled = t * (pathPoints.Length - 1);
        int idx = Mathf.FloorToInt(scaled);
        int next = Mathf.Clamp(idx + 1, 0, pathPoints.Length - 1);

        Vector3 dir = (pathPoints[next].position - pathPoints[idx].position).normalized;
        return Quaternion.LookRotation(dir, Vector3.up);
    }
}
