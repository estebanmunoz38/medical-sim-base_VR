using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Drill : MonoBehaviour
{
    public GameObject holeObj;
    public Renderer targetRenderer;
    public Material baseMat;
    public Material[] interactableMat;
    [SerializeField] bool isDrilling;

    public void ResetColor()
    { targetRenderer.material = baseMat; }

    public void ChangeColor(int id)
    { targetRenderer.material = interactableMat[id]; }

    public void EnableDrill(bool _b)
    { isDrilling = _b; }

    private void OnTriggerStay(Collider other)
    {
        if (isDrilling)
        {
            if (other.name == ("low"))
            {
                ChangeColor(0);
                holeObj.SetActive(false);
            }

            if (other.name == ("ideal"))
            {
                ChangeColor(1);
                holeObj.SetActive(false);
            }

            if (other.name == ("high"))
            {
                ChangeColor(2);
                holeObj.SetActive(false);
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    { ResetColor(); }
}
