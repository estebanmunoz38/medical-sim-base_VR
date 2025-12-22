using UnityEngine;

public class SetPosition : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float initialValue;
    [SerializeField] float finalValue;

    public void MoveToInitialPos()
    {
        Vector3 _position = target.position;
        _position.z = initialValue;
        transform.position = _position;
    }

    public void MoveToFixedPos()
    {
        Vector3 _position = transform.position;
        _position.z = finalValue;
        transform.position = _position;
    }
}
