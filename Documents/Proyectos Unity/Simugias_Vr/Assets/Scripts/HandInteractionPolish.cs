using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Resuelve choques de agarre: pantalla vs endoscopio vs puntos de mango.
/// No cambia la lógica médica; solo limita colliders y attach.
/// </summary>
[DefaultExecutionOrder(90)]
public class HandInteractionPolish : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoBoot()
    {
        if (!SimugiasRuntimeGate.AllowMedicalRuntime())
            return;
        if (FindFirstObjectByType<HandInteractionPolish>() != null)
            return;
        if (FindFirstObjectByType<Kerrison>() == null && FindFirstObjectByType<Endoscopio>() == null)
            return;

        var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        var host = origin != null ? origin.gameObject : new GameObject("HandInteractionPolish");
        if (host.GetComponent<HandInteractionPolish>() == null)
            host.AddComponent<HandInteractionPolish>();
    }

    public static void RunNow()
    {
        var polish = FindFirstObjectByType<HandInteractionPolish>();
        if (polish == null)
        {
            var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            var host = origin != null ? origin.gameObject : new GameObject("HandInteractionPolish");
            polish = host.GetComponent<HandInteractionPolish>();
            if (polish == null)
                polish = host.AddComponent<HandInteractionPolish>();
        }
        polish.Polish();
    }

    void Start()
    {
        StartCoroutine(PolishAfterAdapters());
    }

    IEnumerator PolishAfterAdapters()
    {
        yield return null;
        yield return null;
        yield return null;
        Polish();
    }

    [ContextMenu("Polish Grab Conflicts Now")]
    public void Polish()
    {
        StripGrabFromScreensAndUi();
        DisableOversizedScreenColliders();
        RestoreDesignedAttachPoints();
        RemoveConflictingGrabVolumes();
        RestrictToListedColliders();
    }

    static void StripGrabFromScreensAndUi()
    {
        var grabs = FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < grabs.Length; i++)
        {
            var grab = grabs[i];
            if (grab == null) continue;
            if (HandXriGrabAdapter.IsDesignedToolGrab(grab))
                continue;
            if (!HandXriGrabAdapter.IsScreenOrUi(grab.transform))
                continue;

            Destroy(grab);
        }
    }

    static void DisableOversizedScreenColliders()
    {
        var cols = FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cols.Length; i++)
        {
            var col = cols[i];
            if (col == null || !col.enabled) continue;
            if (col.GetComponentInParent<Canvas>() != null)
                continue;
            if (HandXriGrabAdapter.IsHandleName(col.transform.name)
                || col.GetComponentInParent<EndoscopioHandler>() != null)
                continue;
            if (!IsEndoscopeDisplayCollider(col.transform))
                continue;

            Vector3 size = col.bounds.size;
            if (Mathf.Max(size.x, size.y, size.z) < 0.35f)
                continue;
            col.enabled = false;
        }
    }

    static bool IsEndoscopeDisplayCollider(Transform t)
    {
        while (t != null)
        {
            string n = t.name;
            if (n == "EndoscopioScreen" || n == "Endoscopio_UI" || n == "Screen"
                || n.StartsWith("Screen ") || n.IndexOf("Screen Filter", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            t = t.parent;
        }
        return false;
    }

    static void RestoreDesignedAttachPoints()
    {
        var grabs = FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < grabs.Length; i++)
        {
            var grab = grabs[i];
            if (grab == null) continue;

            if (grab.attachTransform == null)
            {
                Transform designed = HandXriGrabAdapter.FindGrabPoint(grab.transform);
                if (designed != null)
                    grab.attachTransform = designed;
            }

            grab.useDynamicAttach = !HandXriGrabAdapter.HasDesignedAttach(grab);
            if (KeepConstrainedMotion(grab))
                continue;

            grab.trackPosition = true;
            grab.trackRotation = true;
            grab.smoothPosition = false;
            grab.smoothRotation = false;
            grab.throwOnDetach = false;
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        }
    }

    static bool KeepConstrainedMotion(XRGrabInteractable grab)
    {
        if (grab == null)
            return true;
        if (grab.GetComponent<XRIArmIKFollow>() != null)
            return true;
        if (grab.GetComponentInParent<MechanicalCeilingArm>() != null)
            return true;
        if (grab.GetComponentInParent<CeilingArm2DOF_HandleDriven>() != null)
            return true;

        var behaviours = grab.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null)
                continue;
            string n = behaviours[i].GetType().Name;
            if (n.IndexOf("XRLever", System.StringComparison.Ordinal) >= 0
                || n.IndexOf("XRKnob", System.StringComparison.Ordinal) >= 0)
                return true;
        }

        return false;
    }

    static void RemoveConflictingGrabVolumes()
    {
        var grabs = FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < grabs.Length; i++)
        {
            var grab = grabs[i];
            if (grab == null) continue;
            if (!HandXriGrabAdapter.HasDesignedAttach(grab)
                && (grab.colliders == null || grab.colliders.Count == 0))
                continue;

            Transform vol = grab.transform.Find("HandGrabVolume");
            if (vol != null)
                Destroy(vol.gameObject);
        }
    }

    static void RestrictToListedColliders()
    {
        var grabs = FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < grabs.Length; i++)
        {
            var grab = grabs[i];
            if (grab == null || grab.colliders == null || grab.colliders.Count == 0)
                continue;

            var extras = grab.GetComponentsInChildren<Collider>(true);
            for (int c = 0; c < extras.Length; c++)
            {
                if (extras[c] == null) continue;
                if (extras[c].transform.name != "HandGrabVolume")
                    continue;
                extras[c].enabled = false;
            }
        }
    }
}
