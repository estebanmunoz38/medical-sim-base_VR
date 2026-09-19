using UnityEngine;

/// <summary>
/// Perfil reutilizable de tolerancias de movimiento para diseñadores.
/// Create → Simugias / Motion Tolerance Preset.
/// </summary>
[CreateAssetMenu(menuName = "Simugias/Motion Tolerance Preset", fileName = "MotionTolerance_")]
public class MotionToleranceAsset : ScriptableObject
{
    public MotionTolerancePreset values = MotionTolerancePreset.Beginner;

    [ContextMenu("Aplicar Principiante")]
    void ApplyBeginner() => values = MotionTolerancePreset.Beginner;

    [ContextMenu("Aplicar Estándar")]
    void ApplyStandard() => values = MotionTolerancePreset.Standard;

    [ContextMenu("Aplicar Experto")]
    void ApplyExpert() => values = MotionTolerancePreset.Expert;
}
