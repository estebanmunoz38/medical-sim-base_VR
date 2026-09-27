using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HandTrackingSceneSetup
{
    const string SourceScene = "Assets/Scenes/Simugias.unity";
    const string HandScene = "Assets/Scenes/Simugias_HandTracking.unity";

    [MenuItem("Simugias/Hand Tracking/Prepare Hand Tracking Scene")]
    public static void Prepare()
    {
        var source = AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScene);
        if (source == null)
        {
            Debug.LogError($"[HandTracking] No existe {SourceScene}.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HandScene) == null)
        {
            if (!AssetDatabase.CopyAsset(SourceScene, HandScene))
            {
                Debug.LogError($"[HandTracking] No se pudo copiar {SourceScene}.");
                return;
            }
        }

        var scene = EditorSceneManager.OpenScene(HandScene, OpenSceneMode.Single);
        if (Object.FindAnyObjectByType<HandTrackingBootstrap>(FindObjectsInactive.Include) == null)
        {
            var root = new GameObject("HandTracking");
            root.AddComponent<HandTrackingBootstrap>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[HandTracking] Lista {HandScene}. Simugias.unity no se modificó. El objeto HandTracking solo está en la copia.");
    }
}
