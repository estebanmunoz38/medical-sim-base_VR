using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

/// <summary>
/// Estabiliza la sesión quirúrgica sin crear un sistema de locomoción nuevo:
/// ancla el giro del rig, apaga el rayo de teleport sobre el campo y
/// reemplaza materiales Standard/error que en URP se ven magenta.
/// </summary>
[DefaultExecutionOrder(-20)]
public class SurgicalSessionGuard : MonoBehaviour
{
    XROrigin _origin;
    Quaternion _lockedYaw = Quaternion.identity;
    bool _yawReady;
    Material _fallback;
    readonly Dictionary<int, Material> _replacements = new Dictionary<int, Material>();

    void Start()
    {
        _origin = FindFirstObjectByType<XROrigin>();
        DisableUnwantedLocomotion();
        RepairBrokenMaterials();
        StartCoroutine(SettleAfterBootstrap());
    }

    System.Collections.IEnumerator SettleAfterBootstrap()
    {
        yield return null;
        yield return new WaitForSeconds(0.6f);
        DisableUnwantedLocomotion();
        RepairBrokenMaterials();
    }

    void LateUpdate()
    {
        LockRigYaw();
    }

    void LockRigYaw()
    {
        if (_origin == null)
            _origin = FindFirstObjectByType<XROrigin>();
        if (_origin == null)
            return;

        if (!_yawReady)
        {
            _lockedYaw = _origin.transform.rotation;
            _yawReady = true;
            return;
        }

        if (Quaternion.Angle(_origin.transform.rotation, _lockedYaw) < 0.4f)
            return;

        Vector3 position = _origin.transform.position;
        _origin.transform.rotation = _lockedYaw;
        _origin.transform.position = position;
    }

    void DisableUnwantedLocomotion()
    {
        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            var behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            string typeName = behaviour.GetType().Name;
            if (typeName == "SnapTurnProvider" || typeName == "ContinuousTurnProvider"
                || typeName == "GrabMoveProvider" || typeName == "TeleportationProvider")
            {
                behaviour.enabled = false;
                continue;
            }

            if (typeName == "TeleportationAnchor" || typeName == "TeleportationArea")
            {
                if (behaviour.transform.position.y > 0.35f)
                    behaviour.enabled = false;
                continue;
            }

            if (typeName == "NearFarInteractor" && !IsUnderHand(behaviour.transform))
            {
                var farCast = behaviour.GetType().GetProperty("enableFarCasting");
                if (farCast != null && farCast.CanWrite && farCast.PropertyType == typeof(bool))
                    farCast.SetValue(behaviour, false);
            }

            if (!IsTeleportRay(behaviour))
                continue;

            behaviour.enabled = false;
        }
    }

    static bool IsUnderHand(Transform cursor)
    {
        while (cursor != null)
        {
            if (cursor.name == "Left Hand" || cursor.name == "Right Hand")
                return true;
            cursor = cursor.parent;
        }

        return false;
    }

    static bool IsTeleportRay(MonoBehaviour behaviour)
    {
        string typeName = behaviour.GetType().Name;
        if (typeName != "XRRayInteractor" && typeName != "NearFarInteractor")
            return false;

        Transform cursor = behaviour.transform;
        while (cursor != null)
        {
            if (cursor.name.IndexOf("Teleport", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            cursor = cursor.parent;
        }

        return false;
    }

    void RepairBrokenMaterials()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
            lit = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (lit == null)
            return;

        if (_fallback == null)
        {
            _fallback = new Material(lit);
            _fallback.color = new Color(0.72f, 0.7f, 0.68f, 1f);
            _fallback.name = "SimugiasFallbackLit";
        }

        var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer)
                continue;
            if (renderer.GetComponentInParent<Canvas>() != null)
                continue;

            var shared = renderer.sharedMaterials;
            bool changed = false;
            for (int m = 0; m < shared.Length; m++)
            {
                if (!IsBroken(shared[m]))
                    continue;
                shared[m] = FallbackFor(shared[m], lit);
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = shared;
        }
    }

    Material FallbackFor(Material source, Shader lit)
    {
        if (source == null)
            return _fallback;

        int id = source.GetInstanceID();
        if (_replacements.TryGetValue(id, out Material existing) && existing != null)
            return existing;

        var replacement = new Material(lit);
        replacement.name = source.name + " (URP)";
        if (source.HasProperty("_MainTex"))
            replacement.mainTexture = source.GetTexture("_MainTex");
        if (source.HasProperty("_BaseMap") && replacement.mainTexture == null)
            replacement.mainTexture = source.GetTexture("_BaseMap");
        if (source.HasProperty("_Color"))
            replacement.color = source.color;
        else if (source.HasProperty("_BaseColor"))
            replacement.color = source.GetColor("_BaseColor");

        _replacements[id] = replacement;
        return replacement;
    }

    static bool IsBroken(Material material)
    {
        if (material == null || material.shader == null)
            return true;

        string shader = material.shader.name;
        if (shader == "Standard" || shader == "Legacy Shaders/Diffuse")
            return true;
        if (shader.IndexOf("InternalError", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (shader.IndexOf("Hidden/InternalErrorShader", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        return false;
    }
}
