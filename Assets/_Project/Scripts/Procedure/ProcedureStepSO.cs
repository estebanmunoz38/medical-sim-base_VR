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

    [Header("Visuales (usar IDs, NO objetos)")]
    public string[] objectsToEnableIDs;
    public string[] objectsToDisableIDs;

    [Header("Objetivo visual")]
    public string validActionZoneID;
    public string movementPathID;

    [Header("Tutorial")]
    public bool waitForExternalComplete = true;
    public float inactivitySeconds = 12f;

    [Header("Creditos")]
    public bool isCreditsStep;
}