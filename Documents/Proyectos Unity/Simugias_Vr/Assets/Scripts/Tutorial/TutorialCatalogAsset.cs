using UnityEngine;

[CreateAssetMenu(menuName = "Simugias/Tutorial Catalog", fileName = "TutorialCatalog")]
public class TutorialCatalogAsset : ScriptableObject
{
    public TutorialModuleAsset[] modules;
}
