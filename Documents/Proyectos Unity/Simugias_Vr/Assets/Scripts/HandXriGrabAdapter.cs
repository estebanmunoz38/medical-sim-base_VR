using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Adapta herramientas a manos sin pisar puntos de agarre ni la pantalla
/// del endoscopio. No reescribe Kerrison / Bisturí / Handler.
/// </summary>
[DefaultExecutionOrder(50)]
public class HandXriGrabAdapter : MonoBehaviour
{
    [SerializeField] bool adaptAllGrabInteractables = true;
    [SerializeField] bool adaptKnownMedicalTools = true;

    static readonly System.Type[] KnownTools =
    {
        typeof(Kerrison),
        typeof(BisturiCutControl),
        typeof(Drill),
        typeof(DrillVRTool),
        typeof(Hemostasico),
        typeof(HemostasicoTool),
        typeof(Retractor),
        typeof(RetractorVRTool),
        typeof(ScalpelVRTool),
        typeof(MarkerVRTool),
        typeof(Coagulador),
        typeof(ShaveVRTool)
    };

    public void Adapt()
    {
        if (adaptAllGrabInteractables)
        {
            var grabs = FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < grabs.Length; i++)
                AdaptOne(grabs[i]);
        }

        if (adaptKnownMedicalTools)
            AdaptKnownTools();
    }

    public static void AdaptOne(XRGrabInteractable grab)
    {
        if (grab == null) return;
        if (IsScreenOrUi(grab.transform) && !IsDesignedToolGrab(grab))
            return;

        grab.interactionLayers = ~0;
        grab.useDynamicAttach = !HasDesignedAttach(grab);

        if (grab.colliders != null && grab.colliders.Count > 0)
            return;

        if (grab.GetComponentInChildren<Collider>(true) == null
            && grab.transform.lossyScale.sqrMagnitude < 9f)
            grab.gameObject.AddComponent<BoxCollider>();

        if (grab.GetComponent<Rigidbody>() == null)
        {
            var body = grab.gameObject.AddComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            if (grab.transform.parent != null)
                body.isKinematic = true;
        }
    }

    public static void AdaptKnownTools()
    {
        for (int t = 0; t < KnownTools.Length; t++)
        {
            var found = Object.FindObjectsByType(KnownTools[t], FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                var comp = found[i] as Component;
                if (comp == null) continue;
                if (IsFixture(comp.transform)) continue;

                var grab = comp.GetComponent<XRGrabInteractable>();
                if (grab == null)
                    grab = comp.GetComponentInParent<XRGrabInteractable>();
                if (grab == null)
                    continue;
                AdaptOne(grab);
            }
        }
    }

    public static bool HasDesignedAttach(XRGrabInteractable grab)
    {
        if (grab == null) return false;
        if (grab.attachTransform != null) return true;
        if (grab is ClosestAttachGrabInteractable) return true;
        if (grab.GetComponent<XRIArmIKFollow>() != null) return true;
        return FindGrabPoint(grab.transform) != null;
    }

    public static Transform FindGrabPoint(Transform root)
    {
        if (root == null) return null;
        string n = root.name;
        if (n == "GrabPoint" || n == "Grabs" || n == "HandleA" || n == "HandleB")
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindGrabPoint(root.GetChild(i));
            if (found != null) return found;
        }
        return null;
    }

    public static bool IsDesignedToolGrab(XRGrabInteractable grab)
    {
        if (grab == null) return false;
        if (grab.GetComponent<EndoscopioHandler>() != null) return true;
        return IsHandleName(grab.name);
    }

    public static bool IsHandleName(string n)
    {
        if (string.IsNullOrEmpty(n)) return false;
        return n.IndexOf("Unlock", System.StringComparison.OrdinalIgnoreCase) >= 0
               || n.IndexOf("Handler", System.StringComparison.OrdinalIgnoreCase) >= 0
               || n.IndexOf("GrabPoint", System.StringComparison.OrdinalIgnoreCase) >= 0
               || n.IndexOf("HandleA", System.StringComparison.OrdinalIgnoreCase) >= 0
               || n.IndexOf("HandleB", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool IsFixture(Transform t)
    {
        return t.GetComponentInParent<MechanicalCeilingArm>() != null
               || t.GetComponentInParent<CeilingArm2DOF_HandleDriven>() != null;
    }

    public static bool IsScreenOrUi(Transform t)
    {
        while (t != null)
        {
            if (IsHandleName(t.name))
                return false;
            string n = t.name;
            if (n.IndexOf("EndoscopioScreen", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Endoscopio_UI", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n == "Screen"
                || n.StartsWith("Screen ", System.StringComparison.Ordinal)
                || n.IndexOf("Screen Filter", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("CoachingCardRoot", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (t.GetComponent<Canvas>() != null)
                return true;
            t = t.parent;
        }
        return false;
    }

    void Start()
    {
        Adapt();
    }
}
