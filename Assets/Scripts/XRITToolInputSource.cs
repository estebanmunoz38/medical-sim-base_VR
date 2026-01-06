using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class XRITToolInputSource : MonoBehaviour
{
    [Header("➡ Controlador VR (Action-Based Controller)")]
    public ActionBasedController controller;

    [Header("➡ Origen del trazo / dirección del marcador")]
    public Transform pointerOrigin;

    private void Awake()
    {
        // Si no existe pointerOrigin, lo creamos
        if (pointerOrigin == null)
        {
            GameObject origin = new GameObject("PointerOrigin");
            origin.transform.SetParent(transform);
            origin.transform.localPosition = Vector3.zero;
            origin.transform.localRotation = Quaternion.identity;
            pointerOrigin = origin.transform;
        }
    }

    /// ✅ BOTÓN PRINCIPAL PRESIONADO
    /// Usa el sistema REAL del XR Interaction Toolkit
    /// SIN depender del wizard
    /// SIN requerir otros scripts
    public bool IsPrimaryPressed()
    {
        if (controller == null)
            return false;

        float triggerValue = 0f;
        float gripValue = 0f;

        // Trigger (Activate)
        if (controller.activateAction.action != null)
            triggerValue = controller.activateAction.action.ReadValue<float>();

        // Grip (Select)
        if (controller.selectAction.action != null)
            gripValue = controller.selectAction.action.ReadValue<float>();

        // Si cualquiera supera 0.5, se considera PRESIONADO
        return triggerValue > 0.5f || gripValue > 0.5f;
    }

    /// ✅ Posición del origen
    public Vector3 GetPointerPosition()
    {
        return pointerOrigin != null ? pointerOrigin.position : transform.position;
    }

    /// ✅ Dirección del origen
    public Vector3 GetPointerForward()
    {
        return pointerOrigin != null ? pointerOrigin.forward : transform.forward;
    }
}
