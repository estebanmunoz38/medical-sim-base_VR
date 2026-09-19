using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Si se pierde el tracking de manos, suelta agarres fantasma y
/// prioriza continuidad (controladores como fallback).
/// </summary>
public class HandTrackingFallback : MonoBehaviour
{
    [Tooltip("Pérdidas cortas: no hace nada. La herramienta mantiene la última pose del SDK/XRI.")]
    [SerializeField] float lostGraceSeconds = 0.6f;
    [Tooltip("Tras la gracia, suelta agarres fantasma. Desactivar si GuidedMotion maneja el caso.")]
    [SerializeField] bool dropGrabsOnLostHands = true;
    [SerializeField] bool logWarnings = true;

    HandPinchToolInput _hands;
    float _lostTimer;
    bool _wasTracked;

    void Start()
    {
        _hands = FindFirstObjectByType<HandPinchToolInput>();
    }

    void Update()
    {
        if (_hands == null)
            _hands = FindFirstObjectByType<HandPinchToolInput>();

        bool tracked = _hands != null && _hands.AnyHandTracked;

        if (tracked)
        {
            _wasTracked = true;
            _lostTimer = 0f;
            return;
        }

        if (!_wasTracked)
            return;

        _lostTimer += Time.deltaTime;
        if (_lostTimer < lostGraceSeconds)
            return;

        // Tracking perdido tras haber tenido manos
        if (dropGrabsOnLostHands)
            ForceDropHeldTools();

        if (logWarnings)
            Debug.LogWarning("[Hands] Tracking perdido. Use controladores o reaparezca las manos. La sesión continúa.");

        _wasTracked = false;
        _lostTimer = 0f;
    }

    void ForceDropHeldTools()
    {
        var grabs = FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < grabs.Length; i++)
        {
            var g = grabs[i];
            if (g == null || !g.isSelected)
                continue;

            // Liberar todos los interactors que lo tienen seleccionado
            var selectors = new List<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor>(g.interactorsSelecting);
            for (int s = 0; s < selectors.Count; s++)
            {
                var interactor = selectors[s] as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;
                if (interactor != null)
                    g.interactionManager?.SelectExit(interactor, g);
            }
        }
    }
}
