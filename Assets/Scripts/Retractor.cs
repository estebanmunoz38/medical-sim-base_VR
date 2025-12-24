using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Retractor : MonoBehaviour
{
    [Header("Player SETUP")]
    [SerializeField] private XRGrabInteractable xrInteractable;

    [Header("Checker SETUP")]
    [SerializeField]  Rigidbody rb;
    [SerializeField]  Collider colCheck;
    [SerializeField]  string colKeyName;
    [SerializeField]  bool isFreeze;

    [Header("Ghost OBJ")]
    [SerializeField]  GameObject GhostRetractor;
    [SerializeField]  GameObject LeverRetractor;

    bool checkUpActive = false;
    bool checkDownActive = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == colKeyName)
        { checkUpActive = true; }

        UpdateStatus();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == colKeyName)
        { checkUpActive = false; }

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        bool _status = checkUpActive;

        if (_status != isFreeze)
        {
            isFreeze = _status;

            if (isFreeze)
            {
                rb.useGravity = false;
                rb.isKinematic = true;
                xrInteractable.enabled = false;
                this.gameObject.SetActive(false);
                GhostRetractor.SetActive(false);
                LeverRetractor.SetActive(true);
            }
        }
    }
}