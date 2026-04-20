using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RetractorReleaseSelector : MonoBehaviour
{
    [Header("ROTACION A REVERTIR")]
    [SerializeField] private Transform rotationTarget;
    [SerializeField] private Vector3 initialLocalEuler;

    [Header("OBJETOS A ACTIVAR / DESACTIVAR")]
    [SerializeField] private GameObject leverObject;       // objeto anclado / visual colocado
    [SerializeField] private GameObject toolObject;        // herramienta real

    [Header("RETORNO DE HERRAMIENTA")]
    [SerializeField] private Transform returnPoint;
    [SerializeField] private XRGrabInteractable toolGrabInteractable;
    [SerializeField] private Rigidbody toolRb;

    [Header("OPCIONAL")]
    [SerializeField] private GameObject ghostObject;

    public void ReleaseRetractor()
    {
        Debug.Log("ReleaseRetractor() ejecutado");

        // 1) Revertir rotacion
        if (rotationTarget != null)
        {
            rotationTarget.localEulerAngles = initialLocalEuler;
            Debug.Log("Rotacion revertida");
        }
        else
        {
            Debug.LogWarning("rotationTarget NO asignado");
        }

        // 2) Apagar objeto anclado / lever
        if (leverObject != null)
        {
            leverObject.SetActive(false);
            Debug.Log("leverObject apagado");
        }
        else
        {
            Debug.LogWarning("leverObject NO asignado");
        }

        // 3) Reactivar herramienta real
        if (toolObject != null)
        {
            toolObject.SetActive(true);
            Debug.Log("toolObject reactivado");
        }
        else
        {
            Debug.LogWarning("toolObject NO asignado");
        }

        // 4) Devolver herramienta a la mano / punto de retorno
        if (toolObject != null && returnPoint != null)
        {
            toolObject.transform.position = returnPoint.position;
            toolObject.transform.rotation = returnPoint.rotation;
            Debug.Log("toolObject movido a returnPoint");
        }
        else
        {
            Debug.LogWarning("toolObject o returnPoint NO asignado");
        }

        // 5) Restaurar rigidbody
        if (toolRb != null)
        {
            toolRb.linearVelocity = Vector3.zero;
            toolRb.angularVelocity = Vector3.zero;
            toolRb.isKinematic = false;
            toolRb.useGravity = true;
            Debug.Log("Rigidbody restaurado");
        }
        else
        {
            Debug.LogWarning("toolRb NO asignado");
        }

        // 6) Restaurar grab
        if (toolGrabInteractable != null)
        {
            toolGrabInteractable.enabled = true;
            Debug.Log("XRGrabInteractable habilitado");
        }
        else
        {
            Debug.LogWarning("toolGrabInteractable NO asignado");
        }

        // 7) Ghost opcional
        if (ghostObject != null)
        {
            ghostObject.SetActive(true);
            Debug.Log("ghostObject activado");
        }
    }
}
