using System;
using UnityEngine;


public class HandGestureProvider : MonoBehaviour, IHandGestureProvider
{
    #region Fields
    [Header("Hand Joints")]
    public Transform thumbTip;
    public Transform indexTip;
    public Transform middleTip;
    public Transform palm;

    [Header("Pinch")]
    public float pinchMaxDistance = 0.035f;
    public float pinchActivation = 0.7f;
    public float secondaryPinchActivation = 0.75f;
    
    [Header("Grasp")]
    public float graspActivation = 0.9f;

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
    public float SecondaryPinch { get; private set; }
    public bool IsPinching => Pinch >= pinchActivation;
    public bool IsGrasping => Pinch >= graspActivation;
    public float ScrubSpeed { get; private set; }
    public float Pressure { get; private set; }       // 0..1
    public bool IsStable { get; private set; }
    public float Precision { get; private set; }      // 0..1

    public Vector3 PalmNormal => palm.forward;

    // =====================
    // Eventos
    // =====================
    public event Action OnSecondaryActivated;
    public event Action OnSecondaryDeactivated;
    
    // =====================
    // Internos
    // =====================
    Vector3 lastIndexPos;
    Vector3 velocityAccumulator;
    float stableTimer;
    float smoothedPressure;
    bool secondaryWasActive;
    #endregion

    #region Unity Methods
    void Start()
    {
        lastIndexPos = indexTip.position;
    }

    void Update()
    {
        UpdatePinch();
        UpdateSecondaryPinch();
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
        float rawPinch = 1f - Mathf.Clamp01(d / pinchMaxDistance);
        Pinch = Mathf.Lerp(Pinch, rawPinch, Time.deltaTime * 12f);
    }
    
    void UpdateSecondaryPinch()
    {
        float d = Vector3.Distance(thumbTip.position, middleTip.position);
        float rawPinch = 1f - Mathf.Clamp01(d / pinchMaxDistance);
        SecondaryPinch = Mathf.Lerp(SecondaryPinch, rawPinch, Time.deltaTime * 12f);

        bool isActive = SecondaryPinch >= secondaryPinchActivation;

        // Edge detection
        if (isActive && !secondaryWasActive)
            OnSecondaryActivated?.Invoke();
        else if (!isActive && secondaryWasActive)
            OnSecondaryDeactivated?.Invoke();

        secondaryWasActive = isActive;
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

        Pressure = Mathf.Clamp01(smoothedPressure);
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
