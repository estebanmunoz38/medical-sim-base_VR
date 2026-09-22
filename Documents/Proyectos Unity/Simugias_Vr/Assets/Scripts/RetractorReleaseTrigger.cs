using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RetractorReleaseTrigger : MonoBehaviour
{
    [Header("Retractor a liberar")]
    [SerializeField] private Retractor targetRetractor;

    public Retractor TargetRetractor
    {
        get => targetRetractor;
        set => targetRetractor = value;
    }

    [Header("XR Interactable")]
    [SerializeField] private XRBaseInteractable interactable;

    [Header("Collider del trigger")]
    [SerializeField] private Collider triggerCollider;

    [Header("Evento cuando se libera el retractor")]
    public UnityEvent OnRetractorReleased;

    private void Reset()
    {
        interactable = GetComponent<XRBaseInteractable>();
        triggerCollider = GetComponent<Collider>();
    }

    private void Awake()
    {
        if (interactable == null)
            interactable = GetComponent<XRBaseInteractable>();

        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();

        RetireManualButton();
    }

    /// <summary>
    /// El botón externo ya no libera la valva. La restauración de mallas
    /// se dispara cuando el usuario vuelve a tomar el retractor.
    /// </summary>
    public void InvokeReleaseConsequences()
    {
        OnRetractorReleased?.Invoke();
    }

    void RetireManualButton()
    {
        if (interactable != null)
            interactable.enabled = false;
        if (triggerCollider != null)
            triggerCollider.enabled = false;

        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = false;
        }

        var canvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null)
                canvases[i].gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        RetireManualButton();
    }
}