using UnityEngine;

public class SetBonePositionX : MonoBehaviour
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
        Vector3 _rotacion = new Vector3(finalValue, 0, 0);
        transform.eulerAngles = _rotacion;
    }

    // ESTO ANTES ERA Z — AHORA USA X
    public void InitialRotationAlt()
    {
        Vector3 _rotacion = new Vector3(initialValue, 0, 0);
        transform.eulerAngles = _rotacion;
    }

    public void FinalRotationAlt()
    {
        Vector3 _rotacion = new Vector3(finalValue, 0, 0);
        transform.eulerAngles = initialRotation + _rotacion;
    }
}