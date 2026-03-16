using UnityEngine;

public class SetPosition : MonoBehaviour
{
    [Header("Target (opcional)")]
    [SerializeField] Transform target;

    [Header("Valores iniciales (se guardan automáticamente al iniciar)")]
    [SerializeField] Vector3 local_position;
    [SerializeField] Quaternion local_rotation;

    [Header("Valores finales")]
    [SerializeField] Vector3 final_position;
    [SerializeField] Quaternion final_rotation;

    void Start()
    {
        Init();
    }

    private void Init()
    {
        GetPosition();
        GetRotation();
        Debug.Log("[SetPosition] Valores iniciales guardados");
    }

    private void GetPosition()
    {
        local_position = transform.localPosition;
    }

    private void GetRotation()
    {
        local_rotation = transform.localRotation;
    }

    // --- POSICIONES ---

    public void MoveToInitialPos()
    {
        transform.localPosition = local_position;
        Debug.Log("[SetPosition] Posición inicial restaurada");
    }

    public void MoveToFixedPos()
    {
        transform.localPosition = final_position;
        Debug.Log("[SetPosition] Posición final aplicada");
    }

    public void ChangePosition()
    {
        transform.localPosition = final_position;
        Debug.Log("[SetPosition] ChangePosition ejecutado");
    }

    // --- ROTACIONES ---

    public void ChangeRotation()
    {
        transform.localRotation = final_rotation;
        Debug.Log("[SetPosition] Rotación final aplicada");
    }


    public void ResetRotation()
    {
    transform.localPosition = local_position;
    transform.localRotation = local_rotation;
    Debug.Log("[SetPosition] Posición y rotación inicial restauradas");
}

}