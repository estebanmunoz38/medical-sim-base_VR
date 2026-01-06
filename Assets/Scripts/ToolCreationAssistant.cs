using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Asistente único para crear herramientas listas para VR:
/// - Instancia los prefabs en (0,0,0)
/// - Agrega XRGrabInteractable automáticamente
/// - Agrega el input adapter XRITToolInputSource
/// - Conecta el ActionBasedController derecho si existe en la escena
/// - Deja TODO listo para usar y mover
/// </summary>
public class ToolCreationAssistant : MonoBehaviour
{
    [Header("Arrastrar aquí los prefabs de tus herramientas")]
    public GameObject[] toolPrefabs;

    [Header("Referencia opcional al RightHand Controller (ActionBasedController)")]
    public ActionBasedController rightHandController;

    void Start()
    {
        if (toolPrefabs == null || toolPrefabs.Length == 0)
        {
            Debug.LogError("ToolCreationAssistant: No hay prefabs asignados.");
            return;
        }

        // Si no asignaste el RightHandController, intento encontrarlo en la escena
        if (rightHandController == null)
        {
            rightHandController = FindObjectOfType<ActionBasedController>();
            if (rightHandController == null)
                Debug.LogWarning("ToolCreationAssistant: No se encontró ActionBasedController automáticamente.");
        }

        SpawnAllTools();
    }

    void SpawnAllTools()
    {
        foreach (var prefab in toolPrefabs)
        {
            if (prefab == null) continue;

            GameObject tool = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            tool.name = prefab.name + "_VR";

            // Añadir XRGrabInteractable
            UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab = tool.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab == null)
                grab = tool.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

            grab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = false;

            // Intentar agregar el input VR
            XRITToolInputSource input = tool.GetComponent<XRITToolInputSource>();
            if (input == null)
                input = tool.AddComponent<XRITToolInputSource>();

            // Asignar ActionBasedController (mano derecha)
            if (rightHandController != null)
                input.controller = rightHandController;

            // Crear un pointer origin si la herramienta no lo tiene
            if (input.pointerOrigin == null)
            {
                GameObject origin = new GameObject("PointerOrigin");
                origin.transform.SetParent(tool.transform);
                origin.transform.localPosition = Vector3.zero;
                origin.transform.localRotation = Quaternion.identity;
                input.pointerOrigin = origin.transform;
            }

            Debug.Log("Tool creada y configurada: " + tool.name);
        }
    }
}
