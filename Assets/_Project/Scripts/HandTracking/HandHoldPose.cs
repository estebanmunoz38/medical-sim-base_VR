using UnityEngine;
using UnityEngine.XR.Hands;

public static class HandHoldPoseSignal
{
    static float leftTarget;
    static float rightTarget;

    public static void Set(bool left, float weight)
    {
        if (left)
            leftTarget = weight;
        else
            rightTarget = weight;
    }

    public static float Target(bool left)
    {
        return left ? leftTarget : rightTarget;
    }
}

/// <summary>
/// Después de que el esqueleto copia el tracking, cierra un poco los dedos
/// mientras esa mano sostiene una herramienta. La muñeca y la palma no se tocan.
/// </summary>
public class HandHoldPose : MonoBehaviour
{
    XRHandTrackingEvents events;
    Transform[] joints;
    bool left;
    float shown;
    bool listening;

    void OnEnable()
    {
        events = GetComponent<XRHandTrackingEvents>();
        XRHandSkeletonDriver driver = GetComponent<XRHandSkeletonDriver>();
        if (events == null || driver == null)
            return;

        left = events.handedness == Handedness.Left;
        Cache(driver);
        events.jointsUpdated.AddListener(OnJoints);
        listening = true;
    }

    void OnDisable()
    {
        if (!listening || events == null)
            return;
        events.jointsUpdated.RemoveListener(OnJoints);
        listening = false;
    }

    void Cache(XRHandSkeletonDriver driver)
    {
        joints = new Transform[32];
        var refs = driver.jointTransformReferences;
        for (int i = 0; i < refs.Count; i++)
        {
            int index = refs[i].xrHandJointID.ToIndex();
            if (index < 0 || index >= joints.Length)
                continue;
            joints[index] = refs[i].jointTransform;
        }
    }

    void OnJoints(XRHandJointsUpdatedEventArgs args)
    {
        float target = HandHoldPoseSignal.Target(left);
        float dt = Mathf.Max(Time.deltaTime, 0.001f);
        float tau = target > shown ? 0.07f : 0.09f;
        shown = Mathf.Lerp(shown, target, 1f - Mathf.Exp(-dt / tau));
        if (shown < 0.02f)
            return;

        Transform wrist = Joint(XRHandJointID.Wrist);
        Transform palm = Joint(XRHandJointID.Palm);
        Transform middle = Joint(XRHandJointID.MiddleMetacarpal);
        Transform thumb = Joint(XRHandJointID.ThumbMetacarpal);
        if (wrist == null || palm == null || middle == null || thumb == null)
            return;

        Vector3 fingers = middle.position - wrist.position;
        Vector3 thumbDir = thumb.position - palm.position;
        if (fingers.sqrMagnitude < 0.0004f || thumbDir.sqrMagnitude < 0.0001f)
            return;
        fingers.Normalize();
        thumbDir = Vector3.ProjectOnPlane(thumbDir, fingers);
        if (thumbDir.sqrMagnitude < 0.0001f)
            return;
        thumbDir.Normalize();
        Vector3 palmar = Vector3.Cross(fingers, thumbDir);
        if (left)
            palmar = -palmar;
        if (palmar.sqrMagnitude < 0.0001f)
            return;
        palmar.Normalize();

        Curl(XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, palmar, 18f);
        Curl(XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, palmar, 22f);
        Curl(XRHandJointID.IndexDistal, XRHandJointID.IndexTip, palmar, 12f);
        Curl(XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, palmar, 32f);
        Curl(XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, palmar, 42f);
        Curl(XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip, palmar, 22f);
        Curl(XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, palmar, 30f);
        Curl(XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, palmar, 40f);
        Curl(XRHandJointID.RingDistal, XRHandJointID.RingTip, palmar, 20f);
        Curl(XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, palmar, 26f);
        Curl(XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, palmar, 34f);
        Curl(XRHandJointID.LittleDistal, XRHandJointID.LittleTip, palmar, 16f);
        Curl(XRHandJointID.ThumbProximal, XRHandJointID.ThumbDistal, palmar, 14f);
        Curl(XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip, palmar, 18f);
    }

    void Curl(XRHandJointID jointId, XRHandJointID childId, Vector3 palmar, float degrees)
    {
        Transform joint = Joint(jointId);
        Transform child = Joint(childId);
        if (joint == null || child == null)
            return;

        Vector3 bone = child.position - joint.position;
        if (bone.sqrMagnitude < 0.0000001f)
            return;
        bone.Normalize();
        float room = Mathf.Clamp01(0.65f - Vector3.Dot(bone, palmar));
        float angle = degrees * shown * room;
        if (angle < 0.4f)
            return;

        Vector3 axis = Vector3.Cross(bone, palmar);
        if (axis.sqrMagnitude < 0.000001f)
            return;
        joint.rotation = Quaternion.AngleAxis(angle, axis.normalized) * joint.rotation;
    }

    Transform Joint(XRHandJointID id)
    {
        int index = id.ToIndex();
        if (joints == null || index < 0 || index >= joints.Length)
            return null;
        return joints[index];
    }
}
