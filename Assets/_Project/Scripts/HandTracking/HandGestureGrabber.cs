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
    public struct HandSample
    {
        public bool valid;
        public bool pinching;
        public Vector3 pinch;
    }

    public static bool EitherPinching { get; private set; }
    public static bool EitherHandTracked { get; private set; }
    public static HandSample LeftSample { get; private set; }
    public static HandSample RightSample { get; private set; }
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
    ToolHandGrip heldGrip;
    Vector3 heldLocalOffset;
    Quaternion heldLocalRotation = Quaternion.identity;
    bool pinchHeld;
    bool warnedMissingSubsystem;
    bool savedKinematic;
    bool savedGravity;
    bool savedTrackPosition;
    bool savedTrackRotation;
    RigidbodyInterpolation savedInterpolation;
    bool driving;
    float trackingLostFor;
    float jointMissingFor;
    float releaseTimer;
    float confirmTimer;
    XRGrabInteractable confirmTarget;
    ToolHandGrip confirmGrip;
    bool frameCalibrated;
    float axisX = 1f;
    float axisY = 1f;
    Vector3 snapFromPosition;
    Quaternion snapFromRotation;
    float snapT;
    bool snapping;
    bool recovering;
    float recoverT;
    Vector3 recoverFromPosition;
    Quaternion recoverFromRotation;
    bool hasReliableTarget;
    Vector3 reliablePosition;
    Quaternion reliableRotation;
    bool hasPendingOutlier;
    Vector3 pendingOutlierPosition;
    Quaternion pendingOutlierRotation;
    int outlierStreak;
    bool filterSeeded;
    bool gripCaptured;
    Vector3 gripLocalPosition;
    Quaternion gripLocalRotation = Quaternion.identity;
    Vector3 tipLocal;
    bool hasTipLocal;
    Draw contactDraw;
    bool contactActive;
    bool hasContactTip;
    Vector3 contactTip;
    Quaternion shownRotation = Quaternion.identity;
    bool hasLastPinch;
    Vector3 lastPinchWorld;
    Rigidbody heldBody;
    OneEuroVector3 positionFilter;
    OneEuroQuaternion rotationFilter;

    static readonly Collider[] OverlapBuffer = new Collider[48];
    static readonly RaycastHit[] ContactHits = new RaycastHit[24];

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
        HandHoldPoseSignal.Set(isLeftHand, 0f);
        SetTracked(false);
        SetPinching(false);
        PublishSample(false, false, Vector3.zero);
        Release("componente deshabilitado");
    }

    void Update()
    {
        if (settings == null || interactor == null)
            return;

        if (!TryGetSubsystem(out subsystem) || !subsystem.running)
        {
            SetTracked(false);
            PublishSample(false, false, Vector3.zero);
            Release("subsistema de manos detenido");
            return;
        }

        XRHand hand = isLeftHand ? subsystem.leftHand : subsystem.rightHand;
        if (driving && held == null)
        {
            Release("referencia perdida");
            return;
        }

        if (!hand.isTracked || !TryGetPose(hand, XRHandJointID.Palm, out Pose palm))
        {
            PublishSample(false, false, Vector3.zero);
            if (GripRules.ReleaseImmediatelyOnLoss(driving, releaseTimer))
            {
                Release("gesto abierto");
                return;
            }

            if (driving && trackingLostFor <= 0f && heldGrip != null)
                heldGrip.NoteTrackingLoss(isLeftHand);

            float grace = AnchoredGrace();
            if (GripRules.HoldDuringLoss(driving, trackingLostFor, grace))
            {
                trackingLostFor += Time.deltaTime;
                ReportPhase(ToolHandGrip.GripPhase.Gracia, false, grace - trackingLostFor);
                return;
            }

            SetTracked(false);
            Release("mano sin tracking");
            return;
        }

        if (driving && trackingLostFor > 0f && heldGrip != null)
            heldGrip.NoteTrackingRecovered(isLeftHand);
        trackingLostFor = 0f;
        SetTracked(true);

        bool thumbOk = TryGetPose(hand, XRHandJointID.ThumbTip, out Pose thumb);
        bool indexOk = TryGetPose(hand, XRHandJointID.IndexTip, out Pose index);
        bool tipsOk = thumbOk && indexOk;
        bool followPalmThroughOcclusion = driving && PalmDrivesHeldPose();
        if (!tipsOk && !followPalmThroughOcclusion)
        {
            PublishSample(false, pinchHeld, Vector3.zero);
            if (driving && heldGrip != null && heldGrip.mode == ToolHandGrip.GripMode.Anchored)
            {
                if (jointMissingFor <= 0f)
                    heldGrip.NoteTrackingLoss(isLeftHand);
                jointMissingFor += Time.deltaTime;
                float grace = AnchoredGrace();
                if (jointMissingFor < grace)
                {
                    ReportPhase(ToolHandGrip.GripPhase.Gracia, false, grace - jointMissingFor);
                    return;
                }

                Release("articulaciones perdidas");
            }

            return;
        }

        if (!tipsOk)
        {
            if (jointMissingFor <= 0f && heldGrip != null)
                heldGrip.NoteFingerOcclusion(isLeftHand);
            jointMissingFor += Time.deltaTime;
        }
        else
        {
            jointMissingFor = 0f;
            UpdateHysteresis(Vector3.Distance(thumb.position, index.position));
        }

        if (!TryResolveSpace(palm.position))
        {
            PublishSample(false, pinchHeld, Vector3.zero);
            return;
        }

        Quaternion palmRotation = space.rotation * palm.rotation;
        Vector3 pinchPoint = hasLastPinch ? lastPinchWorld : ToWorld(palm.position);
        if (tipsOk)
        {
            pinchPoint = (ToWorld(thumb.position) + ToWorld(index.position)) * 0.5f;
            lastPinchWorld = pinchPoint;
            hasLastPinch = true;
            if (!pinchHeld &&
                TryGetPose(hand, XRHandJointID.Wrist, out Pose wrist) &&
                TryGetPose(hand, XRHandJointID.MiddleProximal, out Pose middle) &&
                TryGetPose(hand, XRHandJointID.ThumbMetacarpal, out Pose thumbBase))
            {
                CalibrateFrame(palmRotation, ToWorld(palm.position), ToWorld(wrist.position), ToWorld(middle.position), ToWorld(thumbBase.position));
            }
        }

        transform.SetPositionAndRotation(tipsOk ? pinchPoint : ToWorld(palm.position), palmRotation);
        PublishSample(tipsOk, pinchHeld, pinchPoint);

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
            {
                releaseTimer += Time.deltaTime;
                float needed = heldGrip != null && heldGrip.mode == ToolHandGrip.GripMode.Anchored
                    ? heldGrip.ReleaseDelaySeconds()
                    : 0.02f;
                ReportPhase(ToolHandGrip.GripPhase.Soltando, true, 0f);
                if (releaseTimer >= needed)
                    Release("gesto abierto");
            }
            else
            {
                releaseTimer = 0f;
            }

            return;
        }

        if (!tipsOk || !pinchHeld)
        {
            confirmTarget = null;
            confirmGrip = null;
            confirmTimer = 0f;
            return;
        }

        XRGrabInteractable candidate = FindCandidate(pinchPoint);
        if (candidate != confirmTarget)
        {
            confirmTarget = candidate;
            confirmGrip = candidate != null ? candidate.GetComponent<ToolHandGrip>() : null;
            confirmTimer = 0f;
        }

        if (candidate == null)
            return;

        float confirm = confirmGrip != null && confirmGrip.mode == ToolHandGrip.GripMode.Anchored
            ? Mathf.Max(0f, confirmGrip.grabConfirmSeconds)
            : 0f;
        confirmTimer += Time.deltaTime;
        if (confirmTimer < confirm)
            return;

        TryGrab(pinchPoint, palmRotation, null);
    }

    public static float NearestPinch(Vector3 worldPoint, float nearRadius, out bool pinchingNear)
    {
        pinchingNear = false;
        float best = float.MaxValue;
        ConsiderSample(LeftSample, worldPoint, nearRadius, ref best, ref pinchingNear);
        ConsiderSample(RightSample, worldPoint, nearRadius, ref best, ref pinchingNear);
        return best;
    }

    static void ConsiderSample(HandSample sample, Vector3 worldPoint, float nearRadius, ref float best, ref bool pinchingNear)
    {
        if (!sample.valid)
            return;
        float distance = Vector3.Distance(sample.pinch, worldPoint);
        if (distance < best)
            best = distance;
        if (sample.pinching && distance <= nearRadius)
            pinchingNear = true;
    }

    void LateUpdate()
    {
        HandHoldPoseSignal.Set(isLeftHand, driving && held != null ? 1f : 0f);
        if (!driving || held == null)
            return;

        if (!TryResolveSpace(Vector3.zero) && space == null)
            return;

        XRHand hand = isLeftHand ? subsystem.leftHand : subsystem.rightHand;
        if (!hand.isTracked || !TryGetPose(hand, XRHandJointID.Palm, out Pose palm))
            return;

        bool thumbOk = TryGetPose(hand, XRHandJointID.ThumbTip, out Pose thumb);
        bool indexOk = TryGetPose(hand, XRHandJointID.IndexTip, out Pose index);
        bool tipsOk = thumbOk && indexOk;
        if (!tipsOk && !PalmDrivesHeldPose())
            return;

        Quaternion rawPalm = space.rotation * palm.rotation;
        Vector3 pinchPoint = tipsOk
            ? (ToWorld(thumb.position) + ToWorld(index.position)) * 0.5f
            : ToWorld(palm.position);
        Vector3 position;
        Quaternion rotation;
        if (!TryAnchoredPose(hand, HandFrame(rawPalm), pinchPoint, out position, out rotation))
        {
            position = pinchPoint + rawPalm * heldLocalOffset;
            rotation = rawPalm * heldLocalRotation;
        }

        ApplyHoldPose(position, rotation);
    }

    bool TryAnchoredPose(XRHand hand, Quaternion palmRotation, Vector3 pinchPoint, out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = default;
        if (heldGrip == null || heldGrip.mode != ToolHandGrip.GripMode.Anchored)
            return false;

        Vector3 indexDirection = Vector3.zero;
        if (!heldGrip.lockPoseWhileHeld && heldGrip.indexAim > 0.001f &&
            TryGetPose(hand, XRHandJointID.IndexProximal, out Pose indexBase) &&
            TryGetPose(hand, XRHandJointID.IndexTip, out Pose indexTip))
            indexDirection = ToWorld(indexTip.position) - ToWorld(indexBase.position);

        if (!TryGetPose(hand, XRHandJointID.Palm, out Pose palm))
            return false;
        Vector3 palmPosition = ToWorld(palm.position);

        float dt = Time.deltaTime;
        Quaternion gatedRotation = palmRotation;
        Vector3 gatedPosition = StabilizeTarget(palmPosition, ref gatedRotation, dt);
        if (!filterSeeded)
        {
            positionFilter.Reset(gatedPosition);
            rotationFilter.Reset(gatedRotation);
            filterSeeded = true;
        }

        Vector3 stablePosition = positionFilter.Filter(gatedPosition, dt, heldGrip.positionMinCutoff, heldGrip.positionBeta, heldGrip.positionDeadzone);
        Quaternion stableRotation = rotationFilter.Filter(gatedRotation, dt, heldGrip.rotationMinCutoff, heldGrip.rotationBeta, heldGrip.rotationDeadzone);
        if (!gripCaptured)
        {
            heldGrip.Evaluate(new Pose(stablePosition, stableRotation), pinchPoint, indexDirection, isLeftHand, out Vector3 authoredPosition, out Quaternion authoredRotation);
            gripLocalPosition = Quaternion.Inverse(stableRotation) * (authoredPosition - stablePosition);
            gripLocalRotation = Quaternion.Inverse(stableRotation) * authoredRotation;
            gripCaptured = true;
        }

        Vector3 targetPosition = stablePosition + stableRotation * gripLocalPosition;
        Quaternion targetRotation = stableRotation * gripLocalRotation;

        if (snapping)
        {
            float duration = Mathf.Max(0.04f, heldGrip.snapSeconds);
            snapT += dt / duration;
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(snapT));
            position = Vector3.Lerp(snapFromPosition, targetPosition, blend);
            rotation = Quaternion.Slerp(snapFromRotation, targetRotation, blend);
            if (snapT >= 1f)
                snapping = false;
        }
        else if (recovering)
        {
            recoverT += dt / 0.1f;
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(recoverT));
            position = Vector3.Lerp(recoverFromPosition, targetPosition, blend);
            rotation = Quaternion.Slerp(recoverFromRotation, targetRotation, blend);
            if (recoverT >= 1f)
                recovering = false;
        }
        else
        {
            position = targetPosition;
            rotation = targetRotation;
        }

        if (!snapping)
            ApplyContact(ref position, ref rotation, dt);
        else if (heldGrip != null)
            heldGrip.ReportContact(0f, 0f);

        if (heldGrip.debugPose || heldGrip.recordSession)
        {
            float posError = Vector3.Distance(stablePosition, gatedPosition);
            float angError = Quaternion.Angle(stableRotation, gatedRotation);
            if (!snapping && !recovering)
            {
                if (heldGrip.recordSession)
                    GripSessionLog.Sample(heldGrip, posError * 1000f, angError);
                if (heldGrip.debugPose)
                    heldGrip.NoteQuiet(position, rotation, targetPosition, targetRotation, dt);
            }

            if (heldGrip.debugPose)
                heldGrip.Report(recovering ? ToolHandGrip.GripPhase.Gracia : ToolHandGrip.GripPhase.Tomada, HandName(), true, 0f, posError, angError);
        }

        return true;
    }

    Vector3 StabilizeTarget(Vector3 targetPosition, ref Quaternion targetRotation, float dt)
    {
        if (!hasReliableTarget)
        {
            hasReliableTarget = true;
            reliablePosition = targetPosition;
            reliableRotation = targetRotation;
            return targetPosition;
        }

        GripRules.StepLimits(heldGrip.maxLinearSpeed, heldGrip.maxAngularSpeed, dt, out float stepLimit, out float angleLimit);
        float step = Vector3.Distance(targetPosition, reliablePosition);
        float angle = Quaternion.Angle(targetRotation, reliableRotation);
        if (GripRules.PoseStepPlausible(step, angle, heldGrip.maxLinearSpeed, heldGrip.maxAngularSpeed, dt))
        {
            outlierStreak = 0;
            hasPendingOutlier = false;
            reliablePosition = targetPosition;
            reliableRotation = targetRotation;
            return targetPosition;
        }

        bool settled = hasPendingOutlier &&
                       Vector3.Distance(targetPosition, pendingOutlierPosition) < 0.02f &&
                       Quaternion.Angle(targetRotation, pendingOutlierRotation) < 10f;
        bool continuous = hasPendingOutlier &&
                          Vector3.Distance(targetPosition, pendingOutlierPosition) <= stepLimit * 1.8f &&
                          Quaternion.Angle(targetRotation, pendingOutlierRotation) <= angleLimit * 1.8f;
        bool popped = step > 0.08f || angle > 45f;
        if (settled && popped)
        {
            recoverFromPosition = held != null ? held.transform.position : reliablePosition;
            recoverFromRotation = held != null ? held.transform.rotation : reliableRotation;
            recovering = true;
            recoverT = 0f;
            snapping = false;
            AcceptReliable(targetPosition, targetRotation, true);
            if (heldGrip != null)
                heldGrip.NoteReacquisition(isLeftHand);
            if (heldGrip != null && heldGrip.debugPose && held != null)
                HandTrackingLog.Write("Grab", $"{HandName()} recuperó {held.name} con una transición corta.");
            return targetPosition;
        }

        if (continuous)
        {
            AcceptReliable(targetPosition, targetRotation, false);
            return targetPosition;
        }

        if (!hasPendingOutlier)
        {
            if (heldGrip != null)
                heldGrip.NoteOutlier(isLeftHand);
            if (heldGrip != null && heldGrip.debugPose && held != null)
                HandTrackingLog.Write("Grab", $"{HandName()} descartó una muestra de {held.name}. Paso={step:0.000}m. Giro={angle:0}°.");
        }

        pendingOutlierPosition = targetPosition;
        pendingOutlierRotation = targetRotation;
        hasPendingOutlier = true;
        outlierStreak++;
        targetRotation = reliableRotation;
        return reliablePosition;
    }

    void AcceptReliable(Vector3 targetPosition, Quaternion targetRotation, bool reseedFilter)
    {
        reliablePosition = targetPosition;
        reliableRotation = targetRotation;
        outlierStreak = 0;
        hasPendingOutlier = false;
        if (!reseedFilter)
            return;
        positionFilter.Reset(targetPosition);
        rotationFilter.Reset(targetRotation);
        filterSeeded = true;
    }

    void ApplyContact(ref Vector3 position, ref Quaternion rotation, float dt)
    {
        if (heldGrip == null)
            return;

        if (!heldGrip.stabilizeContact || !hasTipLocal)
        {
            contactActive = false;
            shownRotation = rotation;
            heldGrip.ReportContact(0f, 0f);
            return;
        }

        Vector3 axis = rotation * tipLocal;
        if (axis.sqrMagnitude < 0.0000001f)
        {
            EndContact(rotation);
            heldGrip.ReportContact(0f, 0f);
            return;
        }

        axis.Normalize();
        const float behind = 0.002f;
        Vector3 targetTip = position + rotation * tipLocal;
        float band = contactDraw != null && contactDraw.contactDistance > 0.0001f
            ? contactDraw.contactDistance
            : 0.008f;
        int count = Physics.RaycastNonAlloc(targetTip - axis * behind, axis, ContactHits, behind + band, ~0, QueryTriggerInteraction.Ignore);
        float bestDistance = float.MaxValue;
        bool found = false;
        Vector3 hitPoint = Vector3.zero;
        Vector3 hitNormal = Vector3.up;
        for (int i = 0; i < count; i++)
        {
            if (!AcceptContact(ContactHits[i].collider) || ContactHits[i].distance >= bestDistance)
                continue;
            bestDistance = ContactHits[i].distance;
            hitPoint = ContactHits[i].point;
            hitNormal = ContactHits[i].normal;
            found = true;
        }

        if (!found)
        {
            EndContact(rotation);
            heldGrip.ReportContact(0f, 0f);
            return;
        }

        if (Vector3.Dot(hitNormal, targetTip - hitPoint) < 0f)
            hitNormal = -hitNormal;
        if (!contactActive || !hasContactTip)
        {
            contactActive = true;
            hasContactTip = true;
            contactTip = targetTip;
            shownRotation = rotation;
        }

        Vector3 delta = targetTip - contactTip;
        Vector3 normalDelta = Vector3.Project(delta, hitNormal);
        Vector3 tangentDelta = delta - normalDelta;
        float tau = Mathf.Max(0.02f, heldGrip.contactNormalSeconds);
        float alpha = 1f - Mathf.Exp(-dt / tau);
        float normalSpeed = normalDelta.magnitude / Mathf.Max(dt, 0.001f);
        if (normalSpeed > 0.04f)
            alpha = Mathf.Max(alpha, 0.85f);
        Vector3 outputTip = contactTip + tangentDelta + normalDelta * alpha;
        Vector3 correction = outputTip - targetTip;
        float limit = Mathf.Max(0f, heldGrip.maxContactCorrection);
        if (correction.sqrMagnitude > limit * limit && correction.sqrMagnitude > 0.0000001f)
            outputTip = targetTip + correction.normalized * limit;

        Vector3 before = position;
        position += outputTip - targetTip;
        if (heldGrip.contactRotationSeconds > 0.001f)
        {
            float speed = Quaternion.Angle(shownRotation, rotation) / Mathf.Max(dt, 0.001f);
            float rotTau = speed > 70f ? 0.012f : heldGrip.contactRotationSeconds;
            float rotAlpha = 1f - Mathf.Exp(-dt / Mathf.Max(0.012f, rotTau));
            Quaternion damped = Quaternion.Slerp(shownRotation, rotation, rotAlpha);
            Vector3 pivot = position + rotation * tipLocal;
            rotation = damped;
            position += pivot - (position + rotation * tipLocal);
        }

        shownRotation = rotation;
        contactTip = position + rotation * tipLocal;
        float gap = Vector3.Dot(targetTip - hitPoint, hitNormal);
        heldGrip.ReportContact(gap * 1000f, Vector3.Distance(before, position) * 1000f);
    }

    void EndContact(Quaternion rotation)
    {
        contactActive = false;
        hasContactTip = false;
        shownRotation = rotation;
    }

    bool AcceptContact(Collider col)
    {
        if (col == null || held == null)
            return false;
        if (col.transform == held.transform || col.transform.IsChildOf(held.transform))
            return false;
        if (contactDraw == null)
            return true;

        Collider[] surfaces = contactDraw.surfaceColliders;
        if (surfaces == null)
            return false;
        for (int i = 0; i < surfaces.Length; i++)
        {
            if (surfaces[i] == col)
                return true;
        }

        return false;
    }

    void ApplyHoldPose(Vector3 position, Quaternion rotation)
    {
        held.transform.SetPositionAndRotation(position, rotation);
        if (heldBody == null)
            return;

        heldBody.position = position;
        heldBody.rotation = rotation;
        heldBody.linearVelocity = Vector3.zero;
        heldBody.angularVelocity = Vector3.zero;
    }

    void TryGrab(Vector3 pinchPoint, Quaternion palmRotation, XRGrabInteractable prefer)
    {
        XRGrabInteractable candidate = FindCandidate(pinchPoint);
        if (prefer != null && prefer.isActiveAndEnabled && !prefer.isSelected &&
            DistanceToTool(prefer, pinchPoint) <= settings.pinchGrabRadius)
        {
            ToolHandGrip preferredGrip = prefer.GetComponent<ToolHandGrip>();
            if (preferredGrip == null || preferredGrip.AcceptsPinch(pinchPoint))
                candidate = prefer;
        }
        if (candidate == null || candidate.isSelected)
            return;

        Vector3 poseBefore = candidate.transform.position;
        Quaternion rotationBefore = candidate.transform.rotation;
        interactor.StartManualInteraction((IXRSelectInteractable)candidate);
        candidate.transform.SetPositionAndRotation(poseBefore, rotationBefore);
        if (!interactor.isPerformingManualInteraction && !candidate.isSelected)
            return;
        held = candidate;
        heldGrip = candidate.GetComponent<ToolHandGrip>();
        if (heldGrip != null)
            heldGrip.BeginHold(isLeftHand);
        savedTrackPosition = candidate.trackPosition;
        savedTrackRotation = candidate.trackRotation;
        candidate.trackPosition = false;
        candidate.trackRotation = false;
        candidate.throwOnDetach = false;
        candidate.forceGravityOnDetach = false;

        heldBody = candidate.GetComponent<Rigidbody>();
        Rigidbody body = heldBody;
        if (body != null)
        {
            savedKinematic = body.isKinematic;
            savedGravity = body.useGravity;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            savedInterpolation = body.interpolation;
            body.interpolation = RigidbodyInterpolation.None;
            body.isKinematic = true;
            body.useGravity = false;
        }

        heldLocalRotation = Quaternion.Inverse(palmRotation) * candidate.transform.rotation;
        heldLocalOffset = Quaternion.Inverse(palmRotation) * (candidate.transform.position - pinchPoint);
        snapping = heldGrip != null && heldGrip.mode == ToolHandGrip.GripMode.Anchored && heldGrip.snapSeconds > 0.001f;
        snapFromPosition = candidate.transform.position;
        snapFromRotation = candidate.transform.rotation;
        snapT = 0f;
        recovering = false;
        filterSeeded = false;
        gripCaptured = false;
        contactActive = false;
        hasTipLocal = false;
        contactDraw = candidate.GetComponent<Draw>();
        Vector3 tipWorld = Vector3.zero;
        bool foundTip = false;
        if (contactDraw != null && contactDraw.tip != null)
        {
            tipWorld = contactDraw.tip.position;
            foundTip = true;
        }
        else if (heldGrip != null && heldGrip.TryGetFunctionalTip(out tipWorld))
        {
            foundTip = true;
            contactDraw = null;
        }

        if (foundTip)
        {
            tipLocal = Quaternion.Inverse(candidate.transform.rotation) * (tipWorld - candidate.transform.position);
            hasTipLocal = tipLocal.sqrMagnitude > 0.0000001f;
        }

        hasReliableTarget = false;
        hasPendingOutlier = false;
        outlierStreak = 0;
        releaseTimer = 0f;
        confirmTarget = null;
        confirmGrip = null;
        confirmTimer = 0f;
        driving = true;
        Grabbed?.Invoke(candidate);
        string pose = snapping ? "anclado" : "libre";
        HandTrackingLog.Write("Grab",
            $"{HandName()} agarró {candidate.name}. Pose={pose}. Gesto=pellizco.");
    }

    void UpdateHysteresis(float pinchDistance)
    {
        bool was = pinchHeld;
        pinchHeld = GripRules.StepPinch(pinchHeld, pinchDistance, settings.pinchOnMeters, settings.pinchOffMeters);
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

            ToolHandGrip grip = grab.GetComponent<ToolHandGrip>();
            if (grip != null && !grip.AcceptsPinch(pinchPoint))
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

    void CalibrateFrame(Quaternion palmRotation, Vector3 palmPosition, Vector3 wristPosition, Vector3 middlePosition, Vector3 thumbPosition)
    {
        if (frameCalibrated)
            return;

        Vector3 fingers = middlePosition - wristPosition;
        if (fingers.sqrMagnitude < 0.0004f)
            return;
        fingers.Normalize();

        Vector3 thumb = Vector3.ProjectOnPlane(thumbPosition - palmPosition, fingers);
        if (thumb.sqrMagnitude < 0.0001f)
            return;
        thumb.Normalize();

        float yDot = Vector3.Dot(palmRotation * Vector3.up, fingers);
        float xDot = Vector3.Dot(palmRotation * Vector3.right, thumb);
        if (Mathf.Abs(yDot) < 0.45f || Mathf.Abs(xDot) < 0.35f)
            return;

        axisX = xDot >= 0f ? 1f : -1f;
        axisY = yDot >= 0f ? 1f : -1f;
        frameCalibrated = true;
        HandTrackingLog.Write("Grab",
            $"{HandName()} marco de palma listo. Pulgar={(axisX > 0f ? "+X" : "-X")}. Dedos={(axisY > 0f ? "+Y" : "-Y")}.");
    }

    Quaternion HandFrame(Quaternion palmRotation)
    {
        Vector3 right = palmRotation * new Vector3(axisX, 0f, 0f);
        Vector3 up = palmRotation * new Vector3(0f, axisY, 0f);
        Vector3 forward = Vector3.Cross(right, up);
        if (forward.sqrMagnitude < 0.000001f)
            return palmRotation;
        return Quaternion.LookRotation(forward, up);
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
        LeftSample = default;
        RightSample = default;
    }

    bool PalmDrivesHeldPose()
    {
        return heldGrip != null &&
               heldGrip.mode == ToolHandGrip.GripMode.Anchored &&
               heldGrip.lockPoseWhileHeld;
    }

    float AnchoredGrace()
    {
        if (heldGrip != null && heldGrip.mode == ToolHandGrip.GripMode.Anchored)
            return Mathf.Max(0.05f, heldGrip.trackingGraceSeconds);
        return 0.1f;
    }

    void ReportPhase(ToolHandGrip.GripPhase phase, bool trackingValid, float graceRemaining)
    {
        if (heldGrip == null || !heldGrip.debugPose)
            return;
        heldGrip.Report(phase, HandName(), trackingValid, graceRemaining, heldGrip.PositionErrorMeters, heldGrip.AngleErrorDegrees);
    }

    void PublishSample(bool valid, bool pinching, Vector3 pinch)
    {
        var sample = new HandSample { valid = valid, pinching = pinching, pinch = pinch };
        if (isLeftHand)
            LeftSample = sample;
        else
            RightSample = sample;
    }

    void Release(string reason)
    {
        if (held == null && (interactor == null || !interactor.isPerformingManualInteraction))
        {
            driving = false;
            snapping = false;
            recovering = false;
            releaseTimer = 0f;
            return;
        }

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
                body.interpolation = savedInterpolation;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        if (heldGrip != null && reason == "gesto abierto")
            heldGrip.NoteUserRelease(isLeftHand);
        else if (heldGrip != null)
            heldGrip.NoteUnexpectedRelease(isLeftHand);

        held = null;
        heldGrip = null;
        heldBody = null;
        snapping = false;
        recovering = false;
        filterSeeded = false;
        gripCaptured = false;
        contactActive = false;
        contactDraw = null;
        hasTipLocal = false;
        hasReliableTarget = false;
        releaseTimer = 0f;
        confirmTarget = null;
        confirmGrip = null;
        confirmTimer = 0f;
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
