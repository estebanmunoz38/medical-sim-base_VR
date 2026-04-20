using UnityEngine;
using Dreamteck.Splines;

public class Endoscopio : MonoBehaviour
{
    [Header("Camara de endoscopio")]
    [SerializeField] SplineFollower cameraFollower;
    [SerializeField] float speedMovement = 0.05f;
    [SerializeField] bool isMovingForward = false;
    [SerializeField] bool isMovingBackward = false;
    [SerializeField] SplineRenderer endoscopyTail;

    void Start()
    { Init(); }

    void Init()
    {
        cameraFollower.follow = false;
        endoscopyTail.SetClipRange(0f, 0f);
    }

    public void MoveForward()
    {
        isMovingForward = true;
        isMovingBackward = false;
    }

    public void MoveBackward()
    {
        isMovingForward = false;
        isMovingBackward = true;
    }

    public void MovementStop()
    {
        isMovingForward = false;
        isMovingBackward = false;
    }

    void Update()
    {
        if (isMovingForward) {
            double _currentPercent = cameraFollower.result.percent;
            double _newPercent = Mathf.Clamp01((float)(_currentPercent + speedMovement * Time.deltaTime));
            cameraFollower.SetPercent(_newPercent);
            endoscopyTail.SetClipRange(0, _newPercent);
        }

        if (isMovingBackward)
        {
            double _currentPercent = cameraFollower.result.percent;
            double _newPercent = Mathf.Clamp01((float)(_currentPercent - speedMovement * Time.deltaTime));
            cameraFollower.SetPercent(_newPercent);
            endoscopyTail.SetClipRange(0, _newPercent);
        }
    }
}