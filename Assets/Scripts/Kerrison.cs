using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.XR.Haptics;

public class Kerrison : MonoBehaviour
{
    [Header("Variables requeridas")]
    [Tooltip("Posicion de obj padre, bool de deteccion activa, bool si tiene una pieza agarrada, Controlador de Joystick")]
    [SerializeField] Transform targetPosition;
    [SerializeField] bool isDetecting;
    [SerializeField] string targetTag;
    [SerializeField] bool hasOne;

    InputDevice rightHand;
    InputDevice leftHand;

    Transform heldObj;
    SkullPieces skullPiece;

    void Start()
    { Init(); }

    void Init()
    {
        rightHand = InputSystem.GetDevice<XRController>(CommonUsages.RightHand);
        leftHand = InputSystem.GetDevice<XRController>(CommonUsages.LeftHand);
    }

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
                skullPiece = heldObj.GetComponent<SkullPieces>();
                skullPiece.SetOutlineColor(Color.green);
                isDetecting = false;
                hasOne = true;
            }
        }

        if(other.gameObject.name == "ClearCol" && hasOne)
        {
            DropPiece();
            if (ProcedureManager.Instance != null)
    ProcedureManager.Instance.CompleteStep("remove_piece");
        }
    }
public void NotifyTakeKerrison()
{
    if (ProcedureManager.Instance != null)
        ProcedureManager.Instance.CompleteStep("take_kerrison");
}

}