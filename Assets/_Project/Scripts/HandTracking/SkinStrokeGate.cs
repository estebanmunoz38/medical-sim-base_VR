using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SkinStrokeGate : MonoBehaviour
{
    public static event Action StrokeOnSkin;

    static readonly string[] SkinNames =
    {
        "piel", "skin", "cabeza", "head", "craneo", "cráneo", "bebe", "bebé", "fontanela", "frente"
    };

    XRGrabInteractable grab;
    Transform tip;
    Behaviour[] painters;
    bool stroking;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        tip = FindTip(transform);
        painters = CollectPainters();
    }

    void LateUpdate()
    {
        bool held = grab != null && grab.isSelected;
        bool onSkin = held && TipOnSkin();
        if (onSkin && !stroking)
            StrokeOnSkin?.Invoke();
        stroking = onSkin;
    }

    bool TipOnSkin()
    {
        Vector3 origin = tip.position - tip.forward * 0.008f;
        if (!Physics.Raycast(origin, tip.forward, out RaycastHit hit, 0.02f, ~0, QueryTriggerInteraction.Ignore))
            return false;

        Transform cursor = hit.collider.transform;
        while (cursor != null)
        {
            string name = cursor.name.ToLowerInvariant();
            for (int i = 0; i < SkinNames.Length; i++)
            {
                if (name.Contains(SkinNames[i]))
                    return true;
            }

            cursor = cursor.parent;
        }

        return false;
    }

    Behaviour[] CollectPainters()
    {
        var found = new System.Collections.Generic.List<Behaviour>();
        MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null)
                continue;
            string typeName = behaviours[i].GetType().Name;
            if (typeName.Contains("CwHit") || typeName == "Draw")
                found.Add(behaviours[i]);
        }

        return found.ToArray();
    }

    static Transform FindTip(Transform root)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string name = children[i].name.ToLowerInvariant();
            if (name.Contains("tip") || name.Contains("punta"))
                return children[i];
        }

        return root;
    }
}
