using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SurgicalTool : MonoBehaviour
{
    #region Fields
    protected HandGestureManager activeGestures;
    protected XRGrabInteractable grab;
    
    public HandGestureManager ActiveGestures
    {
        get => activeGestures;
    }

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
        grab.selectExited.AddListener(OnReleased);
    }
    
    protected virtual void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }
    #endregion
    
    #region Private Methods
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // El interactor que agarró el objeto
        var interactorGO = args.interactorObject.transform;

        activeGestures = interactorGO.GetComponentInParent<HandGestureManager>();
        
        OnToolGrabbed();
        
        if (activeGestures == null)
            Debug.LogWarning("Tool grabbed but no HandGestureManager found.");
    }
    
    private void OnReleased(SelectExitEventArgs args)
    {
        activeGestures = null;
        
        OnToolReleased();
    }
    
    // Hooks para hijos
    protected virtual void OnToolGrabbed() { }
    protected virtual void OnToolReleased() { }
    #endregion
}
