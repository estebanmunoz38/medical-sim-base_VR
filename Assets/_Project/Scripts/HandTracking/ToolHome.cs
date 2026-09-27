using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ToolHome : MonoBehaviour
{
    Vector3 homePosition;
    Quaternion homeRotation;
    XRGrabInteractable grab;
    bool captured;
    bool placed;

    public void Capture()
    {
        homePosition = transform.position;
        homeRotation = transform.rotation;
        captured = true;
    }

    public void SetPlaced(bool value)
    {
        placed = value;
    }

    public void MarkPlaced()
    {
        placed = true;
        Capture();
    }

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    void Start()
    {
        if (!captured)
            Capture();
    }

    void LateUpdate()
    {
        if (!captured || placed)
            return;

        if (grab != null && (grab.isSelected || !grab.isActiveAndEnabled))
            return;

        bool fell = transform.position.y < homePosition.y - 0.35f || transform.position.y < 0.05f;
        bool leftArea = Vector3.Distance(transform.position, homePosition) > 2.5f;
        if (!fell && !leftArea)
            return;

        transform.SetPositionAndRotation(homePosition, homeRotation);
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
