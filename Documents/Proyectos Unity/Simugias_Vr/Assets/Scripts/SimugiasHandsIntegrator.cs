using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Samples.VisualizerSample;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.Hands;
using Unity.XR.CoreUtils;

/// <summary>
/// Integra el rig de manos que ya siguió al usuario en HandTracking_Test
/// dentro del XR Origin existente de Simugias. No instancia un segundo
/// origen, cámara ni Interaction Manager.
/// </summary>
[DefaultExecutionOrder(-80)]
public class SimugiasHandsIntegrator : MonoBehaviour
{
    [SerializeField] InputActionAsset xriDefaultActions;

    readonly List<XRHandSubsystem> _subsystems = new List<XRHandSubsystem>();
    [SerializeField] bool showDiagnosticsHud;
    TextMesh _hud;
    XROrigin _origin;
    Transform _leftHand;
    Transform _rightHand;
    XRGrabInteractable _hovered;
    XRGrabInteractable _grabbed;
    string _mode = "manos";
    bool _ready;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (!SimugiasRuntimeGate.AllowMedicalRuntime())
            return;
        if (FindFirstObjectByType<SimugiasHandsIntegrator>() != null)
            return;
        if (FindFirstObjectByType<Kerrison>() == null && FindFirstObjectByType<Endoscopio>() == null
            && FindFirstObjectByType<BisturiCutControl>() == null)
            return;

        var origin = FindFirstObjectByType<XROrigin>();
        if (origin == null)
        {
            Debug.LogError("[SimugiasHands] No hay XR Origin. No se duplica: se usa el de la escena.");
            return;
        }

        if (origin.GetComponent<SimugiasHandsIntegrator>() == null)
            origin.gameObject.AddComponent<SimugiasHandsIntegrator>();
    }

    void Awake()
    {
        _origin = GetComponent<XROrigin>() ?? FindFirstObjectByType<XROrigin>();
        StartCoroutine(InstallWhenReady());
    }

    IEnumerator InstallWhenReady()
    {
        yield return null;
        yield return null;
        AlignTrackingOrigin();
        EnsureSingleHmdCamera();
        EnsureInteractionManager();
        EnableXriActions();
        CacheHands();
        ForceBothHandsLive();
        AlignHandVisualizer();
        var adapter = GetComponent<HandXriGrabAdapter>();
        if (adapter == null)
            adapter = gameObject.AddComponent<HandXriGrabAdapter>();
        adapter.Adapt();
        HandInteractionPolish.RunNow();
        HandsVisibilityGuard.Ensure(transform);
        if (showDiagnosticsHud)
            BuildHud();
        _ready = true;
        Debug.Log("[SimugiasHands] Manos integradas en el XR Origin existente. Quirófano intacto.");
    }

    void LateUpdate()
    {
        if (!_ready || _origin == null) return;
        AlignTrackingOrigin();
        ForceBothHandsLive();
        KeepVisualizerAtIdentity();
        if (showDiagnosticsHud)
            UpdateHud();
    }

    void AlignTrackingOrigin()
    {
        if (_origin == null) return;
        if (_origin.RequestedTrackingOriginMode != XROrigin.TrackingOriginMode.Floor)
            _origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        Transform root = _origin.Origin != null ? _origin.Origin.transform : _origin.transform;
        if (root.localScale != Vector3.one)
            root.localScale = Vector3.one;

        Transform offset = _origin.CameraFloorOffsetObject != null
            ? _origin.CameraFloorOffsetObject.transform
            : root.Find("Camera Offset");
        if (offset == null) return;

        offset.localRotation = Quaternion.identity;
        if (offset.localScale != Vector3.one)
            offset.localScale = Vector3.one;

        bool floor = _origin.CurrentTrackingOriginMode == TrackingOriginModeFlags.Floor;
        if (floor && offset.localPosition.sqrMagnitude > 0.0001f)
            offset.localPosition = Vector3.zero;
    }

    void EnsureSingleHmdCamera()
    {
        Camera keep = _origin != null ? _origin.Camera : Camera.main;
        var cams = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < cams.Length; i++)
        {
            Camera cam = cams[i];
            if (cam == null || cam == keep) continue;
            if (cam.targetTexture != null) continue;
            if (cam.GetComponentInParent<Canvas>() != null) continue;
            if (cam.stereoTargetEye != StereoTargetEyeMask.None)
                cam.stereoTargetEye = StereoTargetEyeMask.None;
        }

        if (keep != null)
        {
            keep.cullingMask = ~0;
            keep.nearClipPlane = Mathf.Min(keep.nearClipPlane, 0.02f);
            keep.stereoTargetEye = StereoTargetEyeMask.Both;
        }
    }

    void EnsureInteractionManager()
    {
        if (FindFirstObjectByType<XRInteractionManager>() == null)
            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
    }

    void EnableXriActions()
    {
        InputActionAsset asset = xriDefaultActions != null ? xriDefaultActions : FindXriActions();
        if (asset == null)
        {
            Debug.LogWarning("[SimugiasHands] No está XRI Default Input Actions. Componente: InputActionManager.");
            return;
        }

        var manager = _origin.GetComponent<InputActionManager>();
        if (manager == null)
            manager = _origin.gameObject.AddComponent<InputActionManager>();
        manager.actionAssets = new List<InputActionAsset> { asset };
        manager.EnableInput();
        asset.Enable();
    }

    static InputActionAsset FindXriActions()
    {
        var all = Resources.FindObjectsOfTypeAll<InputActionAsset>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name.IndexOf("XRI Default", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return all[i];
        }
        return null;
    }

    void CacheHands()
    {
        Transform offset = Offset();
        if (offset == null) return;
        _leftHand = FindNamed(offset, "Left Hand");
        _rightHand = FindNamed(offset, "Right Hand");
        if (_leftHand == null || _rightHand == null)
            Debug.LogWarning("[SimugiasHands] Falta Left Hand o Right Hand en Camera Offset. HandsInteractionBootstrap debe inyectarlos.");
    }

    void ForceBothHandsLive()
    {
        Transform offset = Offset();
        if (offset == null) return;
        if (_leftHand == null) _leftHand = FindNamed(offset, "Left Hand");
        if (_rightHand == null) _rightHand = FindNamed(offset, "Right Hand");

        if (_leftHand != null)
        {
            _leftHand.gameObject.SetActive(true);
            _leftHand.localScale = Vector3.one;
        }
        if (_rightHand != null)
        {
            _rightHand.gameObject.SetActive(true);
            _rightHand.localScale = Vector3.one;
        }

        var nearFars = _origin.GetComponentsInChildren<NearFarInteractor>(true);
        for (int i = 0; i < nearFars.Length; i++)
        {
            if (nearFars[i] == null || !IsUnderHand(nearFars[i].transform)) continue;
            nearFars[i].gameObject.SetActive(true);
            nearFars[i].enabled = true;
            nearFars[i].enableNearCasting = true;
            nearFars[i].interactionLayers = ~0;
        }

        var readers = _origin.GetComponentsInChildren<ReleaseThresholdButtonReader>(true);
        for (int i = 0; i < readers.Length; i++)
        {
            if (readers[i] == null) continue;
            readers[i].enabled = true;
            readers[i].gameObject.SetActive(true);
        }

        var modality = _origin.GetComponent<XRInputModalityManager>();
        if (modality != null)
        {
            if (_leftHand != null)
                modality.leftHand = _leftHand.gameObject;
            if (_rightHand != null)
                modality.rightHand = _rightHand.gameObject;
        }
    }

    void AlignHandVisualizer()
    {
        Transform offset = Offset();
        if (offset == null) return;
        Transform viz = FindNamed(offset, "Hand Visualizer");
        if (viz == null)
            viz = FindNamed(_origin.transform, "Hand Visualizer");
        if (viz == null) return;

        viz.SetParent(offset, false);
        viz.localPosition = Vector3.zero;
        viz.localRotation = Quaternion.identity;
        viz.localScale = Vector3.one;
        viz.gameObject.SetActive(true);

        var visualizer = viz.GetComponent<HandVisualizer>();
        if (visualizer == null) return;
        visualizer.enabled = true;
        visualizer.drawMeshes = true;
        visualizer.debugDrawJoints = false;

        var skins = viz.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i] == null) continue;
            skins[i].enabled = true;
            skins[i].gameObject.SetActive(true);
            skins[i].gameObject.layer = 0;
        }
    }

    void KeepVisualizerAtIdentity()
    {
        Transform offset = Offset();
        if (offset == null) return;
        Transform viz = offset.Find("Hand Visualizer");
        if (viz == null) return;
        if (viz.localPosition.sqrMagnitude > 0.0001f)
            viz.localPosition = Vector3.zero;
        if (viz.localScale != Vector3.one)
            viz.localScale = Vector3.one;
        if (!viz.gameObject.activeSelf)
            viz.gameObject.SetActive(true);
    }

    Transform Offset()
    {
        if (_origin == null) return null;
        if (_origin.CameraFloorOffsetObject != null)
            return _origin.CameraFloorOffsetObject.transform;
        return _origin.transform.Find("Camera Offset");
    }

    void BuildHud()
    {
        Camera cam = _origin != null ? _origin.Camera : Camera.main;
        if (cam == null) return;
        var go = new GameObject("SimugiasHands_HUD");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0.22f, 0.18f, 0.65f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one * 0.008f;
        _hud = go.AddComponent<TextMesh>();
        _hud.fontSize = 22;
        _hud.characterSize = 0.45f;
        _hud.anchor = TextAnchor.UpperRight;
        _hud.alignment = TextAlignment.Right;
        _hud.color = new Color(1f, 1f, 1f, 0.85f);
        _hud.text = "";
    }

    void UpdateHud()
    {
        if (_hud == null) return;
        XRHandSubsystem hands = RunningHands();
        bool lDet = hands != null && hands.leftHand.isTracked;
        bool rDet = hands != null && hands.rightHand.isTracked;
        bool lVis = HandMeshVisible(true);
        bool rVis = HandMeshVisible(false);
        bool lPinch = Pinch(true);
        bool rPinch = Pinch(false);
        RefreshGrabState();
        _mode = (lDet || rDet) ? "manos" : "controles";

        _hud.text =
            "L: " + (lDet ? "detectada" : "no") + "/" + (lVis ? "visible" : "oculta") + "\n" +
            "R: " + (rDet ? "detectada" : "no") + "/" + (rVis ? "visible" : "oculta") + "\n" +
            "Pellizco L/R: " + Yn(lPinch) + "/" + Yn(rPinch) + "\n" +
            "Herramienta: " + (_hovered != null ? _hovered.name : "no") + "\n" +
            "Agarrada: " + (_grabbed != null ? _grabbed.name : "no") + "\n" +
            "Modo: " + _mode;
    }

    bool HandMeshVisible(bool left)
    {
        Transform offset = Offset();
        if (offset == null) return false;
        Transform viz = offset.Find("Hand Visualizer");
        if (viz == null) return false;
        var skins = viz.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i] == null || !skins[i].enabled || !skins[i].gameObject.activeInHierarchy)
                continue;
            string n = skins[i].gameObject.name + skins[i].transform.parent.name;
            bool isLeft = n.IndexOf("Left", System.StringComparison.OrdinalIgnoreCase) >= 0
                          || n.IndexOf("_L", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool isRight = n.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0
                           || n.IndexOf("_R", System.StringComparison.OrdinalIgnoreCase) >= 0;
            if (left && isLeft) return true;
            if (!left && isRight) return true;
        }
        return left ? (_leftHand != null && _leftHand.gameObject.activeInHierarchy)
                    : (_rightHand != null && _rightHand.gameObject.activeInHierarchy);
    }

    bool Pinch(bool left)
    {
        var readers = _origin.GetComponentsInChildren<ReleaseThresholdButtonReader>(true);
        for (int i = 0; i < readers.Length; i++)
        {
            if (readers[i] == null || !readers[i].isActiveAndEnabled) continue;
            bool underLeft = IsUnderNamed(readers[i].transform, "Left Hand");
            bool underRight = IsUnderNamed(readers[i].transform, "Right Hand");
            if (left && underLeft && readers[i].ReadIsPerformed()) return true;
            if (!left && underRight && readers[i].ReadIsPerformed()) return true;
        }

        XRHandSubsystem hands = RunningHands();
        if (hands == null) return false;
        XRHand hand = left ? hands.leftHand : hands.rightHand;
        if (!hand.isTracked) return false;
        if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose index)) return false;
        if (!hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumb)) return false;
        return Vector3.Distance(index.position, thumb.position) < 0.025f;
    }

    void RefreshGrabState()
    {
        _hovered = null;
        _grabbed = null;
        var grabs = FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < grabs.Length; i++)
        {
            var g = grabs[i];
            if (g == null) continue;
            if (g.isSelected)
                _grabbed = g;
            else if (g.isHovered && _hovered == null)
                _hovered = g;
        }
    }

    XRHandSubsystem RunningHands()
    {
        _subsystems.Clear();
        SubsystemManager.GetSubsystems(_subsystems);
        for (int i = 0; i < _subsystems.Count; i++)
        {
            if (_subsystems[i] != null && _subsystems[i].running)
                return _subsystems[i];
        }
        return null;
    }

    static bool IsUnderHand(Transform t)
    {
        return IsUnderNamed(t, "Left Hand") || IsUnderNamed(t, "Right Hand");
    }

    static bool IsUnderNamed(Transform t, string name)
    {
        while (t != null)
        {
            if (t.name == name) return true;
            t = t.parent;
        }
        return false;
    }

    static Transform FindNamed(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindNamed(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static string Yn(bool v) => v ? "SI" : "NO";
}
