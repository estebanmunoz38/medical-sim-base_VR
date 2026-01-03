using UnityEngine;
using System;
using Logic.SurgicalProcedure;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class SurgicalStep : MonoBehaviour
{
    [Header("Surgical Step")]
    [SerializeField] public SurgicalStepId StepId;
    [Space]
    [Header("Surgical Events")]
    public UnityEvent<SurgicalStep> OnStepStarted;
    public UnityEvent<SurgicalStep> OnStepCompleted;
    public UnityEvent<SurgicalStep,string> OnStepFailed;

    protected bool isEnabled = false;
    protected bool trabajando = false;
    protected bool terminado = false;

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
        trabajando = true;
        terminado = false;
        OnStepStarted?.Invoke(this);
    }

    public virtual void EndStep()
    {
        terminado = true;
        OnStepCompleted?.Invoke(this);
    }

    public virtual void FailStep(string reason)
    {
        terminado = true;
        OnStepFailed?.Invoke(this, reason);
    }
    #endregion
}