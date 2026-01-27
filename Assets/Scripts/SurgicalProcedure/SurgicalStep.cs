using UnityEngine;
using System;
using Logic.SurgicalProcedure;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SurgicalStep : MonoBehaviour
{
    [Header("Surgical Step")]
    [SerializeField] public SurgicalStepId StepId;
    [Space]
    [Header("Surgical Events")]
    public bool shouldLockTool = false;
    public UnityEvent<SurgicalStep> OnStepStarted;
    public UnityEvent<SurgicalStep> OnStepCompleted;
    public UnityEvent<SurgicalStep,string> OnStepFailed;
    [Space]
    [Header("Tool")]
    [SerializeField] public Transform toolModel;
    [SerializeField] public Transform toolTip;   
    
    protected IHandGestureProvider activeHandGestures;
    protected SurgicalTool surgicalTool;
    protected bool isEnabled = false;
    protected bool trabajando = false;
    protected bool terminado = false;

    #region Unity Methods

    protected virtual void Start()
    {
        if (surgicalTool != null)
        {
            SurgicalTool st = toolModel.GetComponent<SurgicalTool>();
            surgicalTool = st != null ? st : toolModel.gameObject.AddComponent<SurgicalTool>();

            activeHandGestures = surgicalTool.ActiveGestures;
        }
        
    }
    #endregion
    
    #region Public Methods
    public void EnableStep()
    {
        isEnabled = true;
    }

    public void DisableStep()
    {
        isEnabled = false;
    }

    public virtual void StartStep()
    {
        OnStepStarted?.Invoke(this);

        if (shouldLockTool)
        {
            // Apply Lock Logic
            //toolModel.GetComponent<ToolInteractionLock>().Lock();
        }
    }

    public virtual void EndStep()
    {
        OnStepCompleted?.Invoke(this);
        
        if (shouldLockTool)
        {
            // Apply Unlock Logic
            // toolModel.GetComponent<ToolInteractionLock>().Unlock();
        }
    }

    public virtual void FailStep(string reason)
    {
        OnStepFailed?.Invoke(this, reason);
    }
    #endregion
}