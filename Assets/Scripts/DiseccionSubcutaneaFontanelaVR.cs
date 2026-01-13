using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class DiseccionSubcutaneaFontanelaVR : SurgicalStep
{
    [Header("Input VR")]
    public MonoBehaviour inputSourceBehaviour;
    private IToolInputSource input;

    [Header("Splines")]
    public SplineContainer subcutaneousSpline;
    public SplineContainer fontanelleSpline;

    [Header("Config")]
    public float maxDistanceToSpline = 0.015f;
    public bool doSubcutaneousThenFontanelle = true;
    public float penaltyThreshold = 0.7f;

    [Header("FEEDBACK")]
    [Header("Bones Movement")]
    public Transform endBoneSubcutanea;
    public Transform endBoneFontanela;
    public float finalRotation = -20f;
    public float boneSpeed = 4f;
    [Space]
    [Header("Precision Halo")]
    public Transform precisionHalo;
    public Renderer haloRenderer;
    public Color goodColor = new Color(0.3f, 1f, 0.8f);
    public Color badColor = new Color(1f, 0.3f, 0.3f);
    public float minHaloScale = 0.005f;
    public float maxHaloScale = 0.02f;
    public float haloSmooth = 10f;
    [Space]
    [Header("Fontanelle")]
    [SerializeField] FontanellePainter painter;
    [SerializeField] LayerMask fontanelleLayer;
    [SerializeField] float paintRate = 1f;
    [SerializeField] ParticleSystem tissueParticles;
    [SerializeField] float maxEmissionRate = 30f;

    

    enum Paso { Subcutanea, Fontanela }
    
    #region PRIVATE FIELDS
    [SerializeField] Paso pasoActual;

    SplineContainer currentSpline;
    float validatedProgress;
    float lastValidT = 0f;
    private float currentDistance;

    private float initialBoneAngle;
    private float errorPenalty;

    private float Speed
    {
        get
        {
            if (pasoActual == Paso.Subcutanea)
            {
                return 0.25f;
            }
            else
            {
                return 0.075f;
            }
        }
    }
    #endregion

    #region Unity Methods
    void Start()
    {
        input = inputSourceBehaviour as IToolInputSource;
        if (input == null || toolTip == null)
        {
            Debug.LogError("❌ Disección: Setup inválido.");
            enabled = false;
            return;
        }

        errorPenalty = 0f;
        pasoActual = Paso.Subcutanea;
        SetCurrentSpline();
        initialBoneAngle = endBoneSubcutanea.localEulerAngles.x;
    }

    void Update()
    {
        if (terminado)
            return;

        EvaluateToolOnSpline();
    }
    #endregion

    // =========================================================
    // CORE SIMULATOR LOGIC
    // =========================================================
    private void EvaluateToolOnSpline()
    {
        if (!input.PrimaryHeld)
            return;

        // Punto más cercano en el spline
        SplineUtility.GetNearestPoint(
            currentSpline.Spline,
            currentSpline.transform.InverseTransformPoint(toolTip.position),
            out float3 localPos,
            out float nearestT
        );

        Vector3 nearestWorldPos =
            currentSpline.transform.TransformPoint(localPos);

        // Distancia al plano anatómico
        float distance = Vector3.Distance(toolTip.position, nearestWorldPos);

        // Influencia
        float influence = Mathf.InverseLerp(
            maxDistanceToSpline,
            0f,
            distance
        );

        // Penalty
        if (influence < penaltyThreshold)
        {
            errorPenalty += (1f - influence) * (Time.deltaTime*0.05f);
        }
        else
        {
            errorPenalty -= (influence) * (Time.deltaTime * 0.15f);
        }
        errorPenalty = Mathf.Clamp01(errorPenalty);

        if (influence > 0f)
        {
            // Progreso acumulativo (NO depende de T)
            float speed = Speed;
            if (errorPenalty <= 1 - penaltyThreshold)
            {
                validatedProgress += speed * influence * Time.deltaTime;
            }
            validatedProgress = Mathf.Clamp01(validatedProgress);
        }
        
        float effectiveT = validatedProgress * Mathf.Clamp01(influence);
        
        // 5. Feedback visual orgánico
        UpdateVisualFeedback(validatedProgress, effectiveT, distance, nearestWorldPos);

        // 6. Fin del paso
        if (validatedProgress >= 0.98f)
            CompleteCurrentStep();
    }

    // =========================================================
    // TRANSICIONES
    // =========================================================
    void CompleteCurrentStep()
    {
        ResetValues();
        
        if (pasoActual == Paso.Subcutanea)
        {
            if (endBoneSubcutanea != null)
                StartCoroutine(RotateBone(endBoneSubcutanea));

            if (doSubcutaneousThenFontanelle)
            {
                pasoActual = Paso.Fontanela;
                SetCurrentSpline();
                lastValidT = 0f;
                validatedProgress = 0f;
                return;
            }
        }
        else
        {
            if (endBoneFontanela != null)
                StartCoroutine(RotateBone(endBoneFontanela));
        } 
        ResetValues();
        terminado = true;
        EndStep();
    }

    void SetCurrentSpline()
    {
        currentSpline = (pasoActual == Paso.Subcutanea)
            ? subcutaneousSpline
            : fontanelleSpline;
    }

    // =========================================================
    // FEEDBACK
    // =========================================================
    void UpdateVisualFeedback(float t, float precision, float distance, Vector3 nearestWorldPos)
    {
        if (precisionHalo == null || haloRenderer == null)
            return;

        // Retorno si está muy mal
        if (precision <= 0f)
        {
            return;
        }

        precisionHalo.gameObject.SetActive(true);

        // Tamaño (más preciso = más chico)
        float scale = Mathf.Lerp(maxHaloScale, minHaloScale, distance);
        precisionHalo.localScale = Vector3.Lerp(
            precisionHalo.localScale,
            Vector3.one * scale,
            Time.deltaTime * haloSmooth
        );

        // Color
        Color c = Color.Lerp(badColor, goodColor, precision);
        haloRenderer.material.color = c;
        
        // Apertura de piel
        if (pasoActual == Paso.Subcutanea)
        {
            RotateBone(endBoneSubcutanea, precision);
        }
        // Painter Brush
        else
        {
            Ray ray = new Ray(toolTip.position, Vector3.down);
            Debug.DrawRay(ray.origin, ray.direction * 0.002f, Color.green);
            if (Physics.Raycast(ray, out RaycastHit hit, 0.002f, fontanelleLayer))
            {
                painter.Paint(
                    hit.textureCoord,
                    paintRate * (Time.deltaTime * t)
                );
            }
            
            PlayFontanelaParticles(precision, toolTip.position);
        }
        
    }

    System.Collections.IEnumerator RotateBone(Transform bone)
    {
        float start = bone.localEulerAngles.x;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            float angle = Mathf.LerpAngle(start, finalRotation, elapsed);
            Vector3 e = bone.localEulerAngles;
            e.x = angle;
            bone.localEulerAngles = e;

            elapsed += Time.deltaTime * boneSpeed;
            yield return null;
        }
    }

    private void RotateBone(Transform bone, float t)
    {
        float start = initialBoneAngle;
        float angle = Mathf.LerpAngle(start, finalRotation, t);
        Vector3 e = bone.localEulerAngles;
        e.x = angle;
        bone.localEulerAngles = e;
    }

    private void PlayFontanelaParticles(float precision, Vector3 pos)
    {
        if (tissueParticles == null)
            return;

        if(!tissueParticles.gameObject.activeInHierarchy)
            tissueParticles.gameObject.SetActive(true);
        
        tissueParticles.transform.position = pos;
        var emission = tissueParticles.emission;

        if (precision < 0.1f)
        {
            emission.rateOverTime = 0f;
            return;
        }

        emission.rateOverTime = Mathf.Lerp(5f, maxEmissionRate, precision);
    }

    private void ResetValues()
    {
        precisionHalo.gameObject.SetActive(false);
        errorPenalty = 0;
        tissueParticles.gameObject.SetActive(false);
    }
}
