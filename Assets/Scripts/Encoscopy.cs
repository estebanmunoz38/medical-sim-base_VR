using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.XR.Content.Interaction;

public class Encoscopy : MonoBehaviour
{
    public Camera cam;
    public XRJoystick posStick;
    public XRJoystick rotStick;
    [SerializeField] float movementSpeed;
    [SerializeField] float rotationSpeed;
    private float _actualPosition;
    private float _actualRotationX;
    private float _actualRotationY;

    public SplineAnimate splineAnimate;

    void Start()
    { Init(); }

    private void Init()
    {
        splineAnimate.Pause();
        _actualPosition = splineAnimate.NormalizedTime;
        _actualRotationX = 0f;
        _actualRotationY = 0f;
    }

    void Update()
    {
        MoveSpline();
        RotateCamera();
    }

    public void MoveSpline()
    {
        float _joystickValue = posStick.value.x;
        //Debug.Log(_joystickValue);
        _actualPosition += _joystickValue * movementSpeed * Time.deltaTime;
        _actualPosition = Mathf.Clamp01(_actualPosition);
        splineAnimate.Pause();
        splineAnimate.NormalizedTime = _actualPosition;
    }

    public void RotateCamera()
    {
        float _rotationX = rotStick.value.y;
        float _rotationY = rotStick.value.x;

        _actualRotationX += _rotationX * rotationSpeed * Time.deltaTime;
        _actualRotationX = Mathf.Clamp(_actualRotationX, -5f, 5f);

        _actualRotationY += _rotationY * rotationSpeed * Time.deltaTime;
        _actualRotationY = Mathf.Clamp(_actualRotationY, -5f, 5f);

        cam.transform.localRotation = Quaternion.Euler(_actualRotationY, _actualRotationX, 0f);
    }

    public void EnableMovement()
    { movementSpeed = 0.25f; }

    public void DisableMovement()
    { movementSpeed = 0f; }

    public void SplineDuration(float _f)
    { splineAnimate.Duration = _f; }
}