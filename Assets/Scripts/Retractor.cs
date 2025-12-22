using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Retractor : MonoBehaviour
{
    [Header("Player SETUP")]
    [SerializeField] private XRGrabInteractable xrInteractable;

    [Header("Checker SETUP")]
    [SerializeField]  Rigidbody rb;
    [SerializeField]  Collider checkUp;
    [SerializeField]  Collider checkDown;
    [SerializeField]  string nameUp;
    [SerializeField]  string nameDown;
    [SerializeField]  bool isFreeze;

    [Header("Ghost OBJ")]
    [SerializeField]  GameObject GhostRetractor;
    [SerializeField]  GameObject LeverRetractor;

    bool checkUpActive = false;
    bool checkDownActive = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == nameUp)
        { checkUpActive = true; }

        if (other.gameObject.name == nameDown)
        { checkDownActive = true; }

        UpdateStatus();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == nameUp)
        { checkUpActive = false; }

        if (other.gameObject.name == nameDown)
        { checkDownActive = false; }

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        bool _status = checkUpActive && checkDownActive;

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