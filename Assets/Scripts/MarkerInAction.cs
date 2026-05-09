using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class MarkerInAction : MonoBehaviour
{
    public List<GameObject> objectsToHide;
    public List<GameObject> objectsToShow;

    public bool ResetSelectExited=true;
    private XRGrabInteractable grab;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(_ => SetInAction(true));

        if(ResetSelectExited)
        grab.selectExited.AddListener(_ => SetInAction(false));
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveAllListeners();
        grab.selectExited.RemoveAllListeners();
    }

    public void SetInAction(bool isInAction)
    {
        foreach (var obj in objectsToHide)
            if (obj) obj.SetActive(!isInAction);

        foreach (var obj in objectsToShow)
            if (obj) obj.SetActive(isInAction);
    }
}