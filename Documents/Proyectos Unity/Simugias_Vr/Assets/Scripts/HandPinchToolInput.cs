using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Hands;

/// <summary>
/// Lee pinch de manos (XR Hands) y lo expone como Primary/Secondary
/// para que las herramientas funcionen igual que con trigger/grip.
/// Primary = pinch sostenido (usar tool). Secondary = pinch fuerte (agarrar/alternativo).
/// Umbrales con histéresis + debounce para evitar falsos agarres/soltados.
/// </summary>
public class HandPinchToolInput : MonoBehaviour, IToolInputSource
{
    [Header("Umbrales (metros) — entrada más estricta que salida")]
    [FormerlySerializedAs("pinchPrimaryMeters")]
    [SerializeField] float pinchPrimaryEnter = 0.032f;
    [SerializeField] float pinchPrimaryExit = 0.042f;
    [FormerlySerializedAs("pinchSecondaryMeters")]
    [SerializeField] float pinchSecondaryEnter = 0.022f;
    [SerializeField] float pinchSecondaryExit = 0.032f;

    [Header("Debounce")]
    [SerializeField] float stateDebounceSeconds = 0.05f;

    [SerializeField] Transform pointerOriginOverride;

    XRHandSubsystem _hands;
    bool _primaryPrev;
    bool _secondaryPrev;
    bool _primary;
    bool _secondary;
    bool _primaryRaw;
    bool _secondaryRaw;
    float _primaryTimer;
    float _secondaryTimer;
    bool _leftTracked;
    bool _rightTracked;
    float _trackingConfidence = 1f;
    Vector3 _pointerPos;
    Vector3 _pointerFwd = Vector3.forward;

    public bool AnyHandTracked => _leftTracked || _rightTracked;
    public bool LeftTracked => _leftTracked;
    public bool RightTracked => _rightTracked;
    public float TrackingConfidence => _trackingConfidence;
    public bool BothHandsTracked => _leftTracked && _rightTracked;

    public bool PrimaryDown => _primary && !_primaryPrev;
    public bool PrimaryHeld => _primary;
    public bool PrimaryUp => !_primary && _primaryPrev;
    public bool SecondaryDown => _secondary && !_secondaryPrev;
    public bool SecondaryHeld => _secondary;
    public bool SecondaryUp => !_secondary && _secondaryPrev;
    public float ScrollDelta => 0f;
    public Ray PointerRay => new Ray(
        pointerOriginOverride != null ? pointerOriginOverride.position : _pointerPos,
        pointerOriginOverride != null ? pointerOriginOverride.forward : _pointerFwd);

    void Update()
    {
        _primaryPrev = _primary;
        _secondaryPrev = _secondary;

        EnsureSubsystem();
        _primaryRaw = false;
        _secondaryRaw = false;
        _leftTracked = false;
        _rightTracked = false;
        _trackingConfidence = 0f;

        if (_hands == null || !_hands.running)
        {
            ApplyDebounced(false, false);
            return;
        }

        SampleHand(_hands.leftHand, ref _leftTracked);
        SampleHand(_hands.rightHand, ref _rightTracked);

        if (_leftTracked && _rightTracked) _trackingConfidence = 1f;
        else if (_leftTracked || _rightTracked) _trackingConfidence = 0.7f;

        ApplyDebounced(_primaryRaw, _secondaryRaw);
    }

    void ApplyDebounced(bool primaryTarget, bool secondaryTarget)
    {
        float dt = Time.deltaTime;

        if (primaryTarget == _primary)
            _primaryTimer = 0f;
        else
        {
            _primaryTimer += dt;
            if (_primaryTimer >= stateDebounceSeconds)
            {
                _primary = primaryTarget;
                _primaryTimer = 0f;
            }
        }

        if (secondaryTarget == _secondary)
            _secondaryTimer = 0f;
        else
        {
            _secondaryTimer += dt;
            if (_secondaryTimer >= stateDebounceSeconds)
            {
                _secondary = secondaryTarget;
                _secondaryTimer = 0f;
            }
        }
    }

    void EnsureSubsystem()
    {
        if (_hands != null && _hands.running)
            return;

        var list = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(list);
        _hands = null;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].running)
            {
                _hands = list[i];
                break;
            }
        }
        if (_hands == null && list.Count > 0)
            _hands = list[0];
    }

    void SampleHand(XRHand hand, ref bool trackedFlag)
    {
        if (!hand.isTracked)
            return;

        trackedFlag = true;

        if (!hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumb))
            return;
        if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose index))
            return;

        float dist = Vector3.Distance(thumb.position, index.position);

        // Histéresis: entrar estricto, salir holgado
        if (_primary)
        {
            if (dist <= pinchPrimaryExit)
                _primaryRaw = true;
        }
        else if (dist <= pinchPrimaryEnter)
        {
            _primaryRaw = true;
        }

        if (_secondary)
        {
            if (dist <= pinchSecondaryExit)
                _secondaryRaw = true;
        }
        else if (dist <= pinchSecondaryEnter)
        {
            _secondaryRaw = true;
        }

        _pointerPos = (thumb.position + index.position) * 0.5f;
        if (hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose tip)
            && hand.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out Pose prox))
        {
            Vector3 fwd = tip.position - prox.position;
            if (fwd.sqrMagnitude > 0.0001f)
                _pointerFwd = fwd.normalized;
        }
    }
}
