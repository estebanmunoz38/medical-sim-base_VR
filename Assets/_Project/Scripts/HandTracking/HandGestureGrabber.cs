using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HandGestureGrabber : MonoBehaviour
{
    public enum GrabGesture
    {
        Pinch,
        Fist
    }

    [SerializeField] HandTrackingBootstrap settings;
    [SerializeField] bool isLeftHand;
    [SerializeField] XRDirectInteractor interactor;
    [SerializeField] Transform trackingOrigin;

    XRHandSubsystem subsystem;
    GrabGesture heldGesture;
    XRGrabInteractable held;
    bool pinchHeld;
    bool fistHeld;
    bool warnedMissingSubsystem;
    bool warnedShaverPinch;

    static readonly Collider[] OverlapBuffer = new Collider[32];

    public void Configure(HandTrackingBootstrap bootstrap, bool left, XRDirectInteractor directInteractor, Transform origin)
    {
        settings = bootstrap;
        isLeftHand = left;
        interactor = directInteractor;
        trackingOrigin = origin;
    }

    void OnDisable()
    {
        Release("componente deshabilitado");
    }

    void Update()
    {
        if (settings == null || interactor == null)
            return;

        if (!TryGetSubsystem(out subsystem) || !subsystem.running)
        {
            Release("subsistema de manos detenido");
            return;
        }

        XRHand hand = isLeftHand ? subsystem.leftHand : subsystem.rightHand;
        if (!hand.isTracked)
        {
            Release("mano sin tracking");
            return;
        }

        if (!TryGetPose(hand, XRHandJointID.Wrist, out Pose wrist) ||
            !TryGetPose(hand, XRHandJointID.Palm, out Pose palm) ||
            !TryGetPose(hand, XRHandJointID.ThumbTip, out Pose thumb) ||
            !TryGetPose(hand, XRHandJointID.IndexTip, out Pose index))
        {
            return;
        }

        Vector3 thumbWorld = ToWorld(thumb.position);
        Vector3 indexWorld = ToWorld(index.position);
        Vector3 palmWorld = ToWorld(palm.position);
        Vector3 pinchPoint = (thumbWorld + indexWorld) * 0.5f;
        transform.SetPositionAndRotation(pinchPoint, trackingOrigin.rotation * wrist.rotation);

        float pinchDistance = Vector3.Distance(thumbWorld, indexWorld);
        float fistDistance = FistDistance(hand, wrist.position);
        UpdateHysteresis(pinchDistance, fistDistance);

        if (held != null)
        {
            if (heldGesture == GrabGesture.Fist)
                transform.position = palmWorld;
            if (!GestureActive(heldGesture))
                Release("gesto abierto");
            return;
        }

        XRGrabInteractable candidate = FindCandidate(pinchPoint, palmWorld, out GrabGesture gesture);
        if (candidate == null)
            return;

        if (gesture == GrabGesture.Fist)
            transform.position = palmWorld;

        string previous = "libre";
        interactor.StartManualInteraction((IXRSelectInteractable)candidate);
        if (!interactor.isPerformingManualInteraction)
            return;

        held = candidate;
        heldGesture = gesture;
        HandTrackingLog.Write("Grab",
            $"{HandName()} agarró {candidate.name}. Previo={previous}. Resultado=agarrada. Gesto={gesture}. Evento=XRGrabInteractable.select.");
    }

    void UpdateHysteresis(float pinchDistance, float fistDistance)
    {
        if (!pinchHeld && pinchDistance <= settings.pinchOnMeters)
            pinchHeld = true;
        else if (pinchHeld && pinchDistance >= settings.pinchOffMeters)
            pinchHeld = false;

        if (!fistHeld && fistDistance <= settings.fistOnMeters)
            fistHeld = true;
        else if (fistHeld && fistDistance >= settings.fistOffMeters)
            fistHeld = false;
    }

    XRGrabInteractable FindCandidate(Vector3 pinchPoint, Vector3 palmPoint, out GrabGesture gesture)
    {
        gesture = GrabGesture.Pinch;
        XRGrabInteractable best = null;
        float bestDistance = float.MaxValue;

        Collect(pinchPoint, settings.pinchGrabRadius, GrabGesture.Pinch, pinchPoint, ref best, ref bestDistance, ref gesture);
        Collect(palmPoint, settings.fistGrabRadius, GrabGesture.Fist, palmPoint, ref best, ref bestDistance, ref gesture);
        return best;
    }

    void Collect(Vector3 center, float radius, GrabGesture required, Vector3 measureFrom, ref XRGrabInteractable best, ref float bestDistance, ref GrabGesture bestGesture)
    {
        int count = Physics.OverlapSphereNonAlloc(center, radius, OverlapBuffer, settings.grabLayers, QueryTriggerInteraction.Collide);
        var seen = new HashSet<XRGrabInteractable>();
        for (int i = 0; i < count; i++)
        {
            Collider col = OverlapBuffer[i];
            if (col == null)
                continue;

            XRGrabInteractable grab = col.GetComponentInParent<XRGrabInteractable>();
            if (grab == null || grab.isSelected || !seen.Add(grab))
                continue;

            if (GestureFor(grab) != required || !GestureActive(required))
            {
                if (required == GrabGesture.Pinch && grab.name.Contains("Cortadora") && pinchHeld && !warnedShaverPinch)
                {
                    warnedShaverPinch = true;
                    HandTrackingLog.Write("Shaver",
                        "Cortadora de pelo cerca con pinch. Previo=libre. Resultado=no agarrada. Gesto requerido=cierre de mano.");
                }
                continue;
            }

            float distance = Vector3.Distance(measureFrom, col.bounds.center);
            if (distance < bestDistance)
            {
                best = grab;
                bestDistance = distance;
                bestGesture = required;
            }
        }
    }

    GrabGesture GestureFor(XRGrabInteractable grab)
    {
        if (grab.name.Contains("Cortadora"))
            return GrabGesture.Fist;
        return GrabGesture.Pinch;
    }

    bool GestureActive(GrabGesture gesture)
    {
        return gesture == GrabGesture.Fist ? fistHeld : pinchHeld;
    }

    static float FistDistance(XRHand hand, Vector3 wrist)
    {
        float sum = 0f;
        int count = 0;
        Accumulate(hand, XRHandJointID.MiddleTip, wrist, ref sum, ref count);
        Accumulate(hand, XRHandJointID.RingTip, wrist, ref sum, ref count);
        Accumulate(hand, XRHandJointID.LittleTip, wrist, ref sum, ref count);
        return count == 0 ? 1f : sum / count;
    }

    static void Accumulate(XRHand hand, XRHandJointID jointId, Vector3 wrist, ref float sum, ref int count)
    {
        if (!TryGetPose(hand, jointId, out Pose pose))
            return;
        sum += Vector3.Distance(pose.position, wrist);
        count++;
    }

    static bool TryGetPose(XRHand hand, XRHandJointID jointId, out Pose pose)
    {
        pose = default;
        return hand.GetJoint(jointId).TryGetPose(out pose);
    }

    bool TryGetSubsystem(out XRHandSubsystem handSubsystem)
    {
        if (subsystem != null)
        {
            handSubsystem = subsystem;
            return true;
        }

        var list = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(list);
        if (list.Count == 0)
        {
            if (!warnedMissingSubsystem)
            {
                warnedMissingSubsystem = true;
                HandTrackingLog.Write("HandTracking",
                    "XRHandSubsystem no está en ejecución. Previo=sin manos. Resultado=sin agarre. Revisar Hand Tracking en el visor.");
            }
            handSubsystem = null;
            return false;
        }

        subsystem = list[0];
        handSubsystem = subsystem;
        return true;
    }

    void Release(string reason)
    {
        if (held == null && !interactor.isPerformingManualInteraction)
            return;

        string objectName = held != null ? held.name : "desconocido";
        if (interactor.isPerformingManualInteraction)
            interactor.EndManualInteraction();

        HandTrackingLog.Write("Grab",
            $"{HandName()} soltó {objectName}. Previo=agarrada. Resultado=libre. Motivo={reason}.");
        held = null;
    }

    Vector3 ToWorld(Vector3 originLocalPoint)
    {
        return trackingOrigin != null ? trackingOrigin.TransformPoint(originLocalPoint) : originLocalPoint;
    }

    string HandName()
    {
        return isLeftHand ? "Mano izquierda" : "Mano derecha";
    }
}
