using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class DiseccionSubcutaneaFontanelaVR : SurgicalStep
{
    [Header("Splines")]
    public SplineContainer subcutaneousSpline;
    public SplineContainer fontanelleSpline;

    [Header("Config")]
    public bool doSubcutaneousThenFontanelle = true;
    [SerializeField] float snapDistance = 0.015f;
    [SerializeField] private float snapAngle = 10f;
    [SerializeField] private float segmentUnlockThreshold = 0.85f;
    [SerializeField] float smoothingSpeed = 5f; // Ajusta qué tan "pesada" se siente la herramienta
    
    [Header("Subcutanea")]
    [SerializeField] private Transform ghostPoseSubcutaneus;
    [SerializeField] float maxRotationAngleSubcutaneus = 25f;
    [SerializeField] int segmentCountSubcutaneus = 12;
    
    [Header("Fontanela")]
    [SerializeField] Transform ghostPoseFontanela;
    [SerializeField] float maxRotationAngleFontanela = 25f;
    [SerializeField] int segmentCountFontanela = 12;
    
    [Header("Segmented Progress")]
    [SerializeField] SplineSegmentVisualizer segmentVisualizer;

    [Header("Bones Movement")]
    public Transform endBoneSubcutanea;
    public Transform endBoneFontanela;
    public float finalRotation = -20f;
    public float boneSpeed = 4f;
    [Space]
    [Header("Fontanelle")]
    [SerializeField] FontanellePainter painter;
    [SerializeField] LayerMask fontanelleLayer;
    [SerializeField] float paintRate = 1f;
    [SerializeField] ParticleSystem tissueParticles;
    [SerializeField] float maxEmissionRate = 30f;
    
    enum Paso { Subcutanea, Fontanela }
    
    #region PRIVATE FIELDS

    private Paso pasoActual;

    SplineContainer currentSpline;
    float validatedProgress;
    float lastValidT = 0f;
    private float currentDistance;

    private float initialBoneAngle;
    private float errorPenalty;
    
    [SerializeField] private int currentSegmentIndex = 0;
    [SerializeField] private float segmentProgress = 0f;
    bool toolSnapped;
    float splineT;
    Vector3 tipLocalOffset;
    Vector3 lastTipPos;
    private float lastAngle;
    private float smoothedScrapeIntensity;

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
                return 0.125f;
            }
        }
    }

    private int SegmentCount
    {
        get
        {
            if (pasoActual == Paso.Subcutanea)
            {
                return segmentCountSubcutaneus;
            }
            else
            {
                return segmentCountFontanela;
            }
        }
    }
    
    private float MaxRotationAngle
    {
        get
        {
            if (pasoActual == Paso.Subcutanea)
            {
                return maxRotationAngleSubcutaneus;
            }
            else
            {
                return maxRotationAngleFontanela;
            }
        }
    }

    private Transform GhostPose
    {
        get
        {
            if (pasoActual == Paso.Subcutanea)
            {
                return ghostPoseSubcutaneus;
            }
            else
            {
                return ghostPoseFontanela;
            }
        }
    }
    #endregion

    #region Unity Methods
    protected override void Start()
    {
        base.Start();
        errorPenalty = 0f;
        pasoActual = Paso.Subcutanea;
        SetCurrentSpline();
        initialBoneAngle = endBoneSubcutanea.localEulerAngles.x;
        tipLocalOffset = toolModel.InverseTransformPoint(toolTip.position);
        
    }

    void Update()
    {
        if (terminado)
            return;

        EvaluateGhostGuidedSplineMotion();
    }
    #endregion

    // =========================================================
    // CORE SIMULATOR LOGIC
    // =========================================================
    private void EvaluateGhostGuidedSplineMotion()
    {
        var gestures = surgicalTool.ActiveGestures;
        if (gestures == null)
        { 
            return;
        }
        
        if (!gestures.IsPinching)
        {
            if (toolSnapped)
            {
                LockTool(false);
            }

            return;
        }
        
        if (!toolSnapped)
        {
            TrySnapToGhost();
            return;
        }

        EvaluateGuidedRotation(gestures);
    }
    
    void TrySnapToGhost()
    {
        float posDist = Vector3.Distance(
            toolModel.position,
            GhostPose.position
        );

        float angDist = Quaternion.Angle(
            toolModel.rotation,
            GhostPose.rotation
        );

        if (posDist < snapDistance && angDist < snapAngle)
        {
            toolModel.SetPositionAndRotation(
                GhostPose.position,
                GhostPose.rotation
            );
            
            GhostPose.gameObject.SetActive(false);
            
            lastTipPos = toolTip.position;
            segmentProgress = 0f;
            
            LockTool(true);
        }
    }
    
    // =========================================================
    // ROTATION → PROGRESS
    // =========================================================
    void EvaluateGuidedRotation(IHandGestureProvider gestures)
    {
        Vector3 axis = pasoActual == Paso.Subcutanea ? GhostPose.up : -GhostPose.right;

        // 1. CÁLCULO DE VELOCIDAD ANGULAR
        float currentAngle = Vector3.SignedAngle(GhostPose.forward, toolModel.forward, axis);
        float angleDelta = Mathf.Abs(currentAngle - lastAngle);

        if (lastAngle > currentAngle)
        {
            lastAngle = Mathf.Lerp(lastAngle, currentAngle, Time.deltaTime);
        }
        else
        {
            lastAngle = currentAngle;
        }

        
        float rotationSpeed = angleDelta / Time.deltaTime;
    
        // 2. NORMALIZACIÓN (Raw Intensity)
        float rawIntensity = Mathf.Clamp01(rotationSpeed / 90f);
        
        // 3. SUAVIZADO (Smoothing)
        // Mathf.Lerp interpola entre el valor actual y el nuevo basándose en el tiempo
        smoothedScrapeIntensity = Mathf.Lerp(
            smoothedScrapeIntensity, 
            rawIntensity, 
            Time.deltaTime * smoothingSpeed
        );

        // 4. VALIDACIÓN DE RANGO
        float angleFactor = Mathf.InverseLerp(60f, 0f, Mathf.Abs(currentAngle - MaxRotationAngle));
        
        //print("RawScrape: "+rawIntensity + " || Scrape: "+smoothedScrapeIntensity + " || AngleFactor: "+angleFactor);
        // 5. CÁLCULO FINAL
        // Usamos el valor suavizado para un progreso más orgánico
        float delta = Speed * smoothedScrapeIntensity * angleFactor * gestures.Pressure * Time.deltaTime;

        segmentProgress = Mathf.Clamp01(segmentProgress + delta);

        var feedbackProgress = smoothedScrapeIntensity * angleFactor;
        //print("ScrapeIntensity: "+smoothedScrapeIntensity + " || angleFactor: " +angleFactor);
        // Feedback: usa el valor suavizado para que las barras de la UI no vibren locamente
        UpdateVisualFeedback(segmentProgress, feedbackProgress, currentSegmentIndex);

        if (segmentProgress >= segmentUnlockThreshold)
            CompleteSegment();
        
    }
    
    // =========================================================
    // SPLINE GUIDED MOTION
    // =========================================================
    void UpdateToolAlongSpline(float t)
    {
        SplineUtility.Evaluate(
            currentSpline.Spline,
            t,
            out float3 localPos,
            out float3 tangent,
            out float3 up
        );
        
        toolModel.rotation = GhostPose.rotation;
        
        Vector3 worldPos =
            currentSpline.transform.TransformPoint(localPos);

        Vector3 worldTangent =
            currentSpline.transform.TransformDirection(tangent);

        Quaternion targetRot = Quaternion.LookRotation(
            worldTangent,
            currentSpline.transform.up
        );

        

        // 🔹 Aplicamos offset LOCAL rotado a mundo
        toolModel.position = worldPos - toolModel.TransformVector(tipLocalOffset);

        ghostPoseSubcutaneus.position = worldPos;
    }
    
    void RotateAroundTip(Quaternion targetRotation)
    {
        Vector3 tipPos = toolTip.position;

        // Offset actual modelo → tip
        Vector3 offset = toolModel.position - tipPos;

        // Rotamos offset al nuevo frame
        offset = targetRotation * Quaternion.Inverse(toolModel.rotation) * offset;

        // Aplicamos rotación
        toolModel.rotation = targetRotation;

        // Reposicionamos manteniendo el tip fijo
        toolModel.position = tipPos + offset;
    }
    
    // =========================================================
    // SEGMENTS
    // =========================================================
    void UpdateSegmentProgress()
    {
        int newSegment =
            Mathf.FloorToInt(splineT * SegmentCount);

        if (newSegment > currentSegmentIndex)
        {
            currentSegmentIndex = newSegment;
            segmentVisualizer.UpdateVisual(currentSegmentIndex);
            OnSegmentCompleted();
        }
    }
    
    void CompleteSegment()
    {
        segmentProgress = 0f;
        currentSegmentIndex++;

        float t = (float)currentSegmentIndex / SegmentCount;

        splineT = t;

        UpdateToolAlongSpline(splineT);

        segmentVisualizer.UpdateVisual(currentSegmentIndex);

        OnSegmentCompleted();

        if (currentSegmentIndex >= SegmentCount)
            CompleteCurrentStep();
    }
    
    // =========================================================
    // STEP COMPLETION
    // =========================================================
    void CheckStepCompletion()
    {
        if (splineT >= 1f)
        {
            CompleteCurrentStep();
        }
    }

    void LockTool(bool locked)
    {
        surgicalTool.LockPosition(locked);
        //surgicalTool.LockPosition(false,false,false);
        
        if (locked)
        {
            if (pasoActual == Paso.Subcutanea)
            {
                
                surgicalTool.LimitRotation(ConfigurableJointMotion.Limited, ConfigurableJointMotion.Locked, ConfigurableJointMotion.Free, MaxRotationAngle);
            }
            else
            {
                surgicalTool.LimitRotation(ConfigurableJointMotion.Locked, ConfigurableJointMotion.Limited, ConfigurableJointMotion.Locked, MaxRotationAngle);                //surgicalTool.LimiteYRotation(-MaxRotationAngle,MaxRotationAngle);
            }
        }
        else
        {
            surgicalTool.ClearLimits();
        }

        toolSnapped = locked;
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
                GhostPose.gameObject.SetActive(true);
                return;
            }
        }
        else
        {
            if (endBoneFontanela != null)
                StartCoroutine(RotateBone(endBoneFontanela));
        } 
        terminado = true;
        EndStep();
    }

    public override void EndStep()
    {
        base.EndStep();
        segmentVisualizer.Clear();
    }

    void SetCurrentSpline()
    {
        currentSpline = (pasoActual == Paso.Subcutanea)
            ? subcutaneousSpline
            : fontanelleSpline;
        
        segmentVisualizer.Initialize(currentSpline, SegmentCount);
    }

    // =========================================================
    // FEEDBACK
    // =========================================================
    void UpdateVisualFeedback(float t, float precision, int nearestSegment)
    {
        // Retorno si está muy mal
        if (precision <= 0.1f)
        {
            return;
        }
        
        if (nearestSegment != currentSegmentIndex)
        {
            return;
        }

        //print("precision: "+precision);
        var deltaPrecisionBasedOnSegment = Mathf.Clamp(t, 1, precision);
        
        var deltaPrecision = deltaPrecisionBasedOnSegment * (( (float)(currentSegmentIndex + 1) / SegmentCount)  );
        // Apertura de piel
        if (pasoActual == Paso.Subcutanea)
        {
            RotateBone(endBoneSubcutanea, precision);
        }
        // Painter Brush
        else
        {
            Ray ray = new Ray(toolTip.position, Vector3.down);
            Debug.DrawRay(ray.origin, ray.direction * 0.003f, Color.green);
            /*if (Physics.Raycast(ray, out RaycastHit hit, 0.003f, fontanelleLayer))
            {
                painter.Paint(
                    hit.textureCoord,
                    paintRate * (Time.deltaTime * t)
                );
            }*/
            PlayFontanelaParticles(precision, toolTip.position);
        }
    }

    void OnSegmentCompleted()
    {
        // flash halo
        // sound
        // small particle burst
        // haptic (si aplica)
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
        errorPenalty = 0;
        tissueParticles.gameObject.SetActive(false);
        currentSegmentIndex = 0;
        segmentVisualizer.ClearCurrentSegments();
        LockTool(false);
    }
}
