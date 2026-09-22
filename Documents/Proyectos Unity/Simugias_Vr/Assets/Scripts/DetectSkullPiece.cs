using UnityEngine;

public class DetectSkullPiece : MonoBehaviour
{
    public Endoscopio endoscopio;
    Outline _lit;

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Removable"))
            return;

        if (endoscopio != null)
            endoscopio.MovementStop();

        var outline = other.GetComponent<Outline>();
        if (outline == null)
            return;

        if (_lit != null && _lit != outline)
            _lit.enabled = false;

        _lit = outline;
        outline.enabled = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Removable"))
            return;

        var outline = other.GetComponent<Outline>();
        if (outline != null)
            outline.enabled = false;
        if (_lit == outline)
            _lit = null;
    }

    void OnDisable()
    {
        if (_lit != null)
            _lit.enabled = false;
        _lit = null;
    }
}
