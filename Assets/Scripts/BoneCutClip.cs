using UnityEngine;

public class BoneCutClip : MonoBehaviour
{
    [Header("Animator que contiene los AnimationClips")]
    [Tooltip("Animator con eventos de trigger para llamar diferentes clips")]
    [SerializeField] Animator cutAnimator;

    public void ChangeClip(string _keycode)
    {
        if (cutAnimator == null)
        {
            Debug.LogError($"[{name}] cutAnimator NO asignado");
            return;
        }

        Debug.Log($"[{name}] Lanzando trigger: {_keycode} en Animator: {cutAnimator.runtimeAnimatorController?.name}");
        cutAnimator.ResetTrigger("Abierto");
        cutAnimator.ResetTrigger("Cerrado");
        cutAnimator.SetTrigger(_keycode);

        Debug.Log($"[{name}] Estado actual layer 0: {cutAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash}");
    }
}