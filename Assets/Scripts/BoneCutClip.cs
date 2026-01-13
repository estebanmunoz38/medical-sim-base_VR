using UnityEngine;

public class BoneCutClip : MonoBehaviour
{
    [Header("Animator quecontiene los AnimationClips")]
    [Tooltip("Animator con eventos de trigger para llamar diferentes clips")]
    [SerializeField] Animator cutAnimator;

    public void ChangeClip(string _keycode)
    { cutAnimator.SetTrigger(_keycode); }
}
