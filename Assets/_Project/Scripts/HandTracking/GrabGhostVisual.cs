using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GrabGhostVisual : MonoBehaviour
{
    XRGrabInteractable grab;
    GameObject root;
    bool visible;

    public void Build(Transform attach, GrabGestureKind gesture)
    {
        grab = GetComponent<XRGrabInteractable>();
        root = new GameObject("GrabGhost");
        root.transform.SetParent(attach, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;

        if (gesture == GrabGestureKind.Fist)
        {
            Material material = HandVisualMaterial.Create(new Color(1f, 0.42f, 0.08f, 1f), 0.9f);
            AddDot(root.transform, material, new Vector3(0f, 0.04f, 0f), 0.028f);
            AddDot(root.transform, material, new Vector3(0.022f, 0.022f, 0.01f), 0.014f);
            AddDot(root.transform, material, new Vector3(0.008f, 0.02f, 0.022f), 0.014f);
            AddDot(root.transform, material, new Vector3(-0.012f, 0.02f, 0.02f), 0.014f);
            AddDot(root.transform, material, new Vector3(-0.024f, 0.018f, 0.004f), 0.014f);
            HandTrackingLog.Write("Interaction",
                $"{name}: guía naranja visible. Para agarrar, cerrá la mano sobre esa guía.");
        }
        else
        {
            Material material = HandVisualMaterial.Create(new Color(0.15f, 0.9f, 1f, 1f), 0.9f);
            Transform thumb = AddDot(root.transform, material, new Vector3(0.02f, 0.03f, 0f), 0.016f);
            Transform index = AddDot(root.transform, material, new Vector3(-0.02f, 0.03f, 0f), 0.016f);
            AddLine(root.transform, material, thumb.localPosition, index.localPosition);
            HandTrackingLog.Write("Interaction",
                $"{name}: guía celeste visible. Para agarrar, juntá pulgar e índice sobre esa guía.");
        }

        visible = true;
    }

    static Transform AddDot(Transform parent, Material material, Vector3 localPosition, float scale)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(sphere.GetComponent<Collider>());
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = localPosition;
        sphere.transform.localScale = Vector3.one * scale;
        var renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        return sphere.transform;
    }

    static void AddLine(Transform parent, Material material, Vector3 from, Vector3 to)
    {
        var lineObject = new GameObject("PinchGuide");
        lineObject.transform.SetParent(parent, false);
        var line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.startWidth = line.endWidth = 0.004f;
        line.sharedMaterial = material;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
    }

    void LateUpdate()
    {
        if (root == null || grab == null)
            return;

        bool show = !grab.isSelected;
        if (show == visible)
            return;

        visible = show;
        root.SetActive(show);
        HandTrackingLog.Write("Interaction",
            $"{name} guía visual. Previo={(show ? "oculta" : "visible")}. Resultado={(show ? "visible" : "oculta")}.");
    }
}

public enum GrabGestureKind
{
    Pinch,
    Fist
}
