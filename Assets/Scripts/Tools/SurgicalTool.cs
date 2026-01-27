using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SurgicalTool : MonoBehaviour
{
    #region Fields
    [Header("Grab Settings")]
    public float releasePinchThreshold = 0.2f;
    public float minHoldTime = 0.15f;
    
    protected HandGestureManager activeGestures;
    protected XRGrabInteractable grab;
    
    private float holdTimer;
    private bool isLatched;
    private bool allowRelease;
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
    }

    protected void Update()
    {
        if (!isLatched)
            return;

        if (activeGestures == null)
        {
            allowRelease = true;
            ForceRelease();
            return;
        }
            
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

        activeGestures = interactorGO.GetComponentInParent<HandGestureManager>();
        
        holdTimer = 0f;
        isLatched = true;
        allowRelease = false;
        
        OnToolGrabbed();
        
        if (activeGestures == null)
            Debug.LogWarning("Tool grabbed but no HandGestureManager found.");
    }

    private void OnReleaseAttempt(SelectExitEventArgs args)
    {
        // Cancelamos el release automático
        if (!allowRelease)
        {
            // Bloqueamos el release
            grab.interactionManager.SelectEnter(
                args.interactorObject,
                grab
            );
            return;
        }
        
        // Release real
        isLatched = false;
        activeGestures = null;
        OnToolReleased();
    }

    private void ForceRelease()
    {
        if (grab.firstInteractorSelecting == null)
        {
            isLatched = false;
            activeGestures = null;
            OnToolReleased();
            return;
        }

        isLatched = false;

        grab.interactionManager.SelectExit(
            grab.firstInteractorSelecting,
            grab
        );
        
    }
    
    // Hooks para hijos
    protected virtual void OnToolGrabbed() { }
    protected virtual void OnToolReleased() { }
    #endregion
}
