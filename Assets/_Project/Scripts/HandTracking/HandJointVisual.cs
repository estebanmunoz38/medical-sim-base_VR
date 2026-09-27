using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Hands;

public class HandJointVisual : MonoBehaviour
{
    static readonly XRHandJointID[] Joints =
    {
        XRHandJointID.Wrist,
        XRHandJointID.Palm,
        XRHandJointID.ThumbMetacarpal,
        XRHandJointID.ThumbProximal,
        XRHandJointID.ThumbDistal,
        XRHandJointID.ThumbTip,
        XRHandJointID.IndexMetacarpal,
        XRHandJointID.IndexProximal,
        XRHandJointID.IndexIntermediate,
        XRHandJointID.IndexDistal,
        XRHandJointID.IndexTip,
        XRHandJointID.MiddleMetacarpal,
        XRHandJointID.MiddleProximal,
        XRHandJointID.MiddleIntermediate,
        XRHandJointID.MiddleDistal,
        XRHandJointID.MiddleTip,
        XRHandJointID.RingMetacarpal,
        XRHandJointID.RingProximal,
        XRHandJointID.RingIntermediate,
        XRHandJointID.RingDistal,
        XRHandJointID.RingTip,
        XRHandJointID.LittleMetacarpal,
        XRHandJointID.LittleProximal,
        XRHandJointID.LittleIntermediate,
        XRHandJointID.LittleDistal,
        XRHandJointID.LittleTip
    };

    static readonly (XRHandJointID from, XRHandJointID to)[] Bones =
    {
        (XRHandJointID.Wrist, XRHandJointID.Palm),
        (XRHandJointID.Wrist, XRHandJointID.ThumbMetacarpal),
        (XRHandJointID.ThumbMetacarpal, XRHandJointID.ThumbProximal),
        (XRHandJointID.ThumbProximal, XRHandJointID.ThumbDistal),
        (XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip),
        (XRHandJointID.Palm, XRHandJointID.IndexMetacarpal),
        (XRHandJointID.IndexMetacarpal, XRHandJointID.IndexProximal),
        (XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate),
        (XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal),
        (XRHandJointID.IndexDistal, XRHandJointID.IndexTip),
        (XRHandJointID.Palm, XRHandJointID.MiddleMetacarpal),
        (XRHandJointID.MiddleMetacarpal, XRHandJointID.MiddleProximal),
        (XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate),
        (XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal),
        (XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip),
        (XRHandJointID.Palm, XRHandJointID.RingMetacarpal),
        (XRHandJointID.RingMetacarpal, XRHandJointID.RingProximal),
        (XRHandJointID.RingProximal, XRHandJointID.RingIntermediate),
        (XRHandJointID.RingIntermediate, XRHandJointID.RingDistal),
        (XRHandJointID.RingDistal, XRHandJointID.RingTip),
        (XRHandJointID.Palm, XRHandJointID.LittleMetacarpal),
        (XRHandJointID.LittleMetacarpal, XRHandJointID.LittleProximal),
        (XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate),
        (XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal),
        (XRHandJointID.LittleDistal, XRHandJointID.LittleTip)
    };

    HandTrackingSpace trackingSpace;
    bool isLeftHand;
    public bool meshIsShowing;
    XRHandSubsystem subsystem;
    bool subscribed;
    Transform root;
    readonly Dictionary<XRHandJointID, Transform> dots = new Dictionary<XRHandJointID, Transform>();
    readonly List<BoneLine> lines = new List<BoneLine>();

    struct BoneLine
    {
        public XRHandJointID From;
        public XRHandJointID To;
        public LineRenderer Line;
    }

    public void Configure(HandTrackingSpace space, bool left)
    {
        trackingSpace = space;
        isLeftHand = left;
        Build();
    }

    void Build()
    {
        root = new GameObject(isLeftHand ? "Ghost Hand Left" : "Ghost Hand Right").transform;
        root.gameObject.SetActive(false);

        Material material = HandVisualMaterial.Create(new Color(0.55f, 0.9f, 1f, 1f), 0.82f);
        for (int i = 0; i < Joints.Length; i++)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = Joints[i].ToString();
            Destroy(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(root.transform, false);
            bool tip = Joints[i].ToString().EndsWith("Tip");
            sphere.transform.localScale = Vector3.one * (tip ? 0.016f : 0.011f);
            var renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            dots[Joints[i]] = sphere.transform;
        }

        for (int i = 0; i < Bones.Length; i++)
        {
            var lineObject = new GameObject(Bones[i].from + "-" + Bones[i].to);
            lineObject.transform.SetParent(root.transform, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = false;
            line.startWidth = line.endWidth = 0.007f;
            line.sharedMaterial = material;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 2;
            lines.Add(new BoneLine { From = Bones[i].from, To = Bones[i].to, Line = line });
        }
    }

    void OnEnable()
    {
        TrySubscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void OnDestroy()
    {
        Unsubscribe();
        if (root != null)
            Destroy(root);
    }

    void Update()
    {
        if (!subscribed)
            TrySubscribe();
        if (subsystem != null && subsystem.running)
            Apply(subsystem);
    }

    void TrySubscribe()
    {
        if (subscribed)
            return;

        if (subsystem == null)
        {
            var list = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(list);
            if (list.Count == 0)
                return;
            subsystem = list[0];
        }

        if (!subsystem.running)
            return;

        subsystem.updatedHands += OnHandsUpdated;
        subscribed = true;
    }

    void Unsubscribe()
    {
        if (!subscribed || subsystem == null)
            return;
        subsystem.updatedHands -= OnHandsUpdated;
        subscribed = false;
    }

    void OnHandsUpdated(XRHandSubsystem source, XRHandSubsystem.UpdateSuccessFlags successFlags, XRHandSubsystem.UpdateType updateType)
    {
        if (updateType == XRHandSubsystem.UpdateType.Dynamic)
            return;

        Apply(source);
    }

    void Apply(XRHandSubsystem source)
    {
        if (root == null || trackingSpace == null || source == null || !source.running)
            return;

        XRHand hand = isLeftHand ? source.leftHand : source.rightHand;
        if (meshIsShowing || !hand.isTracked || !hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palm))
        {
            root.gameObject.SetActive(false);
            return;
        }

        if (!trackingSpace.TryGet(palm.position, out Transform space))
        {
            root.gameObject.SetActive(false);
            return;
        }

        if (root.parent != space)
        {
            root.SetParent(space, false);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
        }

        root.gameObject.SetActive(true);
        for (int i = 0; i < Joints.Length; i++)
        {
            Transform dot = dots[Joints[i]];
            if (!hand.GetJoint(Joints[i]).TryGetPose(out Pose pose))
            {
                dot.gameObject.SetActive(false);
                continue;
            }

            dot.gameObject.SetActive(true);
            dot.localPosition = pose.position;
            dot.localRotation = pose.rotation;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            BoneLine bone = lines[i];
            bool hasFrom = dots.TryGetValue(bone.From, out Transform from) && from.gameObject.activeSelf;
            bool hasTo = dots.TryGetValue(bone.To, out Transform to) && to.gameObject.activeSelf;
            bone.Line.enabled = hasFrom && hasTo;
            if (!bone.Line.enabled)
                continue;
            bone.Line.SetPosition(0, from.localPosition);
            bone.Line.SetPosition(1, to.localPosition);
        }
    }
}
