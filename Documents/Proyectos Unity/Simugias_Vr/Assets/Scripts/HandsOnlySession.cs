using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

/// <summary>
/// Prioriza Hand Tracking y cablea herramientas a las manos.
/// No apaga ni destruye Left/Right Controller: XRInputModalityManager
/// elige el dispositivo activo.
/// </summary>
[DefaultExecutionOrder(-180)]
public class HandsOnlySession : MonoBehaviour
{
    const string SettingsResource = "XR/HandsOnlySettings";

    static HandsOnlySettings _settings;
    static bool _loaded;

    readonly List<(GameObject go, bool wasActive)> _controllerBackup = new List<(GameObject, bool)>();
    bool _controllersHidden;

    public static HandsOnlySettings Settings
    {
        get
        {
            if (!_loaded)
            {
                _settings = Resources.Load<HandsOnlySettings>(SettingsResource);
                _loaded = true;
            }
            return _settings;
        }
    }

    public static bool Active => Settings == null || Settings.handsOnly;
    public static bool IgnoreControllerInput
    {
        get
        {
            if (!Active) return false;
            if (Settings != null && !Settings.ignoreControllerInput) return false;
            var hands = Object.FindFirstObjectByType<HandPinchToolInput>();
            return hands != null && hands.AnyHandTracked;
        }
    }
    public static bool HoldGrabThroughHiccups => Active && (Settings == null || Settings.holdGrabThroughTrackingHiccups);
    public static bool RunOnboarding => Active && (Settings == null || Settings.runOnboardingEverySession);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (!SimugiasRuntimeGate.AllowMedicalRuntime()) return;
        if (!Active) return;
        if (FindFirstObjectByType<HandsOnlySession>() != null) return;
        if (FindFirstObjectByType<Kerrison>() == null && FindFirstObjectByType<Endoscopio>() == null
            && FindFirstObjectByType<BisturiCutControl>() == null)
            return;

        var host = GameObject.Find("XR Origin (XR Rig)");
        if (host == null)
        {
            var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            host = origin != null ? origin.gameObject : new GameObject("HandsOnlySession");
        }

        var session = host.GetComponent<HandsOnlySession>();
        if (session == null)
            session = host.AddComponent<HandsOnlySession>();
        session.StartCoroutine(session.ApplyRoutine());
    }

    IEnumerator ApplyRoutine()
    {
        yield return null;
        yield return null;
        Apply();
    }

    [ContextMenu("Apply Hands Only Now")]
    public void Apply()
    {
        if (!Active) return;

        ForceHandModality();
        BindHandDrivenTools();
        RestoreControllers();
        HandsVisibilityGuard.Ensure(transform);

        if (GetComponent<HandGrabStability>() == null)
            gameObject.AddComponent<HandGrabStability>();
        if (GetComponent<HandsEndoscopeAssist>() == null)
            gameObject.AddComponent<HandsEndoscopeAssist>();
        HandInteractionPolish.RunNow();
    }

    [ContextMenu("Restore Controllers")]
    public void RestoreControllers()
    {
        for (int i = 0; i < _controllerBackup.Count; i++)
        {
            var entry = _controllerBackup[i];
            if (entry.go != null)
                entry.go.SetActive(entry.wasActive);
        }
        _controllerBackup.Clear();
        _controllersHidden = false;
    }

    public static Transform ResolveHandTransform(bool preferRight)
    {
        string first = preferRight ? "Right Hand" : "Left Hand";
        string second = preferRight ? "Left Hand" : "Right Hand";
        var a = GameObject.Find(first);
        if (a != null) return a.transform;
        var b = GameObject.Find(second);
        if (b != null) return b.transform;
        return Camera.main != null ? Camera.main.transform : null;
    }

    void HideControllers()
    {
        if (_controllersHidden) return;
        if (Settings != null && !Settings.disableControllersAtRuntime) return;

        string[] names =
        {
            "Left Controller", "Right Controller",
            "Left Controller Teleport", "Right Controller Teleport"
        };

        for (int i = 0; i < names.Length; i++)
            HideNamed(names[i]);

        var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (origin != null)
        {
            HideIfControllerBranch(origin.transform, "Left Controller");
            HideIfControllerBranch(origin.transform, "Right Controller");
        }

        _controllersHidden = true;
    }

    void HideNamed(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) return;
        _controllerBackup.Add((go, go.activeSelf));
        go.SetActive(false);
    }

    void HideIfControllerBranch(Transform root, string name)
    {
        var t = FindChildRecursive(root, name);
        if (t == null || !t.gameObject.activeSelf) return;
        _controllerBackup.Add((t.gameObject, true));
        t.gameObject.SetActive(false);
    }

    void ForceHandModality()
    {
        var modality = GetComponentInChildren<XRInputModalityManager>(true);
        if (modality == null)
            modality = FindFirstObjectByType<XRInputModalityManager>();
        if (modality == null) return;

        var left = GameObject.Find("Left Hand");
        var right = GameObject.Find("Right Hand");
        var type = modality.GetType();
        SetField(type, modality, "m_LeftHand", left);
        SetField(type, modality, "m_RightHand", right);

    }

    void Update()
    {
        if (!Active) return;
        ForceHandModality();
    }

    void BindHandDrivenTools()
    {
        var right = ResolveHandTransform(true);
        var retractors = FindObjectsByType<RetractorVRTool>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < retractors.Length; i++)
        {
            var tool = retractors[i];
            if (tool == null) continue;
            if (tool.controllerTransform == null || Active)
                tool.controllerTransform = right;
            if (!tool.enabled)
                tool.enabled = true;
        }
    }

    static void SetField(System.Type type, object target, string name, object value)
    {
        var field = type.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(target, value);
    }

    static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindChildRecursive(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
