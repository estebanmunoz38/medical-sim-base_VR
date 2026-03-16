using UnityEngine;

public class SetBonePositionX : MonoBehaviour
{
    [SerializeField] float initialValue;
    [SerializeField] float finalValue;
    [SerializeField] Vector3 initialRotation;

    void Start()
    {
        Init();
    }

    private void Init()
    {
        initialRotation = transform.localEulerAngles;
    }

    public void InitialRotationX()
    {
        Vector3 _rotacion = initialRotation;
        _rotacion.x = initialValue;
        transform.localEulerAngles = _rotacion;

        Debug.Log("[SetBonePositionX] InitialRotationX -> " + _rotacion);
    }

    public void FinalRotationX()
    {
        Vector3 _rotacion = initialRotation;
        _rotacion.x = finalValue;
        transform.localEulerAngles = _rotacion;

        Debug.Log("[SetBonePositionX] FinalRotationX -> " + _rotacion);
    }

    public void InitialRotationAlt()
    {
        InitialRotationX();
    }

    public void FinalRotationAlt()
    {
        FinalRotationX();
    }
}