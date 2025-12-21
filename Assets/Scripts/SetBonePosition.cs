using UnityEngine;

public class SetBonePosition : MonoBehaviour
{
    [SerializeField] float initialValue;
    [SerializeField] float finalValue;
    [SerializeField] Vector3 initialRotation;

    void Start()
    { Init(); }

    private void Init()
    { initialRotation = transform.rotation.eulerAngles; }

    public void InitialRotationX()
    {
        Vector3 _rotacion = new Vector3(initialValue, 0, 0);   
        transform.eulerAngles = _rotacion;
    }

    public void FinalRotationX()
    {
        Vector3 _rotacion = new Vector3(finalValue, 0, 0) ;
        transform.eulerAngles = _rotacion;
    }
    public void InitialRotationZ()
    {
        Vector3 _rotacion = new Vector3(0, 0, initialValue);
        transform.eulerAngles = _rotacion;
    }

    public void FinalRotationZ()
    {
        Vector3 _rotacion = new Vector3(0, 0, finalValue);
        transform.eulerAngles = initialRotation + _rotacion;
    }
}