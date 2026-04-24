using UnityEngine;

public class ProcedureActionRelay : MonoBehaviour
{
    public void CompleteStep(string stepID)
    {
        if (ProcedureManager.Instance != null)
            ProcedureManager.Instance.CompleteStep(stepID);
    }

    public void NotifyAction()
    {
        if (ProcedureManager.Instance != null)
            ProcedureManager.Instance.NotifyUserAction();
    }
}