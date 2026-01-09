using UnityEngine;

public class BoneCutPosition : MonoBehaviour
{
    [Header("Ejes para tomar la posicion de cada corte")]
    [Tooltip("Vector3 para convertir en transform.position")]
    [SerializeField] Vector3 initialPosition;
    [SerializeField] Vector3 firstCutPosition;
    [SerializeField] Vector3 secondCutPosition;
    [SerializeField] Vector3 thirdCutPosition;

    void Start()
    { Init(); }

    public void Init()
    {
        initialPosition = transform.position;
        SetPosition(initialPosition);
    }

    void SetPosition(Vector3 _position)
    { transform.position = _position; }

    public void FirstCut()
    { SetPosition(firstCutPosition); }

    public void SecondCut()
    { SetPosition(secondCutPosition); }

    public void FinalCut()
    { SetPosition(thirdCutPosition); }
}
