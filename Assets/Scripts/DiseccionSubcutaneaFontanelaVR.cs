using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class DiseccionSubcutaneaFontanelaVR : SurgicalStep
{
    [Header("Splines")]
    public SplineContainer subcutaneousSpline;
    public SplineContainer fontanelleSpline;

    [Header("Config")]
    public float maxDistanceToSpline = 0.015f;
    public bool doSubcutaneousThenFontanelle = true;
    public float penaltyThreshold = 0.7f;
    public float maxLiftDistance = 0.003f;
    
    [Header("Ghost Guidance")]
    [SerializeField] Transform ghostPose;
    [SerializeField] float snapDistance = 0.015f;
    [SerializeField] float snapAngle = 10f;
    [SerializeField] float maxRotationAngle = 25f;
    
    [Header("Segmented Progress")]
    [SerializeField] int segmentCount = 12;
    [SerializeField] float segmentUnlockThreshold = 0.85f;
    [SerializeField] SplineSegmentVisualizer segmentVisualizer;

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
    
    private int currentSegmentIndex = 0;
    private float segmentProgress = 0f;
    bool toolSnapped;
    float splineT;
    Vector3 tipLocalOffset;
    Vector3 lastTipPos;

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
    private void EvaluateToolOnSpline()
    {
        var activeHandGestures = surgicalTool.ActiveGestures;
        
        if (activeHandGestures == null)
            return;

        if (!activeHandGestures.IsPinching)
            return;

        // ===============================
        // NEAREST POINT ON SPLINE
        // ===============================
        SplineUtility.GetNearestPoint(
            currentSpline.Spline,
            currentSpline.transform.InverseTransformPoint(toolTip.position),
            out float3 localPos,
            out float nearestT
        );

        Vector3 nearestWorldPos =
            currentSpline.transform.TransformPoint(localPos);

        float distance =
            Vector3.Distance(toolTip.position, nearestWorldPos);

        // ===============================
        // SPLINE SEGMENT LOGIC
        // ===============================
        float segmentSize = 1f / segmentCount;

        int nearestSegment =
            Mathf.FloorToInt(nearestT / segmentSize);

        nearestSegment = Mathf.Clamp(
            nearestSegment,
            0,
            segmentCount - 1
        );

        bool isCorrectSegment =
            nearestSegment == currentSegmentIndex;

        bool isAheadOfSegment =
            nearestSegment > currentSegmentIndex;

        // ===============================
        // INFLUENCE (SPATIAL)
        // ===============================
        float influence = Mathf.InverseLerp(
            maxDistanceToSpline,
            0f,
            distance
        );

        influence = Mathf.Clamp01(influence);

        // ===============================
        // PRECISION FEEDBACK
        // ===============================
        activeHandGestures.UpdatePrecision(distance);
        float precision = activeHandGestures.Precision;

        // ===============================
        // ERROR PENALTY (ORDER + FORCE)
        // ===============================
        bool pushingHardOutside =
            influence < 0.4f &&
            activeHandGestures.Pressure > 0.25f;

        if (pushingHardOutside || isAheadOfSegment)
        {
            errorPenalty += Time.deltaTime * 0.25f;
        }
        else
        {
            errorPenalty -= Time.deltaTime * 0.15f;
        }

        errorPenalty = Mathf.Clamp01(errorPenalty);

        // ===============================
        // PROGRESS FACTORS
        // ===============================
        float pressureFactor = activeHandGestures.Pressure;
        float stabilityFactor = activeHandGestures.IsStable ? 1f : 0.6f;
        float penaltyFactor = Mathf.Lerp(1f, 0.3f, errorPenalty);

        float speed = Speed;

        float deltaProgress =
            speed *
            pressureFactor *
            influence *
            stabilityFactor *
            penaltyFactor *
            Time.deltaTime;

        // ===============================
        // SEGMENTED PROGRESS
        // ===============================
        if (isCorrectSegment)
        {
            segmentProgress += deltaProgress;
            segmentProgress = Mathf.Clamp01(segmentProgress);
        }
        else if (isAheadOfSegment)
        {
            // Feedback negativo por adelantarse
            segmentProgress -= Time.deltaTime * 0.2f;
            segmentProgress = Mathf.Clamp01(segmentProgress);
        }
        //print("Segment Progress: "+segmentProgress + " and current Segment: "+currentSegmentIndex);
        // ===============================
        // VISUAL FEEDBACK
        // ===============================
        UpdateVisualFeedback(
            segmentProgress,
            precision,
            distance,
            nearestSegment
        );
        
        // ===============================
        // SEGMENT COMPLETION
        // ===============================
        if (segmentProgress >= segmentUnlockThreshold)
        {
            currentSegmentIndex++;
            segmentProgress = 0f;
            
            segmentVisualizer.UpdateVisual(currentSegmentIndex);
            // Refuerzo visual / auditivo (hook)
            OnSegmentCompleted();

            // Paso terminado
            if (currentSegmentIndex >= segmentCount)
            {
                CompleteCurrentStep();
                
            }
        }
    }
    
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
            ghostPose.position
        );

        float angDist = Quaternion.Angle(
            toolModel.rotation,
            ghostPose.rotation
        );

        if (posDist < snapDistance && angDist < snapAngle)
        {
            toolModel.SetPositionAndRotation(
                ghostPose.position,
                ghostPose.rotation
            );

            LockTool(true);
            
            ghostPose.gameObject.SetActive(false);
            
            lastTipPos = toolTip.position;
            segmentProgress = 0f;
        }
    }
    
    // =========================================================
    // ROTATION → PROGRESS
    // =========================================================
    void EvaluateGuidedRotation(IHandGestureProvider gestures)
    {
        Vector3 axis = pasoActual == Paso.Subcutanea
            ? ghostPose.right
            : ghostPose.up;

        float angle = Vector3.SignedAngle(
            ghostPose.forward,
            toolTip.forward,
            axis
        );

        float rotation01 = Mathf.InverseLerp(0f, maxRotationAngle, angle);
        rotation01 = Mathf.Clamp01(rotation01);

        Vector3 deltaTip = toolTip.position - lastTipPos;
        lastTipPos = toolTip.position;

        float lift = Vector3.Dot(deltaTip, ghostPose.up);

        // 🔥 ESCALA REALISTA
        float lift01 = Mathf.InverseLerp(0f, 0.003f, lift);
        lift01 = Mathf.Clamp01(lift01);

        float pressure = gestures.Pressure;
        float stability = gestures.IsStable ? 1f : 0.6f;

        float delta =
            Speed *
            rotation01 *
            lift01 *
            pressure *
            stability *
            Time.deltaTime;

        segmentProgress += delta;
        segmentProgress = Mathf.Clamp01(segmentProgress);

        UpdateVisualFeedback(
            segmentProgress,
            rotation01,
            lift01,
            currentSegmentIndex
        );

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

        Vector3 worldPos =
            currentSpline.transform.TransformPoint(localPos);

        Vector3 worldTangent =
            currentSpline.transform.TransformDirection(tangent);

        Quaternion targetRot = Quaternion.LookRotation(
            worldTangent,
            currentSpline.transform.up
        );

        //toolModel.rotation = ghostPose.rotation;

        // 🔹 Aplicamos offset LOCAL rotado a mundo
        toolModel.position = worldPos - toolModel.TransformVector(tipLocalOffset);

        ghostPose.position = worldPos;
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
            Mathf.FloorToInt(splineT * segmentCount);

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

        float t = (float)currentSegmentIndex / segmentCount;

        splineT = t;

        UpdateToolAlongSpline(splineT);

        segmentVisualizer.UpdateVisual(currentSegmentIndex);

        OnSegmentCompleted();

        if (currentSegmentIndex >= segmentCount)
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
        precisionHalo.gameObject.SetActive(false);
    }

    void SetCurrentSpline()
    {
        currentSpline = (pasoActual == Paso.Subcutanea)
            ? subcutaneousSpline
            : fontanelleSpline;
        
        segmentVisualizer.Initialize(currentSpline, segmentCount);
    }

    // =========================================================
    // FEEDBACK
    // =========================================================
    void UpdateVisualFeedback(float t, float precision, float distance, int nearestSegment)
    {
        if (precisionHalo == null || haloRenderer == null)
            return;

        // Retorno si está muy mal
        if (precision <= 0f)
        {
            return;
        }
        
        //precisionHalo.gameObject.SetActive(true);
        
        if (nearestSegment != currentSegmentIndex)
        {
            //precisionHalo.gameObject.SetActive(false);
            return;
        }
        
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

        //print("precision: "+precision);
        var deltaPrecision = precision * ( (float)(currentSegmentIndex + 1) / segmentCount);
        // Apertura de piel
        if (pasoActual == Paso.Subcutanea)
        {
            RotateBone(endBoneSubcutanea, deltaPrecision);
        }
        // Painter Brush
        else
        {
            Ray ray = new Ray(toolTip.position, Vector3.down);
            Debug.DrawRay(ray.origin, ray.direction * 0.003f, Color.green);
            if (Physics.Raycast(ray, out RaycastHit hit, 0.003f, fontanelleLayer))
            {
                painter.Paint(
                    hit.textureCoord,
                    paintRate * (Time.deltaTime * t)
                );
            }
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
        precisionHalo.gameObject.SetActive(false);
        errorPenalty = 0;
        tissueParticles.gameObject.SetActive(false);
        currentSegmentIndex = 0;
        segmentVisualizer.ClearCurrentSegments();
        LockTool(false);
    }
}
