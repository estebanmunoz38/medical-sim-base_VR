using UnityEngine;

[CreateAssetMenu(menuName = "Simugias/Tutorial Module", fileName = "TutorialModule")]
public class TutorialModuleAsset : ScriptableObject
{
    public string id;
    public string title;
    [TextArea(1, 3)] public string description;
    public TutorialStepConfig[] steps;

    public TutorialModuleConfig ToConfig()
    {
        return new TutorialModuleConfig
        {
            id = string.IsNullOrEmpty(id) ? name : id,
            title = title,
            description = description,
            steps = steps
        };
    }
}
