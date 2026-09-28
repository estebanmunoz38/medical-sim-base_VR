using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DefaultExecutionOrder(20000)]
public class HandGestureGrabber : MonoBehaviour
{
    public static bool EitherPinching { get; private set; }
    public static bool EitherHandTracked { get; private set; }
    public static event Action<XRGrabInteractable> Grabbed;
    public static event Action<XRGrabInteractable> Released;

    static int pinchingHands;
    static int trackedHands;

    [SerializeField] HandTrackingBootstrap settings;
    [SerializeField] bool isLeftHand;
    [SerializeField] XRDirectInteractor interactor;

    HandTrackingSpace trackingSpace;
    Transform space;
    XRHandSubsystem subsystem;
    XRGrabInteractable held;
    Vector3 heldLocalOffset;
    Quaternion heldLocalRotation = Quaternion.identity;
    bool pinchHeld;
    bool warnedMissingSubsystem;
    bool savedKinematic;
    bool savedGravity;
    bool savedTrackPosition;
    bool savedTrackRotation;
    bool driving;

    static readonly Collider[] OverlapBuffer = new Collider[48];

    public bool IsLeftHand => isLeftHand;
    public bool IsPinching => pinchHeld;

    public void Configure(HandTrackingBootstrap bootstrap, bool left, XRDirectInteractor directInteractor, HandTrackingSpace handSpace)
    {
        settings = bootstrap;
        isLeftHand = left;
        interactor = directInteractor;
        trackingSpace = handSpace;
    }

    void OnDisable()
    {
        SetTracked(false);
        SetPinching(false);
        Release("componente deshabilitado");
    }

    void Update()
    {
        if (settings == null || interactor == null)
            return;

        if (!TryGetSubsystem(out subsystem) || !subsystem.running)
        {
            SetTracked(false);
            Release("subsistema de manos detenido");
            return;
        }

        XRHand hand = isLeftHand ? subsystem.leftHand : subsystem.rightHand;
        if (!hand.isTracked)
        {
            SetTracked(false);
            Release("mano sin tracking");
            return;
        }

        SetTracked(true);

        if (!TryGetPose(hand, XRHandJointID.Palm, out Pose palm) ||
            !TryGetPose(hand, XRHandJointID.ThumbTip, out Pose thumb) ||
            !TryGetPose(hand, XRHandJointID.IndexTip, out Pose index))
        {
            return;
        }

        float pinchDistance = Vector3.Distance(thumb.position, index.position);
        UpdateHysteresis(pinchDistance);

        if (!TryResolveSpace(palm.position))
            return;

        Vector3 thumbWorld = ToWorld(thumb.position);
        Vector3 indexWorld = ToWorld(index.position);
        Quaternion palmRotation = space.rotation * palm.rotation;
        Vector3 pinchPoint = (thumbWorld + indexWorld) * 0.5f;
        transform.SetPositionAndRotation(pinchPoint, palmRotation);

        if (held != null)
        {
            if (!held.isActiveAndEnabled)
            {
                XRGrabInteractable previous = held;
                Release("objeto desactivado");
                if (pinchHeld)
                    TryGrab(pinchPoint, palmRotation, previous);
                return;
            }

            if (!pinchHeld)
                Release("gesto abierto");
            return;
        }

        if (!pinchHeld)
            return;

        TryGrab(pinchPoint, palmRotation, null);
    }

    void LateUpdate()
    {
        if (!driving || held == null)
            return;

        if (!TryResolveSpace(Vector3.zero) && space == null)
            return;

        XRHand hand = isLeftHand ? subsystem.leftHand : subsystem.rightHand;
        if (!hand.isTracked || !TryGetPose(hand, XRHandJointID.Palm, out Pose palm) ||
            !TryGetPose(hand, XRHandJointID.ThumbTip, out Pose thumb) ||
            !TryGetPose(hand, XRHandJointID.IndexTip, out Pose index))
        {
            return;
        }

        Quaternion palmRotation = space.rotation * palm.rotation;
        Vector3 pinchPoint = (ToWorld(thumb.position) + ToWorld(index.position)) * 0.5f;
        Vector3 position = pinchPoint + palmRotation * heldLocalOffset;
        Quaternion rotation = palmRotation * heldLocalRotation;
        held.transform.SetPositionAndRotation(position, rotation);

        Rigidbody body = held.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = position;
            body.rotation = rotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    void TryGrab(Vector3 pinchPoint, Quaternion palmRotation, XRGrabInteractable prefer)
    {
        XRGrabInteractable candidate = FindCandidate(pinchPoint);
        if (prefer != null && prefer.isActiveAndEnabled && !prefer.isSelected &&
            DistanceToTool(prefer, pinchPoint) <= settings.pinchGrabRadius)
            candidate = prefer;
        if (candidate == null)
            return;

        interactor.StartManualInteraction((IXRSelectInteractable)candidate);
        if (!interactor.isPerformingManualInteraction && !candidate.isSelected)
            return;

        held = candidate;
        savedTrackPosition = candidate.trackPosition;
        savedTrackRotation = candidate.trackRotation;
        candidate.trackPosition = false;
        candidate.trackRotation = false;
        candidate.throwOnDetach = false;
        candidate.forceGravityOnDetach = false;

        Rigidbody body = candidate.GetComponent<Rigidbody>();
        if (body != null)
        {
            savedKinematic = body.isKinematic;
            savedGravity = body.useGravity;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.useGravity = false;
        }

        heldLocalRotation = Quaternion.Inverse(palmRotation) * candidate.transform.rotation;
        heldLocalOffset = Quaternion.Inverse(palmRotation) * (candidate.transform.position - pinchPoint);
        driving = true;
        Grabbed?.Invoke(candidate);
        HandTrackingLog.Write("Grab",
            $"{HandName()} agarró {candidate.name} por el mango. Gesto=pellizco.");
    }

    void UpdateHysteresis(float pinchDistance)
    {
        bool was = pinchHeld;
        if (!pinchHeld && pinchDistance <= settings.pinchOnMeters)
            pinchHeld = true;
        else if (pinchHeld && pinchDistance >= settings.pinchOffMeters)
            pinchHeld = false;

        if (was != pinchHeld)
            SetPinching(pinchHeld);
    }

    XRGrabInteractable FindCandidate(Vector3 pinchPoint)
    {
        int count = Physics.OverlapSphereNonAlloc(
            pinchPoint,
            settings.pinchGrabRadius,
            OverlapBuffer,
            settings.grabLayers,
            QueryTriggerInteraction.Collide);

        XRGrabInteractable best = null;
        float bestDistance = settings.pinchGrabRadius;
        for (int i = 0; i < count; i++)
        {
            Collider col = OverlapBuffer[i];
            if (col == null || !col.enabled || col.name == "GrabZone")
                continue;

            XRGrabInteractable grab = col.GetComponentInParent<XRGrabInteractable>();
            if (grab == null || !grab.isActiveAndEnabled || grab.isSelected)
                continue;

            float distance = DistanceToCollider(col, pinchPoint);
            if (distance > bestDistance)
                continue;

            best = grab;
            bestDistance = distance;
        }

        return best;
    }

    static float DistanceToTool(XRGrabInteractable grab, Vector3 pinchPoint)
    {
        Collider[] colliders = grab.GetComponentsInChildren<Collider>(false);
        float best = float.MaxValue;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || !col.enabled || col.name == "GrabZone")
                continue;
            float distance = DistanceToCollider(col, pinchPoint);
            if (distance < best)
                best = distance;
        }

        return best;
    }

    static float DistanceToCollider(Collider col, Vector3 pinchPoint)
    {
        Vector3 closest = col.ClosestPoint(pinchPoint);
        float distance = Vector3.Distance(pinchPoint, closest);
        Vector3 extents = col.bounds.extents;
        float largestExtent = Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z));
        if (largestExtent <= 0.2f && distance > 0.0005f)
            return distance;

        Renderer renderer = col.GetComponent<Renderer>();
        if (renderer == null)
            renderer = col.GetComponentInChildren<Renderer>();
        if (renderer == null)
            renderer = col.GetComponentInParent<Renderer>();
        if (renderer != null)
            return Mathf.Max(distance, DistanceToBounds(renderer.bounds, pinchPoint));

        return Mathf.Max(distance, DistanceToBounds(col.bounds, pinchPoint));
    }

    static float DistanceToBounds(Bounds bounds, Vector3 point)
    {
        Vector3 closest = bounds.ClosestPoint(point);
        return Vector3.Distance(point, closest);
    }

    static Transform GripPoint(XRGrabInteractable grab)
    {
        if (grab.attachTransform != null)
            return grab.attachTransform;

        Transform named = FindNamedGrip(grab.transform);
        return named != null ? named : grab.transform;
    }

    static Transform FindNamedGrip(Transform root)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string name = children[i].name;
            if (name == "AttachPoint" || name == "HandAttach" || name == "Mango" || name == "Handle")
                return children[i];
        }

        return null;
    }

    static bool TryGetPose(XRHand hand, XRHandJointID jointId, out Pose pose)
    {
        pose = default;
        return hand.GetJoint(jointId).TryGetPose(out pose);
    }

    bool TryGetSubsystem(out XRHandSubsystem handSubsystem)
    {
        if (subsystem != null && subsystem.running)
        {
            handSubsystem = subsystem;
            return true;
        }

        subsystem = HandTrackingBootstrap.RunningHands();
        if (subsystem == null)
        {
            if (!warnedMissingSubsystem)
            {
                warnedMissingSubsystem = true;
                HandTrackingLog.Write("HandTracking",
                    "XRHandSubsystem no está en ejecución. Sin visor no hay agarre.");
            }

            handSubsystem = null;
            return false;
        }

        warnedMissingSubsystem = false;
        handSubsystem = subsystem;
        return true;
    }

    public static void ResetSignals()
    {
        pinchingHands = 0;
        trackedHands = 0;
        EitherPinching = false;
        EitherHandTracked = false;
    }

    void Release(string reason)
    {
        if (held == null && (interactor == null || !interactor.isPerformingManualInteraction))
            return;

        XRGrabInteractable released = held;
        string objectName = released != null ? released.name : "desconocido";
        driving = false;

        if (released != null)
        {
            released.trackPosition = savedTrackPosition;
            released.trackRotation = savedTrackRotation;
            released.throwOnDetach = false;
            released.forceGravityOnDetach = false;
        }

        if (interactor != null && interactor.isPerformingManualInteraction)
            interactor.EndManualInteraction();

        if (released != null)
        {
            Rigidbody body = released.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = savedKinematic;
                body.useGravity = savedGravity;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        held = null;
        HandTrackingLog.Write("Grab", $"{HandName()} soltó {objectName}. Motivo={reason}.");
        if (released != null)
            Released?.Invoke(released);
    }

    bool TryResolveSpace(Vector3 palmLocal)
    {
        if (trackingSpace == null || !trackingSpace.TryGet(palmLocal, out Transform resolved))
            return false;

        space = resolved;
        return true;
    }

    Vector3 ToWorld(Vector3 originLocalPoint)
    {
        return space != null ? space.TransformPoint(originLocalPoint) : originLocalPoint;
    }

    void SetTracked(bool tracked)
    {
        if (tracked)
            trackedHands |= isLeftHand ? 1 : 2;
        else
            trackedHands &= ~(isLeftHand ? 1 : 2);
        EitherHandTracked = trackedHands != 0;
    }

    void SetPinching(bool pinching)
    {
        if (pinching)
            pinchingHands |= isLeftHand ? 1 : 2;
        else
            pinchingHands &= ~(isLeftHand ? 1 : 2);
        EitherPinching = pinchingHands != 0;
    }

    string HandName()
    {
        return isLeftHand ? "Mano izquierda" : "Mano derecha";
    }
}
