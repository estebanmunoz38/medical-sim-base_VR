using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit;
using System.IO;

public class ToolWizard : EditorWindow
{
    // Ahora GameObject -> así podés arrastrar desde la escena
    [SerializeField] private GameObject rightHandController;

    // Inicializado para evitar null
    [SerializeField] private GameObject[] models = new GameObject[0];

    [MenuItem("Tools/SimuGias/Build All Tools")]
    public static void ShowWindow()
    {
        GetWindow<ToolWizard>("SimuGias Tool Wizard");
    }

    private void OnGUI()
    {
        GUILayout.Label("SimuGias VR Tool Wizard", EditorStyles.boldLabel);

        // Seguridad extra por si Unity resetea cosas
        if (models == null)
            models = new GameObject[0];

        SerializedObject so = new SerializedObject(this);
        SerializedProperty m = so.FindProperty("models");
        SerializedProperty c = so.FindProperty("rightHandController");

        EditorGUILayout.PropertyField(m, true);
        EditorGUILayout.PropertyField(c);

        so.ApplyModifiedProperties();

        GUILayout.Space(10);

        if (GUILayout.Button("GENERAR TODAS LAS HERRAMIENTAS", GUILayout.Height(45)))
        {
            BuildTools();
        }
    }

    private void BuildTools()
    {
        if (models == null || models.Length == 0)
        {
            Debug.LogError("ToolWizard: No hay modelos asignados.");
            return;
        }

        string prefabFolder = "Assets/SimuGias/Tools/Prefabs";
        if (!Directory.Exists(prefabFolder))
            Directory.CreateDirectory(prefabFolder);

        ActionBasedController controllerComponent = null;

        if (rightHandController != null)
            controllerComponent = rightHandController.GetComponent<ActionBasedController>();

        foreach (GameObject model in models)
        {
            if (model == null) continue;

            // Crear instancia temporal del modelo
            GameObject temp = Instantiate(model);
            temp.name = model.name + "_VR";

            // Asegurar collider (si no tiene)
            if (!temp.GetComponent<Collider>())
                temp.AddComponent<BoxCollider>();

            // Agregar XRGrabInteractable
            UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab = temp.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grab == null) grab = temp.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

            grab.throwOnDetach = false;
            grab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking;

            // Agregar input VR
            XRITToolInputSource input = temp.GetComponent<XRITToolInputSource>();
            if (input == null) input = temp.AddComponent<XRITToolInputSource>();

            if (controllerComponent != null)
                input.controller = controllerComponent;

            // Crear pointer origin si no existe
            if (input.pointerOrigin == null)
            {
                GameObject origin = new GameObject("PointerOrigin");
                origin.transform.SetParent(temp.transform);
                origin.transform.localPosition = Vector3.zero;
                origin.transform.localRotation = Quaternion.identity;
                input.pointerOrigin = origin.transform;
            }

            // Guardar prefab
            string path = prefabFolder + "/" + temp.name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(temp, path);

            // Instanciar en la escena en (0,0,0)
            GameObject placed = Instantiate(temp, Vector3.zero, Quaternion.identity);
            placed.name = temp.name;

            DestroyImmediate(temp);
        }

        AssetDatabase.Refresh();
        Debug.Log("SimuGias Wizard: Herramientas creadas correctamente.");
    }
}
