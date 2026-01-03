using UnityEngine;

public class PlasticaCutaneaVR : MonoBehaviour
{
    [Header("Input VR")]
    public MonoBehaviour inputSourceBehaviour;
    private IToolInputSource input;

    [Header("Herramienta")]
    public Transform toolModel;
    public Transform toolTip;

    [Header("Recorrido de Plástica Cutánea")]
    public Transform[] pathPoints;

    [Header("Huesos / Bordes de piel")]
    public Transform pielIzquierda;
    public Transform pielDerecha;

    [Header("Escala objetivo (asignable)")]
    public Vector3 escalaFinalPielIzq = new Vector3(0.6f, 1f, 1f);
    public Vector3 escalaFinalPielDer = new Vector3(0.6f, 1f, 1f);

    [Header("Movimiento")]
    public float movementSpeed = 0.6f;
    public float snapStartDistance = 0.08f;
    public float smoothPosition = 12f;
    public float smoothRotation = 12f;

    private float t = 0f;
    private bool trabajando = false;
    private bool terminado = false;

    private Vector3 escalaInicialIzq;
    private Vector3 escalaInicialDer;

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

        if (pielIzquierda == null || pielDerecha == null)
        {
            enabled = false;
            return;
        }

        // Guardamos escala inicial
        escalaInicialIzq = pielIzquierda.localScale;
        escalaInicialDer = pielDerecha.localScale;
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

        // Movimiento herramienta por waypoint
        Vector3 targetPos = GetPositionOnPath(t);
        toolModel.position = Vector3.Lerp(toolModel.position, targetPos, Time.deltaTime * smoothPosition);

        Quaternion targetRot = GetRotationOnPath(t);
        toolModel.rotation = Quaternion.Slerp(toolModel.rotation, targetRot, Time.deltaTime * smoothRotation);

        // Escalado progresivo de los bordes de piel
        pielIzquierda.localScale = Vector3.Lerp(
            escalaInicialIzq,
            escalaFinalPielIzq,
            t
        );

        pielDerecha.localScale = Vector3.Lerp(
            escalaInicialDer,
            escalaFinalPielDer,
            t
        );

        if (t >= 0.99f)
        {
            trabajando = false;
            terminado = true;
        }
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
