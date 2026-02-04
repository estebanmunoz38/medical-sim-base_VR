using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SurgicalTool : MonoBehaviour
{
    #region Fields
    [Header("Grab Settings")]
    public float releasePinchThreshold = 0.2f;
    public float minHoldTime = 0.15f;
    
    protected IHandGestureProvider activeGestures;
    protected XRGrabInteractable grab;
    
    private float holdTimer;
    private bool isLatched;
    private bool allowRelease;
    
    public IHandGestureProvider ActiveGestures => activeGestures;
    #endregion
    
    #region Unity Methods
    protected virtual void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        
        if (!grab)
        {
            Debug.LogError($"{name} requires XRGrabInteractable");
            enabled = false;
        }
    }
    
    protected virtual void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleaseAttempt);
        
    }
    
    protected virtual void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleaseAttempt);
        
        if (activeGestures != null)
        {
            activeGestures.OnSecondaryActivated -= OnToolActivated;
            activeGestures.OnSecondaryDeactivated -= OnToolDeactivated;
        }
    }

    protected void Update()
    {
        if (!isLatched || activeGestures == null)
            return;

        holdTimer += Time.deltaTime;

        if (holdTimer > minHoldTime &&
            activeGestures.Pinch < releasePinchThreshold)
        {
            allowRelease = true;
            ForceRelease();
        }
    }

    #endregion
    
    #region Private Methods
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // El interactor que agarró el objeto
        var interactorGO = args.interactorObject.transform;

        activeGestures = interactorGO.GetComponentInParent<IHandGestureProvider>();
        
        activeGestures.OnSecondaryActivated += OnToolActivated;
        activeGestures.OnSecondaryDeactivated += OnToolDeactivated;
        
        // Fallback si no hay gesture provider
        if (activeGestures == null)
        {
            return;
        }
        
        holdTimer = 0f;
        isLatched = true;
        allowRelease = false;
        
        OnToolGrabbed();
    }

    private void OnReleaseAttempt(SelectExitEventArgs args)
    {
        // Si no permitimos soltar, simplemente ignoramos
        if (!allowRelease)
        {
            // Volvemos a marcar el estado interno
            isLatched = true;
            return;
        }

        // Release real
        isLatched = false;
        activeGestures.OnSecondaryActivated -= OnToolActivated;
        activeGestures.OnSecondaryDeactivated -= OnToolDeactivated;
        activeGestures = null;
        
        OnToolReleased();
    }

    private void ForceRelease()
    {
        if (grab.firstInteractorSelecting == null)
        {
            isLatched = false;
            activeGestures.OnSecondaryActivated -= OnToolActivated;
            activeGestures.OnSecondaryDeactivated -= OnToolDeactivated;
            activeGestures = null;
            OnToolReleased();
            return;
        }

        isLatched = false;

        grab.interactionManager.SelectExit(
            grab.firstInteractorSelecting,
            grab
        );
        
        activeGestures = null;
        OnToolReleased();
    }

    private void OnToolActivated()
    {
        grab.activated.Invoke(new ActivateEventArgs());
    }

    private void OnToolDeactivated()
    {
        grab.deactivated.Invoke(new DeactivateEventArgs());
    }
    
    // Hooks para hijos
    protected virtual void OnToolGrabbed() { }
    protected virtual void OnToolReleased() { }
    #endregion
}
