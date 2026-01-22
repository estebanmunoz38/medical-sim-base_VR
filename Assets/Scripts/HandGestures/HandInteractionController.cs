using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HandInteractionController : MonoBehaviour
{
    [Header("Interactors")]
    [SerializeField] private XRDirectInteractor _directInteractor;
    [SerializeField] private XRPokeInteractor _pokeInteractor;
    [SerializeField] private NearFarInteractor _nearFarInteractor;
    
    
    void Start()
    {
        _directInteractor.selectEntered.AddListener(OnGrabStart);
        _directInteractor.selectExited.AddListener(OnGrabEnd);
    }

    void OnDisable()
    {
        _directInteractor.selectEntered.RemoveListener(OnGrabStart);
        _directInteractor.selectExited.RemoveListener(OnGrabEnd);
    }
    

    #region Private Methods
    private void OnGrabStart(SelectEnterEventArgs arg0)
    {
        _pokeInteractor.gameObject.SetActive(false);
        _nearFarInteractor.gameObject.SetActive(false);
    }

    private void OnGrabEnd(SelectExitEventArgs arg0)
    {
        _pokeInteractor.gameObject.SetActive(true);
        _nearFarInteractor.gameObject.SetActive(true);
    }
    #endregion
}
