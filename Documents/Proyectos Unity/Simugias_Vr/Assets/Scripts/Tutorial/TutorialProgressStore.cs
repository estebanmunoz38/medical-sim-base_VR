using UnityEngine;

/// <summary>
/// Persistencia ligera del tutorial. Si más adelante hay un save del simulador,
/// este almacén puede sustituirse sin tocar el director.
/// </summary>
public static class TutorialProgressStore
{
    const string Prefix = "Simugias.Tutorial.v2.";

    public static bool Started
    {
        get => PlayerPrefs.GetInt(Prefix + "Started", 0) == 1;
        set => SetInt("Started", value ? 1 : 0);
    }

    public static bool Finished
    {
        get => PlayerPrefs.GetInt(Prefix + "Finished", 0) == 1;
        set => SetInt("Finished", value ? 1 : 0);
    }

    public static string LastStepId
    {
        get => PlayerPrefs.GetString(Prefix + "LastStep", string.Empty);
        set
        {
            PlayerPrefs.SetString(Prefix + "LastStep", value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    public static string LastModuleId
    {
        get => PlayerPrefs.GetString(Prefix + "LastModule", string.Empty);
        set
        {
            PlayerPrefs.SetString(Prefix + "LastModule", value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    public static bool IsModuleComplete(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId)) return false;
        return PlayerPrefs.GetInt(Prefix + "Mod." + moduleId, 0) == 1;
    }

    public static void MarkModuleComplete(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId)) return;
        SetInt("Mod." + moduleId, 1);
    }

    public static bool OnboardingComplete
    {
        get => PlayerPrefs.GetInt(Prefix + "Onboarding", 0) == 1;
        set => SetInt("Onboarding", value ? 1 : 0);
    }

    /// <summary>0 = desconocida, 1 = derecha, 2 = izquierda.</summary>
    public static int DominantHand
    {
        get => PlayerPrefs.GetInt(Prefix + "DominantHand", 0);
        set => SetInt("DominantHand", Mathf.Clamp(value, 0, 2));
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Prefix + "Started");
        PlayerPrefs.DeleteKey(Prefix + "Finished");
        PlayerPrefs.DeleteKey(Prefix + "LastStep");
        PlayerPrefs.DeleteKey(Prefix + "LastModule");
        PlayerPrefs.DeleteKey(Prefix + "Onboarding");
        PlayerPrefs.DeleteKey(Prefix + "DominantHand");
        PlayerPrefs.Save();
    }

    static void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(Prefix + key, value);
        PlayerPrefs.Save();
    }
}
