using UnityEngine;

public class SetPosition : MonoBehaviour
{
    [SerializeField] Transform target;
    //[SerializeField] float initialValue;
    //[SerializeField] float finalValue;

    [SerializeField] Vector3 local_position;
    [SerializeField] Quaternion local_rotation;

    [SerializeField] Vector3 final_position;
    [SerializeField] Quaternion final_rotation;

    void Start()
    { Init(); }

    private void Init()
    {
        GetPosition();
        GetRotation();
    }

    public void MoveToInitialPos()
    {
        Vector3 _position = target.position;
        //_position.z = initialValue;
        //transform.position = _position;
    }

    public void MoveToFixedPos()
    {
        Vector3 _position = transform.position;
        //_position.z = finalValue;
        //transform.position = _position;
    }

    private void GetPosition()
    { local_position = this.transform.localPosition; }

    private void GetRotation()
    { local_rotation = this.transform.rotation; }

    public void ChangePosition()
    { this.transform.position = final_position; }

    public void ChangeRotation()
    { this.transform.rotation = final_rotation; }
}
