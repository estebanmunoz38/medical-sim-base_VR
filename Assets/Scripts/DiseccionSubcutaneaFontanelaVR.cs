using UnityEngine;

public class DiseccionSubcutaneaFontanelaVR : MonoBehaviour
{
    [Header("Input VR")]
    public MonoBehaviour inputSourceBehaviour;   // Debe implementar IToolInputSource
    private IToolInputSource input;

    [Header("Herramienta (modelo y punta)")]
    public Transform toolModel;                  // Modelo de la herramienta en mano
    public Transform toolTip;                    // Punta (o punto de contacto) de la herramienta

    [Header("Recorrido - Disección Subcutánea")]
    public Transform[] subcutaneousPathPoints;

    [Header("Recorrido - Disección Fontanela")]
    public Transform[] fontanellePathPoints;

    [Header("Comportamiento")]
    public bool hacerSubcutaneaYLuegoFontanela = true;  // Si false, usa 'empezarEn'
    public Paso empezarEn = Paso.Subcutanea;

    [Header("Movimiento")]
    public float movementSpeed = 0.7f;
    public float snapStartDistance = 0.08f;
    public float smoothPosition = 12f;
    public float smoothRotation = 12f;

    [Header("Animación final (opcional) - Subcutánea")]
    public Transform endBoneSubcutanea;
    public float finalRotationSubcutanea = -20f;
    public float boneSpeedSubcutanea = 4f;

    [Header("Animación final (opcional) - Fontanela")]
    public Transform endBoneFontanela;
    public float finalRotationFontanela = -20f;
    public float boneSpeedFontanela = 4f;

    public enum Paso { Subcutanea, Fontanela }

    private Paso pasoActual;
    private Transform[] pathPointsActual;

    private float t = 0f;        // [0..1]
    private bool trabajando = false;
    private bool terminado = false;

    void Start()
    {
        input = inputSourceBehaviour as IToolInputSource;

        if (input == null)
        {
            Debug.LogError("❌ DiseccionSubcutaneaFontanelaVR: inputSourceBehaviour NO implementa IToolInputSource.");
            enabled = false;
            return;
        }

        if (toolModel == null || toolTip == null)
        {
            Debug.LogError("❌ DiseccionSubcutaneaFontanelaVR: toolModel o toolTip no asignados.");
            enabled = false;
            return;
        }

        // Selección inicial del paso
        pasoActual = (hacerSubcutaneaYLuegoFontanela) ? Paso.Subcutanea : empezarEn;
        if (!SetPathForCurrentStep())
        {
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (terminado) return;

        if (!trabajando)
            TrySnapStart();
        else
            Advance();
    }

    // =========================
    // Enganche al inicio
    // =========================
    void TrySnapStart()
    {
        Vector3 start = pathPointsActual[0].position;
        float dist = Vector3.Distance(toolTip.position, start);

        if (dist <= snapStartDistance && input.PrimaryDown)
        {
            trabajando = true;
            t = 0f;
        }
    }

    // =========================
    // Avance por recorrido
    // =========================
    void Advance()
    {
        if (input.PrimaryHeld)
            t += movementSpeed * Time.deltaTime;

        t = Mathf.Clamp01(t);

        Vector3 targetPos = GetPositionOnPath(pathPointsActual, t);
        toolModel.position = Vector3.Lerp(toolModel.position, targetPos, Time.deltaTime * smoothPosition);

        Quaternion targetRot = GetRotationOnPath(pathPointsActual, t);
        toolModel.rotation = Quaternion.Slerp(toolModel.rotation, targetRot, Time.deltaTime * smoothRotation);

        if (t >= 0.99f)
        {
            trabajando = false;
            StartCoroutine(FinishCurrentStep());
        }
    }

    // =========================
    // Fin de paso y transición
    // =========================
    System.Collections.IEnumerator FinishCurrentStep()
    {
        // Animación final opcional del paso actual
        if (pasoActual == Paso.Subcutanea)
        {
            if (endBoneSubcutanea != null)
                yield return StartCoroutine(RotateBone(endBoneSubcutanea, finalRotationSubcutanea, boneSpeedSubcutanea));
        }
        else // Fontanela
        {
            if (endBoneFontanela != null)
                yield return StartCoroutine(RotateBone(endBoneFontanela, finalRotationFontanela, boneSpeedFontanela));
        }

        // Si se quiere encadenar pasos: Subcutánea -> Fontanela
        if (hacerSubcutaneaYLuegoFontanela && pasoActual == Paso.Subcutanea)
        {
            pasoActual = Paso.Fontanela;
            if (!SetPathForCurrentStep())
            {
                terminado = true;
                yield break;
            }

            // Queda listo para enganchar al inicio del segundo recorrido
            t = 0f;
            trabajando = false;
            yield break;
        }

        // Si no hay más pasos
        terminado = true;
    }

    System.Collections.IEnumerator RotateBone(Transform bone, float finalRotX, float speed)
    {
        float angle = bone.localEulerAngles.x;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            angle = Mathf.Lerp(angle, finalRotX, Time.deltaTime * speed);
            Vector3 e = bone.localEulerAngles;
            e.x = angle;
            bone.localEulerAngles = e;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    bool SetPathForCurrentStep()
    {
        pathPointsActual = (pasoActual == Paso.Subcutanea) ? subcutaneousPathPoints : fontanellePathPoints;

        if (pathPointsActual == null || pathPointsActual.Length < 2)
        {
            Debug.LogError("❌ DiseccionSubcutaneaFontanelaVR: pathPoints insuficientes para el paso: " + pasoActual);
            return false;
        }

        return true;
    }

    // =========================
    // Utils de Path
    // =========================
    Vector3 GetPositionOnPath(Transform[] path, float tNorm)
    {
        float scaled = tNorm * (path.Length - 1);
        int idx = Mathf.FloorToInt(scaled);
        int next = Mathf.Clamp(idx + 1, 0, path.Length - 1);

        float localT = scaled - idx;
        return Vector3.Lerp(path[idx].position, path[next].position, localT);
    }

    Quaternion GetRotationOnPath(Transform[] path, float tNorm)
    {
        float scaled = tNorm * (path.Length - 1);
        int idx = Mathf.FloorToInt(scaled);
        int next = Mathf.Clamp(idx + 1, 0, path.Length - 1);

        Vector3 dir = (path[next].position - path[idx].position).normalized;
        if (dir.sqrMagnitude < 0.000001f) dir = toolModel.forward;

        return Quaternion.LookRotation(dir, Vector3.up);
    }
}
