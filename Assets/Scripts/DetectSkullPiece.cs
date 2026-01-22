using UnityEngine;

public class DetectSkullPiece : MonoBehaviour
{
    public Endoscopio endoscopio;

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Removable"))
        {
            endoscopio.MovementStop();
            other.gameObject.GetComponent<Outline>().enabled = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Removable"))
        {
            other.gameObject.GetComponent<Outline>().enabled = false;
        }
    }
}
