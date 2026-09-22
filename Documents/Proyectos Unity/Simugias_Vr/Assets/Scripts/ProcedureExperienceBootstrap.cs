using UnityEngine;

/// <summary>
/// Puente runtime: tutorial nav + FX de tools + opacidad endoscópica + depósito.
/// Se agrega solo si no existe al cargar Simugias.
/// </summary>
[DefaultExecutionOrder(150)]
public class ProcedureExperienceBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!SimugiasRuntimeGate.AllowMedicalRuntime())
            return;
        if (FindFirstObjectByType<ProcedureExperienceBootstrap>() != null)
            return;

        // Solo en escena de simulador
        if (FindFirstObjectByType<Kerrison>() == null && FindFirstObjectByType<Endoscopio>() == null)
            return;

        var go = new GameObject("ProcedureExperience_Runtime");
        go.AddComponent<ProcedureExperienceBootstrap>();
    }

    void Awake()
    {
        if (FindFirstObjectByType<EndoscopicViewOpacityFix>() == null)
            gameObject.AddComponent<EndoscopicViewOpacityFix>();

        if (FindFirstObjectByType<FragmentDepositContainer>() == null)
            gameObject.AddComponent<FragmentDepositContainer>();

        EnsureDrillFx();
        EnsureSutureVisual();
        HideFloatingLiberarLabels();
        if (GetComponent<SurgicalSessionGuard>() == null)
            gameObject.AddComponent<SurgicalSessionGuard>();
    }

    void EnsureDrillFx()
    {
        var drills = FindObjectsByType<Drill>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < drills.Length; i++)
        {
            if (drills[i].GetComponent<DrillFxController>() == null)
                drills[i].gameObject.AddComponent<DrillFxController>();
        }
    }

    void EnsureSutureVisual()
    {
        var sutures = FindObjectsByType<FinSuturectomiaVR>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sutures.Length; i++)
        {
            if (sutures[i].GetComponent<SutureVisualUpgrade>() == null)
                sutures[i].gameObject.AddComponent<SutureVisualUpgrade>();
        }
    }

    void HideFloatingLiberarLabels()
    {
        var all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
                continue;
            string n = all[i].name;
            if (n.IndexOf("Liberar Retract", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Texto Liberar", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Retirador", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                all[i].gameObject.SetActive(false);
            }
        }
    }
}
