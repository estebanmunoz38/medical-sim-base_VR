using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RetractorReleaseTrigger : MonoBehaviour
{
    [Header("Retractor a liberar")]
    [SerializeField] private Retractor targetRetractor;

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
    }

    private void OnEnable()
    {
        StartCoroutine(RefreshInteractableNextFrame());
    }

    private IEnumerator RefreshInteractableNextFrame()
    {
        yield return null;

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
            triggerCollider.enabled = true;
        }

        if (interactable != null)
        {
            interactable.enabled = false;
            interactable.enabled = true;

            interactable.selectEntered.RemoveListener(OnSelected);
            interactable.selectEntered.AddListener(OnSelected);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnSelected);
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        Debug.Log("TRIGGER DE LIBERACION ACTIVADO");

        if (targetRetractor != null)
        {
            targetRetractor.UnfreezeRetractor();
        }

        // Dispara el evento configurable desde inspector
        OnRetractorReleased?.Invoke();
    }
}