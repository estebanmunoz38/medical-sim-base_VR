using UnityEngine;

[CreateAssetMenu(fileName = "New Procedure Timeline", menuName = "Procedure/Timeline")]
public class ProcedureTimelineSO : ScriptableObject
{
    public string procedureName;
    public ProcedureStepSO[] steps;
}