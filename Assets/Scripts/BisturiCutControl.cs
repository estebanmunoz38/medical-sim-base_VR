using UnityEngine;

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

    [Header("Control de las posiciones")]
    [Tooltip("Script, desde donde toma las posiciones de cada corte")]
    public BoneCutPosition boneSuperior;
    public BoneCutPosition boneInferior;

    void Start()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == initialCut.name)
        {
            initialCutDone = true;
            initialCut.gameObject.SetActive(false);
            midCut.gameObject.SetActive(true);
            CheckAllCuts();
            boneSuperior.FirstCut();
            boneInferior.FirstCut();
            Debug.Log("Corte inicial hecho");
        }

        if (other.gameObject.name == midCut.name)
        {
            midCutDone = true;
            midCut.gameObject.SetActive(false);
            finalCut.gameObject.SetActive(true);
            CheckAllCuts();
            boneSuperior.SecondCut();
            boneInferior.SecondCut();
            Debug.Log("Corte medio hecho");
        }

        if (other.gameObject.name == finalCut.name)
        {
            finalCutDone = true;
            finalCut.gameObject.SetActive(false);
            CheckAllCuts();
            boneSuperior.FinalCut();
            boneInferior.FinalCut();
            Debug.Log("Corte final hecho");
        }
    }

    private void CheckAllCuts()
    {
        if(initialCutDone && midCutDone && finalCutDone)
        { CompleteCuts(); }
    }
    
    public void CompleteCuts()
    {
        Debug.Log("TODOS LOS CORTES HECHO");
    }
}
