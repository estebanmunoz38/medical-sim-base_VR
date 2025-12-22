using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Interface general para todas las herramientas VR.
/// Las tools leen de acá, nunca directamente del Input System.
/// </summary>
public interface IToolInputSource
{
    bool PrimaryDown { get; }
    bool PrimaryHeld { get; }
    bool PrimaryUp { get; }

    bool SecondaryDown { get; }
    bool SecondaryHeld { get; }
    bool SecondaryUp { get; }

    float ScrollDelta { get; }

    Ray PointerRay { get; }
}


// ======================================================================
// XR INPUT SOURCE (Oculus / Quest / OpenXR / Unity Input System)
// ======================================================================
public class XRToolInputSource : MonoBehaviour, IToolInputSource
{
#if ENABLE_INPUT_SYSTEM
    [Header("Input Actions (XR)")]
    public InputActionProperty primaryAction;
    public InputActionProperty secondaryAction;
    public InputActionProperty scrollAxisAction;

    [Header("Ray Origin (Controller tip)")]
    public Transform pointerOrigin;

    private bool primary_prev = false;
    private bool secondary_prev = false;

    void OnEnable()
    {
        if (primaryAction.action != null) primaryAction.action.Enable();
        if (secondaryAction.action != null) secondaryAction.action.Enable();
        if (scrollAxisAction.action != null) scrollAxisAction.action.Enable();
    }

    void OnDisable()
    {
        if (primaryAction.action != null) primaryAction.action.Disable();
        if (secondaryAction.action != null) secondaryAction.action.Disable();
        if (scrollAxisAction.action != null) scrollAxisAction.action.Disable();
    }

    // PRIMARY BUTTON
    public bool PrimaryDown
    {
        get
        {
            bool curr = primaryAction.action != null && primaryAction.action.IsPressed();
            bool r = curr && !primary_prev;
            primary_prev = curr;
            return r;
        }
    }

    public bool PrimaryHeld =>
        (primaryAction.action != null && primaryAction.action.IsPressed());

    public bool PrimaryUp
    {
        get
        {
            bool curr = primaryAction.action != null && primaryAction.action.IsPressed();
            bool r = !curr && primary_prev;
            primary_prev = curr;
            return r;
        }
    }

    // SECONDARY BUTTON
    public bool SecondaryDown
    {
        get
        {
            bool curr = secondaryAction.action != null && secondaryAction.action.IsPressed();
            bool r = curr && !secondary_prev;
            secondary_prev = curr;
            return r;
        }
    }

    public bool SecondaryHeld =>
        (secondaryAction.action != null && secondaryAction.action.IsPressed());

    public bool SecondaryUp
    {
        get
        {
            bool curr = secondaryAction.action != null && secondaryAction.action.IsPressed();
            bool r = !curr && secondary_prev;
            secondary_prev = curr;
            return r;
        }
    }

    // SCROLL (joystick vertical o gatillo alternativo)
    public float ScrollDelta =>
        scrollAxisAction.action != null ? scrollAxisAction.action.ReadValue<float>() : 0f;

    // RAY DEL CONTROLADOR
    public Ray PointerRay =>
        new Ray(pointerOrigin.position, pointerOrigin.forward);

#else

    // ======================================================================
    // SIN NUEVO INPUT SYSTEM (Editor PC sin XR)
    // ======================================================================
    public bool PrimaryDown => false;
    public bool PrimaryHeld => false;
    public bool PrimaryUp => false;

    public bool SecondaryDown => false;
    public bool SecondaryHeld => false;
    public bool SecondaryUp => false;

    public float ScrollDelta => 0f;

    public Ray PointerRay => new Ray(Vector3.zero, Vector3.forward);

#endif
}
