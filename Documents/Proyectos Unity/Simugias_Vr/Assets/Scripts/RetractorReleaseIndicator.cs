using UnityEngine;

/// <summary>
/// El cartel y el agarre externo de "liberar retractores" ya no se usan.
/// Si el componente sigue en escena, apaga su indicador y no crea mallas.
/// </summary>
public class RetractorReleaseIndicator : MonoBehaviour
{
    void Awake()
    {
        var visual = transform.Find("ReleaseGrip_Visual");
        if (visual != null)
            Destroy(visual.gameObject);

        var outlines = GetComponentsInChildren<Outline>(true);
        for (int i = 0; i < outlines.Length; i++)
        {
            if (outlines[i] != null)
                outlines[i].enabled = false;
        }

        enabled = false;
    }
}
