using UnityEngine;

public class EndoscopioHandler : MonoBehaviour
{
    [SerializeField] private GameObject handlerManipulator;
    [SerializeField] private GameObject ghostHelper;
    [SerializeField] private GameObject screenUI;
    void OnTriggerEnter(Collider other)
    {
        if(other.name == "Ghost Endoscopio")
        {
            this.gameObject.SetActive(false);
            other.gameObject.SetActive(false);
            handlerManipulator.SetActive(true);
            screenUI.gameObject.SetActive(true);
        }
    }

    public void ResetPosition()
    {
        Vector3 _resetPosition = ghostHelper.transform.position;
        Quaternion _resetRotation = ghostHelper.transform.rotation;
        handlerManipulator.transform.position = _resetPosition;
        handlerManipulator.transform.rotation = _resetRotation;
    }
}
