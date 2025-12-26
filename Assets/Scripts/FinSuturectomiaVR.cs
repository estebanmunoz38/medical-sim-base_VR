using UnityEngine;

public class FinSuturectomiaVR : MonoBehaviour
{
    [Header("Input VR")]
    public MonoBehaviour inputSourceBehaviour;
    private IToolInputSource input;

    [Header("Herramienta (gubia / separador)")]
    public Transform toolModel;
    public Transform toolTip;

    [Header("Recorrido")]
    public Transform[] pathPoints;

    [Header("Huesos del cráneo a separar")]
    public Transform craneoIzquierdo;
    public Transform craneoDerecho;

    [Header("Separación")]
    public Vector3 desplazamientoIzquierdo = new Vector3(-0.01f, 0f, 0f);
    public Vector3 desplazamientoDerecho = new Vector3(0.01f, 0f, 0f);

    [Header("Movimiento")]
    public float movementSpeed = 0.6f;
    public float snapStartDistance = 0.08f;
    public float smoothPosition = 12f;
    public float smoothRotation = 12f;

    [Header("Chequeo de puentes óseos")]
    public bool chequearPuentes = true;
    public LayerMask puenteMask = ~0;
    public float chequeoDistance = 0.02f;

    private float t = 0f;
    private bool trabajando = false;
    private bool terminado = false;

    private Vector3 craneoIzqInicial;
    private Vector3 craneoDerInicial;

    void Start()
    {
        input = inputSourceBehaviour as IToolInputSource;

        if (input == null || toolModel == null || toolTip == null)
        {
            enabled = false;
            return;
        }

        if (pathPoints == null || pathPoints.Length < 2)
        {
            enabled = false;
            return;
        }

        if (craneoIzquierdo == null || craneoDerecho == null)
        {
            enabled = false;
            return;
        }

        craneoIzqInicial = craneoIzquierdo.localPosition;
        craneoDerInicial = craneoDerecho.localPosition;
    }

    void Update()
    {
        if (terminado) return;

        if (!trabajando)
            TrySnapStart();
        else
            Advance();
    }

    void TrySnapStart()
    {
        Vector3 start = pathPoints[0].position;
        float dist = Vector3.Distance(toolTip.position, start);

        if (dist <= snapStartDistance && input.PrimaryDown)
        {
            trabajando = true;
            t = 0f;
        }
    }

    void Advance()
    {
        if (input.PrimaryHeld)
            t += movementSpeed * Time.deltaTime;

        t = Mathf.Clamp01(t);

        // Movimiento herramienta
        Vector3 targetPos = GetPositionOnPath(t);
        toolModel.position = Vector3.Lerp(toolModel.position, targetPos, Time.deltaTime * smoothPosition);

        Quaternion targetRot = GetRotationOnPath(t);
        toolModel.rotation = Quaternion.Slerp(toolModel.rotation, targetRot, Time.deltaTime * smoothRotation);

        // Separación progresiva del cráneo
        craneoIzquierdo.localPosition = Vector3.Lerp(
            craneoIzqInicial,
            craneoIzqInicial + desplazamientoIzquierdo,
            t
        );

        craneoDerecho.localPosition = Vector3.Lerp(
            craneoDerInicial,
            craneoDerInicial + desplazamientoDerecho,
            t
        );

        // Chequeo simple de puentes
        if (chequearPuentes && HayPuenteOseo())
        {
            // Bloquea avance si hay puente
            t -= movementSpeed * Time.deltaTime;
            t = Mathf.Clamp01(t);
            return;
        }

        if (t >= 0.99f)
        {
            trabajando = false;
            terminado = true;
        }
    }

    bool HayPuenteOseo()
    {
        Vector3 origen = craneoIzquierdo.position;
        Vector3 dir = (craneoDerecho.position - craneoIzquierdo.position).normalized;

        return Physics.Raycast(
            origen,
            dir,
            chequeoDistance,
            puenteMask,
            QueryTriggerInteraction.Ignore
        );
    }

    Vector3 GetPositionOnPath(float tNorm)
    {
        float scaled = tNorm * (pathPoints.Length - 1);
        int idx = Mathf.FloorToInt(scaled);
        int next = Mathf.Clamp(idx + 1, 0, pathPoints.Length - 1);

        float localT = scaled - idx;
        return Vector3.Lerp(pathPoints[idx].position, pathPoints[next].position, localT);
    }

    Quaternion GetRotationOnPath(float tNorm)
    {
        float scaled = tNorm * (pathPoints.Length - 1);
        int idx = Mathf.FloorToInt(scaled);
        int next = Mathf.Clamp(idx + 1, 0, pathPoints.Length - 1);

        Vector3 dir = (pathPoints[next].position - pathPoints[idx].position).normalized;
        if (dir.sqrMagnitude < 0.000001f) dir = toolModel.forward;

        return Quaternion.LookRotation(dir, Vector3.up);
    }
}
