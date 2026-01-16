using UnityEngine;
using UnityEngine.XR.Hands;

using UnityEngine.SubsystemsImplementation;
using UnityEngine.Subsystems;

public class HandPinchGrabDriver : MonoBehaviour
{
    [Header("References")]
    public XRHandSubsystem handSubsystem;
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor directInteractor;

    [Header("Hand Settings")]
    public bool isLeftHand = true;

    [Header("Pinch Settings")]
    [Range(0.01f, 0.10f)]
    public float pinchThresholdMeters = 0.03f;

    private bool _isPinching;

    void Awake()
    {
        if (directInteractor == null)
            directInteractor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();

        if (handSubsystem == null)
        {
            var subsystems = new System.Collections.Generic.List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);

            if (subsystems.Count > 0)
                handSubsystem = subsystems[0];
        }
    }

    void Update()
    {
        if (handSubsystem == null)
            return;

        if (!handSubsystem.running)
            return;

        XRHand hand = isLeftHand ? handSubsystem.leftHand : handSubsystem.rightHand;

        if (!hand.isTracked)
        {
            SetPinch(false);
            return;
        }

        bool pinchNow = DetectPinchByTipDistance(hand);

        if (pinchNow && !_isPinching)
            SetPinch(true);
        else if (!pinchNow && _isPinching)
            SetPinch(false);
    }

    bool DetectPinchByTipDistance(XRHand hand)
    {
        if (!hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbPose))
            return false;

        if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexPose))
            return false;

        float dist = Vector3.Distance(thumbPose.position, indexPose.position);
        return dist <= pinchThresholdMeters;
    }

    void SetPinch(bool pinching)
    {
        _isPinching = pinching;

        if (!pinching)
        {
            // Forzar soltar (MVP seguro)
            directInteractor.enabled = false;
            directInteractor.enabled = true;
        }
        else
        {
            if (!directInteractor.enabled)
                directInteractor.enabled = true;
        }
    }
}
