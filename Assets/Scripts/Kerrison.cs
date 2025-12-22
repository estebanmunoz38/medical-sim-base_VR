using UnityEngine;

public class Kerrison : MonoBehaviour
{
    public UICount counter;
    [SerializeField] Transform targetPosition;
    [SerializeField] bool isDetecting;
    [SerializeField] string targetTag;
    [SerializeField] bool hasOne;

    private Transform heldObj;

    public void DetectionActive()
    {
        if(!hasOne)
        { isDetecting = true; }
    }

    public void DetectionDisabled()
    { isDetecting = false; }

    public void DropPiece()
    {
        if(hasOne)
        {
            heldObj.SetParent(null);
            heldObj.GetComponent<Rigidbody>().isKinematic = false;
            heldObj.GetComponent<Rigidbody>().useGravity = true;
            hasOne = false;
            heldObj = null;
            counter.Sum();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDetecting && !hasOne)
        {
            if(other.CompareTag(targetTag))
            {
                heldObj = other.transform;
                heldObj.SetParent(targetPosition);
                heldObj.localPosition = Vector3.zero;
                isDetecting = false;
                hasOne = true;
            }
        }

        if(other.gameObject.name == "ClearCol" && hasOne)
        {
            DropPiece();
            //piecesList.UpdateList(other.gameObject.name);
        }
    }
}