using UnityEngine;

[CreateAssetMenu(fileName = "New Procedure Step", menuName = "Procedure/Step")]
public class ProcedureStepSO : ScriptableObject
{
    [Header("Identificacion")]
    public string stepID;
    public string title;

    [TextArea(3, 8)]
    public string instruction;

    [TextArea(2, 5)]
    public string inactivityReminder;

    [Header("Visuales")]
    public GameObject[] objectsToEnable;
    public GameObject[] objectsToDisable;

    [Header("Objetivo visual")]
    public GameObject validActionZone;
    public LineRenderer movementPath;

    [Header("Tutorial")]
    public bool waitForExternalComplete = true;
    public float inactivitySeconds = 12f;

    [Header("Creditos / Final")]
    public bool isCreditsStep;
}