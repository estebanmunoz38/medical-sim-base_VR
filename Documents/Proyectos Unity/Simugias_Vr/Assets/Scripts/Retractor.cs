using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Retractor : MonoBehaviour
{
    [Header("Player SETUP")]
    [SerializeField] private XRGrabInteractable xrInteractable;

    [Header("Checker SETUP")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private string colKeyName;
    [SerializeField] private bool isFreeze;

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
    SurgicalGuideBeacon _gripGuide;
    XRGrabInteractable _leverGrab;
    bool _leverHooked;
    bool _transferring;

    /// <summary>Valva anclada en el punto de sujeción.</summary>
    public bool IsAttached => isFreeze;
    /// <summary>Se ancló al menos una vez en esta sesión.</summary>
    public bool WasEverAttached { get; private set; }
    public Transform SnapPoint =>
        snapCheckDownChild != null ? snapCheckDownChild.transform
        : attachPointChild != null ? attachPointChild.transform
        : transform;

    private void Awake()
    {
        if (levelTransform != null)
        {
            levelInitialPos = levelTransform.localPosition;
            levelInitialRot = levelTransform.localRotation;
        }

        Transform grip = attachPointChild != null ? attachPointChild.transform
            : GhostRetractor != null ? GhostRetractor.transform
            : transform;
        _gripGuide = SurgicalGuideBeacon.Attach(
            grip,
            "Retractor — sujeción",
            "Ancle la valva en este punto. Al abrir, debe verse la piel del campo.",
            new Vector3(0f, 0.03f, 0f));
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
                WasEverAttached = true;
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

        if (visualParent != null && visualParent != gameObject)
            visualParent.SetActive(false);
        else
            SetOwnRenderers(false);

        if (attachPointChild != null)
            attachPointChild.SetActive(false);

        if (snapCheckDownChild != null)
            snapCheckDownChild.SetActive(false);

        if (GhostRetractor != null)
            GhostRetractor.SetActive(false);

        if (LeverRetractor != null)
            LeverRetractor.SetActive(true);

        if (_gripGuide != null)
            _gripGuide.SetVisible(false);

        ClearStuckOutlines();
        HookLeverGrab();
    }

    public void UnfreezeRetractor()
    {
        if (!isFreeze) return;

        isFreeze = false;
        checkUpActive = false;
        ClearStuckOutlines();

        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        if (xrInteractable != null)
            xrInteractable.enabled = true;

        if (visualParent != null && visualParent != gameObject)
            visualParent.SetActive(true);
        else
            SetOwnRenderers(true);

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

        if (_gripGuide != null)
            _gripGuide.SetVisible(true);
    }

    void HookLeverGrab()
    {
        if (LeverRetractor == null || _leverHooked)
            return;

        _leverGrab = LeverRetractor.GetComponent<XRGrabInteractable>();
        if (_leverGrab == null)
            _leverGrab = LeverRetractor.AddComponent<XRGrabInteractable>();

        var body = LeverRetractor.GetComponent<Rigidbody>();
        if (body == null)
            body = LeverRetractor.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        if (LeverRetractor.GetComponentInChildren<Collider>(true) == null)
        {
            var box = LeverRetractor.AddComponent<BoxCollider>();
            box.size = new Vector3(0.06f, 0.04f, 0.12f);
        }

        _leverGrab.throwOnDetach = false;
        _leverGrab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        _leverGrab.trackPosition = true;
        _leverGrab.trackRotation = true;
        _leverGrab.smoothPosition = false;
        _leverGrab.smoothRotation = false;
        _leverGrab.selectEntered.AddListener(OnLeverSelected);
        _leverHooked = true;
    }

    void OnLeverSelected(SelectEnterEventArgs args)
    {
        if (!isFreeze || args == null || _transferring)
            return;

        _transferring = true;
        StartCoroutine(TransferGrabFromLever(args.interactorObject, args.manager));
    }

    IEnumerator TransferGrabFromLever(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor, UnityEngine.XR.Interaction.Toolkit.XRInteractionManager manager)
    {
        Vector3 posePos = LeverRetractor != null ? LeverRetractor.transform.position : transform.position;
        Quaternion poseRot = LeverRetractor != null ? LeverRetractor.transform.rotation : transform.rotation;
        yield return null;

        InvokeLinkedReleaseEvents();
        UnfreezeRetractor();
        transform.SetPositionAndRotation(posePos, poseRot);
        yield return null;

        if (manager != null && xrInteractable != null && interactor != null)
            manager.SelectEnter(interactor, xrInteractable);

        _transferring = false;
    }

    void InvokeLinkedReleaseEvents()
    {
        var triggers = FindObjectsByType<RetractorReleaseTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < triggers.Length; i++)
        {
            if (triggers[i] != null && triggers[i].TargetRetractor == this)
                triggers[i].InvokeReleaseConsequences();
        }
    }

    void SetOwnRenderers(bool enabledRenderer)
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            if (LeverRetractor != null && renderers[i].transform.IsChildOf(LeverRetractor.transform))
                continue;
            renderers[i].enabled = enabledRenderer;
        }
    }

    void ClearStuckOutlines()
    {
        if (LeverRetractor == null)
            return;

        var outlines = LeverRetractor.GetComponentsInChildren<Outline>(true);
        for (int i = 0; i < outlines.Length; i++)
        {
            if (outlines[i] != null)
                outlines[i].enabled = false;
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