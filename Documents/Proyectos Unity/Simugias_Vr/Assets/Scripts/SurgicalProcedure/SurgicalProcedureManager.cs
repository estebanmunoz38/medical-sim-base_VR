using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Logic.SurgicalProcedure
{
    public enum SurgicalStepId
    {
        Idle,
        Scapel,
        SubcutaneousDissection,
        Drill,
        Hemostasis,
        BoneClosure,
        SkinPlasty,
        Completed
    }

    public enum SurgicalToolType
    {
        Scalpel,
        Retractor,
        Coagulator,
        Forceps,
        Needle
    }
    
    public class SurgicalProcedureManager : MonoBehaviour
    {
        [SerializeField] private SurgicalStep[] surgicalSteps;

        [SerializeField] private int currentStepIndex = 0;
        
        public SurgicalStep CurrentStep { get; private set; }
        
        [Header("Global Events")]
        public UnityEvent OnProcedureStarted;
        public UnityEvent<SurgicalStepId> OnStepCompleted;
        public UnityEvent OnProcedureCompleted;

        #region Unity Methods
        void Awake()
        {
            foreach (var step in surgicalSteps)
            {
                step.OnStepStarted.AddListener(HandleStepStarted);
                step.OnStepCompleted.AddListener(HandleStepCompleted);
                step.OnStepFailed.AddListener(HandleStepFailed);
                step.DisableStep();
            }

            //surgicalSteps[currentStepIndex].EnableStep();
        }

        private void Start()
        {
            StartProcedure();
        }

        private void OnDestroy()
        {
            foreach (var step in surgicalSteps)
            {
                step.OnStepStarted.RemoveListener(HandleStepStarted);
                step.OnStepCompleted.RemoveListener(HandleStepCompleted);
                step.OnStepFailed.RemoveListener(HandleStepFailed);
            }
        }
        #endregion

        #region Public Methods
        public void StartProcedure()
        {
            if (surgicalSteps.Length == 0)
            {
                Debug.LogError("No surgical steps configured.");
                return;
            }

            currentStepIndex = 0;
            SetCurrentStep(surgicalSteps[currentStepIndex]);
            OnProcedureStarted?.Invoke();
            Debug.Log("Starting Procedure");
        }

        public bool IsStepActive(SurgicalStepId stepId)
        {
            return CurrentStep != null && CurrentStep.StepId == stepId;
        }

        public bool CanUseTool(SurgicalToolType toolType)
        {
            if (CurrentStep == null) return false;

            return toolType switch
            {
                SurgicalToolType.Scalpel or SurgicalToolType.Retractor =>
                    CurrentStep.StepId == SurgicalStepId.SubcutaneousDissection,

                SurgicalToolType.Coagulator =>
                    CurrentStep.StepId == SurgicalStepId.Hemostasis,

                SurgicalToolType.Forceps =>
                    CurrentStep.StepId == SurgicalStepId.BoneClosure,

                SurgicalToolType.Needle =>
                    CurrentStep.StepId == SurgicalStepId.SkinPlasty,

                _ => false
            };
        }
        #endregion

        #region Private Methods
        #region Event Handlers
        private void HandleStepStarted(SurgicalStep step)
        {
            if (surgicalSteps[currentStepIndex] != step)
            {
                step.DisableStep();
                //step.OnStepFailed?.Invoke(step, "Paso fuera de orden");
                return;
            }

            Debug.Log($"Step started: {step.StepId.ToString()}");
        }
        
        void HandleStepCompleted(SurgicalStep step)
        {
            if (surgicalSteps[currentStepIndex] != step)
                return;

            Debug.Log($"Step completed: {step.StepId.ToString() }");

            step.DisableStep();
            OnStepCompleted?.Invoke(step.StepId);

            AdvanceToNextStep();
        }

        void HandleStepFailed(SurgicalStep step, string message)
        {
            
        }
        #endregion
        
        private void SetCurrentStep(SurgicalStep step)
        {
            CurrentStep = step;
            CurrentStep.EnableStep();
        }

        private void AdvanceToNextStep()
        {
            currentStepIndex++;

            if (currentStepIndex >= surgicalSteps.Length)
            {
                CurrentStep = null;
                Debug.Log("[Procedure] Procedure completed");
                OnProcedureCompleted?.Invoke();
                return;
            }

            SetCurrentStep(surgicalSteps[currentStepIndex]);
        }
        #endregion
    }
    
    
}

