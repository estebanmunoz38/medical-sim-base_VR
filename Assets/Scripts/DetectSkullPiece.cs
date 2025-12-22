using UnityEngine;

public class DetectSkullPiece : MonoBehaviour
{
    public Encoscopy endoscopy;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Removable")
        { endoscopy.posStick.allowForwardMovement = false; }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.tag == "Removable")
        { endoscopy.posStick.allowForwardMovement = true; }
    }
}
