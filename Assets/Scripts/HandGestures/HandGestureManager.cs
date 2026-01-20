using UnityEngine;

public class HandGestureManager : MonoBehaviour
{
    #region Fields
    [Header("Hand Joints")]
    public Transform thumbTip;
    public Transform indexTip;
    public Transform palm;

    [Header("Pinch")]
    public float pinchMaxDistance = 0.035f;
    public float pinchActivation = 0.7f;

    [Header("Pressure")]
    public float maxScrubSpeed = 0.12f;
    public float pressureSmooth = 8f;

    [Header("Stability")]
    public float stabilityThreshold = 0.002f;
    public float stabilityWindow = 0.15f;

    [Header("Precision")]
    public float maxAllowedError = 0.02f;

    // =====================
    // OUTPUT (API pública)
    // =====================
    public float Pinch { get; private set; }          // 0..1
    public bool IsPinching => Pinch >= pinchActivation;

    public float ScrubSpeed { get; private set; }
    public float Pressure { get; private set; }       // 0..1
    public bool IsStable { get; private set; }
    public float Precision { get; private set; }      // 0..1

    public Vector3 PalmNormal => palm.forward;

    // =====================
    // Internos
    // =====================
    Vector3 lastIndexPos;
    Vector3 velocityAccumulator;
    float stableTimer;
    float smoothedPressure;
    #endregion

    #region Unity Methods
    void Start()
    {
        lastIndexPos = indexTip.position;
    }

    void Update()
    {
        UpdatePinch();
        UpdateMotion();
        UpdateStability();
        UpdatePressure();
    }
    #endregion

    #region Private Methods
    // =====================
    // PINCH
    // =====================
    void UpdatePinch()
    {
        float d = Vector3.Distance(thumbTip.position, indexTip.position);
        Pinch = 1f - Mathf.Clamp01(d / pinchMaxDistance);
    }

    // =====================
    // MOVIMIENTO / SCRUB
    // =====================
    void UpdateMotion()
    {
        Vector3 delta = indexTip.position - lastIndexPos;
        ScrubSpeed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        velocityAccumulator = Vector3.Lerp(
            velocityAccumulator,
            delta,
            Time.deltaTime * 10f
        );

        lastIndexPos = indexTip.position;
    }

    // =====================
    // ESTABILIDAD
    // =====================
    void UpdateStability()
    {
        if (velocityAccumulator.magnitude < stabilityThreshold)
        {
            stableTimer += Time.deltaTime;
            IsStable = stableTimer >= stabilityWindow;
        }
        else
        {
            stableTimer = 0f;
            IsStable = false;
        }
    }

    // =====================
    // PRESIÓN
    // =====================
    void UpdatePressure()
    {
        float motionFactor = Mathf.Clamp01(ScrubSpeed / maxScrubSpeed);
        float stabilityFactor = IsStable ? 1f : 0.5f;

        float target = motionFactor * stabilityFactor * Pinch;
        smoothedPressure = Mathf.Lerp(
            smoothedPressure,
            target,
            Time.deltaTime * pressureSmooth
        );

        Pressure = smoothedPressure;
    }
    #endregion

    #region Public Methods
    // =====================
    // PRECISIÓN (externa)
    // =====================
    public void UpdatePrecision(float distanceToIdeal)
    {
        Precision = 1f - Mathf.Clamp01(distanceToIdeal / maxAllowedError);
    }
    #endregion
}
