using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerGestureProvider : MonoBehaviour, IHandGestureProvider
{
    [Header("Input Actions")]
    public InputActionProperty trigger; // pinch
    public InputActionProperty grip;    // grasp

    [Header("Simulation")]
    public float fakeStabilityThreshold = 0.02f;
    public float pinchSmooth = 12f;

    Vector3 lastPos;
    float velocity;
    private float pinch;

    public float Pinch => pinch;
    public bool IsPinching => trigger.action.ReadValue<float>() > 0.5f;
    public bool IsGrasping => grip.action.ReadValue<float>() > 0.5f;

    public float Pressure => trigger.action.ReadValue<float>();
    public bool IsStable => velocity < fakeStabilityThreshold;

    public float Precision { get; private set; }

    void Update()
    {
        velocity = Vector3.Distance(transform.position, lastPos) / Time.deltaTime;
        lastPos = transform.position;
        
        float rawPinch = trigger.action.ReadValue<float>();
        pinch = Mathf.Lerp(pinch, rawPinch, Time.deltaTime * pinchSmooth);
    }

    public void UpdatePrecision(float distance)
    {
        Precision = Mathf.InverseLerp(0.02f, 0f, distance);
    }
}