using UnityEngine;

public class SetBonePosition : MonoBehaviour
{
    [SerializeField] private float initialValue;
    [SerializeField] private float finalValue;
    [SerializeField] private Vector3 initialRotation;

    void Start()
    {
        Init();
    }

    public void Init()
    {
        initialRotation = transform.rotation.eulerAngles;
    }

    public void InitialRotationX()
    {
        Vector3 rotacion = new Vector3(initialValue, 0f, 0f);
        transform.eulerAngles = rotacion;

        Debug.Log("[SetBonePosition] InitialRotationX -> " + rotacion);
    }

    public void FinalRotationX()
    {
        Vector3 rotacion = new Vector3(finalValue, 0f, 0f);
        transform.eulerAngles = rotacion;

        Debug.Log("[SetBonePosition] FinalRotationX -> " + rotacion);
    }

    public void InitialRotationZ()
    {
        Vector3 rotacion = initialRotation + new Vector3(0f, 0f, initialValue);
        transform.eulerAngles = rotacion;

        Debug.Log("[SetBonePosition] InitialRotationZ -> " + rotacion);
    }

    public void FinalRotationZ()
    {
        Vector3 rotacion = initialRotation + new Vector3(0f, 0f, finalValue);
        transform.eulerAngles = rotacion;

        Debug.Log("[SetBonePosition] FinalRotationZ -> " + rotacion);
    }

    // ESTA ES LA FUNCION QUE VAS A ASIGNAR DESDE EL EVENTO
public void BackToInitialValue()
{
    Vector3 r = transform.localEulerAngles;
    r.z = initialValue;   // -101
    transform.localEulerAngles = r;

    Debug.Log("[SetBonePosition] Z seteado a: " + initialValue);
}

}