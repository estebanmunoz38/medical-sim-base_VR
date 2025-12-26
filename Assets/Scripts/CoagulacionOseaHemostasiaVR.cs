using UnityEngine;

public class CoagulacionOseaHemostasiaVR : MonoBehaviour
{
    [Header("Input VR")]
    public MonoBehaviour inputSourceBehaviour;
    private IToolInputSource input;

    [Header("Herramienta")]
    public Transform toolModel;
    public Transform toolTip;

    [Header("Recorrido - Coagulación Ósea")]
    public Transform[] coagulacionPathPoints;

    [Header("Recorrido - Hemostasia")]
    public Transform[] hemostasiaPathPoints;

    [Header("Orden")]
    public bool hacerCoagulacionYLuegoHemostasia = true;
    public Paso empezarEn = Paso.CoagulacionOsea;

    [Header("Movimiento")]
    public float movementSpeed = 0.7f;
    public float snapStartDistance = 0.08f;
    public float smoothPosition = 12f;
    public float smoothRotation = 12f;

    [Header("FX Coagulación")]
    public ParticleSystem fxCoagulacion;
    public bool fxCoagulacionPlaySoloMientrasAvanza = true;

    [Header("FX Hemostasia")]
    public ParticleSystem fxHemostasia;
    public bool fxHemostasiaPlaySoloMientrasAvanza = true;

    [Header("Hemostasia - Superficie")]
    public bool usarRaycastParaPegarEnSuperficie = true;
    public LayerMask superficieMask = ~0;
    public float raycastDistance = 0.08f;
    public float offsetSobreSuperficie = 0.002f;

    public enum Paso { CoagulacionOsea, Hemostasia }

    private Paso pasoActual;
    private Transform[] pathPointsActual;

    private float t = 0f;
    private bool trabajando = false;
    private bool terminado = false;

    void Start()
    {
        input = inputSourceBehaviour as IToolInputSource;

        if (input == null || toolModel == null || toolTip == null)
        {
            enabled = false;
            return;
        }

        pasoActual = (hacerCoagulacionYLuegoHemostasia) ? Paso.CoagulacionOsea : empezarEn;
        if (!SetPathForCurrentStep())
        {
            enabled = false;
            return;
        }

        StopAllFX();
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
        Vector3 start = pathPointsActual[0].position;
        float dist = Vector3.Distance(toolTip.position, start);

        if (dist <= snapStartDistance && input.PrimaryDown)
        {
            trabajando = true;
            t = 0f;
            OnStepStartFX();
        }
    }

    void Advance()
    {
        if (input.PrimaryHeld)
        {
            t += movementSpeed * Time.deltaTime;
            OnStepHeldFX(true);
        }
        else
        {
            OnStepHeldFX(false);
        }

        t = Mathf.Clamp01(t);

        Vector3 targetPos = GetPositionOnPath(pathPointsActual, t);
        toolModel.position = Vector3.Lerp(toolModel.position, targetPos, Time.deltaTime * smoothPosition);

        Quaternion targetRot = GetRotationOnPath(pathPointsActual, t);
        toolModel.rotation = Quaternion.Slerp(toolModel.rotation, targetRot, Time.deltaTime * smoothRotation);

        if (pasoActual == Paso.Hemostasia && usarRaycastParaPegarEnSuperficie)
        {
            StickTipToSurface();
        }

        if (t >= 0.99f)
        {
            trabajando = false;
            OnStepEndFX();
            StartCoroutine(FinishCurrentStep());
        }
    }

    System.Collections.IEnumerator FinishCurrentStep()
    {
        if (hacerCoagulacionYLuegoHemostasia && pasoActual == Paso.CoagulacionOsea)
        {
            pasoActual = Paso.Hemostasia;
            if (!SetPathForCurrentStep())
            {
                terminado = true;
                yield break;
            }

            t = 0f;
            trabajando = false;
            yield break;
        }

        terminado = true;
    }

    bool SetPathForCurrentStep()
    {
        pathPointsActual = (pasoActual == Paso.CoagulacionOsea) ? coagulacionPathPoints : hemostasiaPathPoints;

        if (pathPointsActual == null || pathPointsActual.Length < 2)
            return false;

        return true;
    }

    void StickTipToSurface()
    {
        Vector3 origin = toolTip.position;
        Vector3 dir = toolTip.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, raycastDistance, superficieMask, QueryTriggerInteraction.Ignore))
        {
            toolTip.position = hit.point + hit.normal * offsetSobreSuperficie;
            toolTip.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
        }
    }

    void StopAllFX()
    {
        if (fxCoagulacion != null) fxCoagulacion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (fxHemostasia != null) fxHemostasia.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void OnStepStartFX()
    {
        if (pasoActual == Paso.CoagulacionOsea)
        {
            if (fxCoagulacion != null && !fxCoagulacionPlaySoloMientrasAvanza)
                fxCoagulacion.Play();
        }
        else
        {
            if (fxHemostasia != null && !fxHemostasiaPlaySoloMientrasAvanza)
                fxHemostasia.Play();
        }
    }

    void OnStepHeldFX(bool held)
    {
        if (pasoActual == Paso.CoagulacionOsea)
        {
            if (fxCoagulacion == null) return;

            if (fxCoagulacionPlaySoloMientrasAvanza)
            {
                if (held && !fxCoagulacion.isPlaying) fxCoagulacion.Play();
                if (!held && fxCoagulacion.isPlaying) fxCoagulacion.Pause();
            }
        }
        else
        {
            if (fxHemostasia == null) return;

            if (fxHemostasiaPlaySoloMientrasAvanza)
            {
                if (held && !fxHemostasia.isPlaying) fxHemostasia.Play();
                if (!held && fxHemostasia.isPlaying) fxHemostasia.Pause();
            }
        }
    }

    void OnStepEndFX()
    {
        if (pasoActual == Paso.CoagulacionOsea)
        {
            if (fxCoagulacion != null) fxCoagulacion.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        else
        {
            if (fxHemostasia != null) fxHemostasia.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

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
