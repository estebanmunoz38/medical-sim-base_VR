using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
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
            xrInteractable.enabled = LeverRetractor == null;

        if (visualParent != null)
            visualParent.SetActive(false);

        if (attachPointChild != null)
            attachPointChild.SetActive(false);

        if (snapCheckDownChild != null)
            snapCheckDownChild.SetActive(false);

        if (GhostRetractor != null)
            GhostRetractor.SetActive(false);

        if (LeverRetractor != null)
        {
            LeverRetractor.SetActive(true);
            EnsureLeverGrabbable();
        }

        SetHomePlaced(true);
        SetOutlines(false);

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
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
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

        SetHomePlaced(false);
        SetOutlines(false);
    }

    void EnsureLeverGrabbable()
    {
        if (LeverRetractor == null)
            return;

        Collider col = LeverRetractor.GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider box = LeverRetractor.AddComponent<BoxCollider>();
            box.size = new Vector3(0.08f, 0.08f, 0.14f);
            col = box;
        }

        col.enabled = true;

        XRGrabInteractable leverGrab = LeverRetractor.GetComponent<XRGrabInteractable>();
        if (leverGrab == null)
            leverGrab = LeverRetractor.AddComponent<XRGrabInteractable>();

        Rigidbody leverBody = LeverRetractor.GetComponent<Rigidbody>();
        if (leverBody == null)
            leverBody = LeverRetractor.AddComponent<Rigidbody>();
        leverBody.isKinematic = true;
        leverBody.useGravity = false;

        leverGrab.enabled = true;
        leverGrab.throwOnDetach = false;
        leverGrab.forceGravityOnDetach = false;
        leverGrab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        leverGrab.selectExited.RemoveListener(OnLeverReleased);
        leverGrab.selectExited.AddListener(OnLeverReleased);

        var home = LeverRetractor.GetComponent<ToolHome>();
        if (home == null)
            home = LeverRetractor.AddComponent<ToolHome>();
        home.MarkPlaced();
    }

    void OnLeverReleased(SelectExitEventArgs args)
    {
        if (LeverRetractor != null && xrInteractable != null)
        {
            xrInteractable.transform.SetPositionAndRotation(
                LeverRetractor.transform.position,
                LeverRetractor.transform.rotation);
        }

        UnfreezeRetractor();
    }

    void SetOutlines(bool enabled)
    {
        Outline[] outlines = GetComponentsInChildren<Outline>(true);
        for (int i = 0; i < outlines.Length; i++)
            outlines[i].enabled = enabled;
    }

    void SetHomePlaced(bool placed)
    {
        ToolHome home = GetComponent<ToolHome>();
        if (home != null)
            home.SetPlaced(placed);
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