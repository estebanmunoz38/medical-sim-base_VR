using UnityEngine;

public class SetBonePosition : MonoBehaviour
{
    [Header("Valores de rotación")]
    [SerializeField] private float initialValue;
    [SerializeField] private float finalValue;
    [SerializeField] private Vector3 initialRotation;

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        initialRotation = transform.localEulerAngles;
    }

    // =========================
    // ROTACION EN Z
    // =========================

    public void FinalRotationZ()
    {
        Vector3 rotacion = transform.localEulerAngles;
        rotacion.z = finalValue;
        transform.localEulerAngles = rotacion;

        Debug.Log("[SetBonePosition] FinalRotationZ -> " + finalValue);
    }

    public void InitialRotationZ()
    {
        Vector3 rotacion = transform.localEulerAngles;
        rotacion.z = initialValue;
        transform.localEulerAngles = rotacion;

        Debug.Log("[SetBonePosition] InitialRotationZ -> " + initialValue);
    }

    public void BackToInitialValue()
    {
        Vector3 rotacion = transform.localEulerAngles;
        rotacion.z = initialValue;
        transform.localEulerAngles = rotacion;

        Debug.Log("[SetBonePosition] Volviendo a Initial Value Z: " + initialValue);
    }

    // =========================
    // ROTACION EN X
    // =========================

    public void FinalRotationX()
    {
        Vector3 rotacion = transform.localEulerAngles;
        rotacion.x = finalValue;
        transform.localEulerAngles = rotacion;

        Debug.Log("[SetBonePosition] FinalRotationX -> " + finalValue);
    }

    public void InitialRotationX()
    {
        Vector3 rotacion = transform.localEulerAngles;
        rotacion.x = initialValue;
        transform.localEulerAngles = rotacion;

        Debug.Log("[SetBonePosition] InitialRotationX -> " + initialValue);
    }

    public void BackToInitialValueX()
    {
        Vector3 rotacion = transform.localEulerAngles;
        rotacion.x = initialValue;
        transform.localEulerAngles = rotacion;

        Debug.Log("[SetBonePosition] Volviendo a Initial Value X: " + initialValue);
    }
}