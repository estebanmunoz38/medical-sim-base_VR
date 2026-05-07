using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

public class XRGrabRotationLimiterTransformer : XRBaseGrabTransformer
{
    [Header("Rotation Reference")]
    public Transform rotationReference;

    [Header("Limits relative to grab pose")]
    public bool limitX = true;
    public bool limitY = true;
    public bool limitZ = true;

    public Vector2 xLimits = new Vector2(-25f, 25f);
    public Vector2 yLimits = new Vector2(-45f, 45f);
    public Vector2 zLimits = new Vector2(-20f, 20f);

    private Quaternion grabStartLocalRotation = Quaternion.identity;
    private bool hasGrabStartRotation = false;

    public override void OnGrab(XRGrabInteractable grabInteractable)
    {
        base.OnGrab(grabInteractable);

        Transform reference = GetReference(grabInteractable);

        grabStartLocalRotation =
            Quaternion.Inverse(reference.rotation) * grabInteractable.transform.rotation;

        hasGrabStartRotation = true;
    }

    public override void OnGrabCountChanged(
        XRGrabInteractable grabInteractable,
        Pose targetPose,
        Vector3 localScale
    )
    {
        base.OnGrabCountChanged(grabInteractable, targetPose, localScale);

        if (grabInteractable.interactorsSelecting.Count == 0)
        {
            hasGrabStartRotation = false;
        }
    }

    public override void Process(
        XRGrabInteractable grabInteractable,
        XRInteractionUpdateOrder.UpdatePhase updatePhase,
        ref Pose targetPose,
        ref Vector3 localScale
    )
    {
        if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic &&
            updatePhase != XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender)
        {
            return;
        }

        Transform reference = GetReference(grabInteractable);

        if (!hasGrabStartRotation)
        {
            grabStartLocalRotation =
                Quaternion.Inverse(reference.rotation) * grabInteractable.transform.rotation;

            hasGrabStartRotation = true;
        }

        Quaternion desiredLocalRotation =
            Quaternion.Inverse(reference.rotation) * targetPose.rotation;

        Quaternion deltaFromGrabStart =
            Quaternion.Inverse(grabStartLocalRotation) * desiredLocalRotation;

        Vector3 deltaEuler = deltaFromGrabStart.eulerAngles;

        deltaEuler.x = NormalizeAngle(deltaEuler.x);
        deltaEuler.y = NormalizeAngle(deltaEuler.y);
        deltaEuler.z = NormalizeAngle(deltaEuler.z);

        if (limitX)
            deltaEuler.x = Mathf.Clamp(deltaEuler.x, xLimits.x, xLimits.y);

        if (limitY)
            deltaEuler.y = Mathf.Clamp(deltaEuler.y, yLimits.x, yLimits.y);

        if (limitZ)
            deltaEuler.z = Mathf.Clamp(deltaEuler.z, zLimits.x, zLimits.y);

        Quaternion limitedDelta = Quaternion.Euler(deltaEuler);

        Quaternion limitedLocalRotation = grabStartLocalRotation * limitedDelta;

        Quaternion limitedWorldRotation = reference.rotation * limitedLocalRotation;

        targetPose.rotation = limitedWorldRotation;
    }

    private Transform GetReference(XRGrabInteractable grabInteractable)
    {
        if (rotationReference != null)
            return rotationReference;

        return grabInteractable.transform.parent != null
            ? grabInteractable.transform.parent
            : grabInteractable.transform;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}