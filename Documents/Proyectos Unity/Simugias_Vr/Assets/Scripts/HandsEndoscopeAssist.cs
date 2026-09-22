using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Con manos, el pellizco de la mano que sostiene el endoscopio avanza;
/// el pellizco de la otra mano retira. Evita depender de B/Y o stick.
/// </summary>
[DefaultExecutionOrder(-45)]
public class HandsEndoscopeAssist : MonoBehaviour
{
    CompositeToolInputSource _composite;
    HandPinchToolInput _hands;
    XRGrabInteractable _endoGrab;
    bool _holding;

    void Start()
    {
        _composite = FindFirstObjectByType<CompositeToolInputSource>();
        _hands = FindFirstObjectByType<HandPinchToolInput>();
        ResolveEndoscope();
    }

    void ResolveEndoscope()
    {
        var handlers = FindObjectsByType<EndoscopioHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        XRGrabInteractable unlock = null;
        XRGrabInteractable handler = null;
        for (int i = 0; i < handlers.Length; i++)
        {
            if (handlers[i] == null) continue;
            var grab = handlers[i].GetComponent<XRGrabInteractable>();
            if (grab == null) continue;
            if (handlers[i].name.IndexOf("Handler", System.StringComparison.OrdinalIgnoreCase) >= 0)
                handler = grab;
            else
                unlock = grab;
        }
        if (handler != null && handler.gameObject.activeInHierarchy)
            _endoGrab = handler;
        else if (unlock != null)
            _endoGrab = unlock;
        else
            _endoGrab = handler != null ? handler : unlock;

        if (_endoGrab != null)
            return;

        var vr = FindFirstObjectByType<EndoscopeVRTool>(FindObjectsInactive.Include);
        if (vr != null)
        {
            var grab = vr.GetComponent<XRGrabInteractable>();
            if (grab == null)
                grab = vr.GetComponentInParent<XRGrabInteractable>();
            if (grab != null && !HandXriGrabAdapter.IsScreenOrUi(grab.transform))
                _endoGrab = grab;
        }
    }

    void Update()
    {
        if (!HandsOnlySession.Active) return;
        if (_hands == null)
            _hands = FindFirstObjectByType<HandPinchToolInput>();
        if (_composite == null)
            _composite = FindFirstObjectByType<CompositeToolInputSource>();
        if (_endoGrab == null || !_endoGrab.gameObject.activeInHierarchy)
            ResolveEndoscope();

        _holding = _endoGrab != null && _endoGrab.isSelected;
        if (!_holding || _hands == null || _composite == null)
        {
            _composite?.ClearHandOverride();
            return;
        }

        bool leftHold = IsInteractorSide(true);
        bool primary = leftHold ? _hands.LeftPrimaryHeld : _hands.RightPrimaryHeld;
        bool secondary = leftHold ? _hands.RightPrimaryHeld : _hands.LeftPrimaryHeld;
        if (!leftHold && !IsInteractorSide(false))
        {
            primary = _hands.PrimaryHeld;
            secondary = false;
        }

        _composite.SetHandOverride(primary, secondary);
    }

    bool IsInteractorSide(bool left)
    {
        if (_endoGrab == null || _endoGrab.interactorsSelecting.Count == 0)
            return false;
        var interactor = _endoGrab.interactorsSelecting[0];
        if (interactor == null) return false;
        Transform t = interactor.transform;
        while (t != null)
        {
            if (left && t.name == "Left Hand") return true;
            if (!left && t.name == "Right Hand") return true;
            t = t.parent;
        }
        return false;
    }
}
