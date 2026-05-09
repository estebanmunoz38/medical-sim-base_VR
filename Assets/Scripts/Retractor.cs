using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.Events;


public class Retractor : MonoBehaviour
{
    [Header("Player SETUP")]
    [SerializeField] private XRGrabInteractable xrInteractable;

    [Header("Checker SETUP")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private string colKeyName;
    [SerializeField] private bool isFreeze;
    [Header("Procedure Manager")]
    [SerializeField] private string stepIDToComplete;
    [SerializeField] private bool completeProcedureStepOnFreeze = true;

    [Header("Tutorial Events")]
    public UnityEvent onRetractorPlaced;

    [Header("Ghost OBJ")]
    [SerializeField] private GameObject GhostRetractor;
    [SerializeField] private GameObject LeverRetractor;

    [Header("Visual Objects")]
    [SerializeField] private GameObject visualParent;
    [SerializeField] private GameObject attachPointChild;
    [SerializeField] private GameObject snapCheckDownChild;

    [Header("Level Transform")]
    [SerializeField] private Transform levelTransform;

    [Header("Snap Settings")]
    [SerializeField] private float snapReactivateDelay = 2f;

    private bool checkUpActive = false;

    private Vector3 levelInitialPos;
    private Quaternion levelInitialRot;

    private void Awake()
    {
        if (levelTransform != null)
        {
            levelInitialPos = levelTransform.localPosition;
            levelInitialRot = levelTransform.localRotation;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isFreeze) return;

        if (other.gameObject.name == colKeyName)
        {
            checkUpActive = true;
            UpdateStatus();
        }
    }

    private void UpdateStatus()
    {
        bool status = checkUpActive;

        if (status != isFreeze)
        {
            isFreeze = status;

            if (isFreeze)
            {
                FreezeRetractor();
            }
        }
    }

    private void FreezeRetractor()
    {
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (xrInteractable != null)
            xrInteractable.enabled = false;

        if (visualParent != null)
            visualParent.SetActive(false);

        if (attachPointChild != null)
            attachPointChild.SetActive(false);

        if (snapCheckDownChild != null)
            snapCheckDownChild.SetActive(false);

        if (GhostRetractor != null)
            GhostRetractor.SetActive(false);

        if (LeverRetractor != null)
            LeverRetractor.SetActive(true);

            onRetractorPlaced?.Invoke();

if (completeProcedureStepOnFreeze && ProcedureManager.Instance != null && !string.IsNullOrEmpty(stepIDToComplete))
{
    ProcedureManager.Instance.CompleteStep(stepIDToComplete);
}


    }

    public void UnfreezeRetractor()
    {
        if (!isFreeze) return;

        isFreeze = false;
        checkUpActive = false;

        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        if (xrInteractable != null)
            xrInteractable.enabled = true;

        if (visualParent != null)
            visualParent.SetActive(true);

        // Desactivar snap temporalmente
        if (attachPointChild != null)
        attachPointChild.SetActive(false);

        if (snapCheckDownChild != null)
         snapCheckDownChild.SetActive(false);

        // Reactivar luego de un tiempo
        StartCoroutine(ReenableSnapAfterDelay());

        if (GhostRetractor != null)
            GhostRetractor.SetActive(true);

        if (LeverRetractor != null)
            LeverRetractor.SetActive(false);

        if (levelTransform != null)
        {
            levelTransform.localPosition = levelInitialPos;
            levelTransform.localRotation = levelInitialRot;
        }
    }
        private IEnumerator ReenableSnapAfterDelay()
    {
    yield return new WaitForSeconds(snapReactivateDelay);

    if (attachPointChild != null)
        attachPointChild.SetActive(true);

    if (snapCheckDownChild != null)
        snapCheckDownChild.SetActive(true);

    Debug.Log("Snap reactivado luego del delay");
}
}