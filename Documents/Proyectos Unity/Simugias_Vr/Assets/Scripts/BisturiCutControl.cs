using UnityEngine;
using UnityEngine.InputSystem;

public class BisturiCutControl : MonoBehaviour
{
    [Header("Hoja del bisturi")]
    [Tooltip("Trigger, en la punta del bisturi hasta el mango")]
    [SerializeField] Collider bladeCollider;

    [Header("Verificacion de cortes")]
    [Tooltip("Triggers, verifican que el usuario cumpla el recorrido")]
    [SerializeField] GameObject initialCut;
    bool initialCutDone = false;
    [SerializeField] GameObject midCut;
    bool midCutDone = false;
    [SerializeField] GameObject finalCut;
    bool finalCutDone = false;

    public bool InitialCutDone => initialCutDone;
    public bool MidCutDone => midCutDone;
    public bool FinalCutDone => finalCutDone;
    public bool AllCutsDone => initialCutDone && midCutDone && finalCutDone;
    public Transform InitialCutPoint => initialCut != null ? initialCut.transform : null;
    public Transform MidCutPoint => midCut != null ? midCut.transform : null;
    public Transform FinalCutPoint => finalCut != null ? finalCut.transform : null;
    public Transform NextCutPoint
    {
        get
        {
            if (!initialCutDone && initialCut != null) return initialCut.transform;
            if (!midCutDone && midCut != null) return midCut.transform;
            if (!finalCutDone && finalCut != null) return finalCut.transform;
            return null;
        }
    }

    [Header("Control de las posiciones")]
    [Tooltip("Script, desde donde toma las posiciones de cada corte")]
    [SerializeField] BoneCutClip inferiorClips;
    [SerializeField] BoneCutClip superiorClips;

    [Header("Tutorial")]
    [SerializeField] bool showTutorial = true;

    SurgicalGuideBeacon _guideInitial;
    SurgicalGuideBeacon _guideMid;
    SurgicalGuideBeacon _guideFinal;

    void Start()
    {
        if (midCut != null) midCut.SetActive(false);
        if (finalCut != null) finalCut.SetActive(false);
        if (initialCut != null) initialCut.SetActive(true);

        if (showTutorial)
        {
            if (initialCut != null)
                _guideInitial = SurgicalGuideBeacon.Attach(initialCut.transform, "Incisión — punto 1", "Toque este hito con la hoja. La piel se abre tramo a tramo.", new Vector3(0f, 0.025f, 0f));
            if (midCut != null)
                _guideMid = SurgicalGuideBeacon.Attach(midCut.transform, "Incisión — punto 2", "Continúe la incisión hasta este punto.", new Vector3(0f, 0.025f, 0f));
            if (finalCut != null)
                _guideFinal = SurgicalGuideBeacon.Attach(finalCut.transform, "Incisión — punto 3", "Complete la incisión en este hito.", new Vector3(0f, 0.025f, 0f));
        }

        RefreshGuides();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == initialCut.name)
        {
            initialCutDone = true;
            initialCut.gameObject.SetActive(false);
            midCut.gameObject.SetActive(true);
            CheckAllCuts();
            ChangeAnimations("incision_1");
            //Debug.Log("Corte inicial hecho");
        }

        if (other.gameObject.name == midCut.name)
        {
            midCutDone = true;
            midCut.gameObject.SetActive(false);
            finalCut.gameObject.SetActive(true);
            CheckAllCuts();
            ChangeAnimations("incision_2");
            //Debug.Log("Corte medio hecho");
        }

        if (other.gameObject.name == finalCut.name)
        {
            finalCutDone = true;
            finalCut.gameObject.SetActive(false);
            CheckAllCuts();
            ChangeAnimations("incision_3");
            //Debug.Log("Corte final hecho");
        }
    }

    private void CheckAllCuts()
    {
        RefreshGuides();
        if(initialCutDone && midCutDone && finalCutDone)
        { CompleteCuts(); }
    }

    void RefreshGuides()
    {
        if (_guideInitial != null) _guideInitial.SetVisible(!initialCutDone);
        if (_guideMid != null) _guideMid.SetVisible(initialCutDone && !midCutDone);
        if (_guideFinal != null) _guideFinal.SetVisible(midCutDone && !finalCutDone);
    }

    private void ChangeAnimations(string _key)
    {
        superiorClips.ChangeClip(_key);
        inferiorClips.ChangeClip(_key);
    }
    
    public void CompleteCuts()
    {
        Debug.Log("TODOS LOS CORTES HECHO");
    }
}
