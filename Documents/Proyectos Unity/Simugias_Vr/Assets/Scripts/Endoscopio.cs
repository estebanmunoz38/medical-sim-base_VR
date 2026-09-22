using UnityEngine;
using Dreamteck.Splines;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Endoscopio : MonoBehaviour
{
    [Header("Camara de endoscopio")]
    [SerializeField] SplineFollower cameraFollower;
    [SerializeField] float speedMovement = 0.05f;
    [SerializeField] bool isMovingForward = false;
    [SerializeField] bool isMovingBackward = false;
    [SerializeField] SplineRenderer endoscopyTail;

    [Header("Input opcional (VR)")]
    [SerializeField] MonoBehaviour inputSourceBehaviour;
    [SerializeField] GameObject screenUI;
    [SerializeField] bool showTutorial = true;
    [TextArea] [SerializeField] string tutorialText =
        "Mantenga el control adelante para introducir la óptica por el acceso. Atrás para retirar. La imagen permanece en el monitor. La herramienta de corte (Kerrison) respeta la orientación de esta vista.";

    IToolInputSource _input;
    SurgicalGuideBeacon _guide;
    XRGrabInteractable _held;
    Rigidbody _heldBody;
    bool _forcedKinematic;
    float _pathPercent;

    public float Depth01 => cameraFollower != null ? (float)cameraFollower.result.percent : 0f;

    void Start()
    { Init(); }

    void Init()
    {
        if (cameraFollower != null)
        {
            cameraFollower.follow = false;
            _pathPercent = (float)cameraFollower.result.percent;
        }
        if (endoscopyTail != null)
            endoscopyTail.SetClipRange(0f, 0f);

        if (inputSourceBehaviour == null)
        {
            var composite = FindFirstObjectByType<CompositeToolInputSource>();
            if (composite != null)
                inputSourceBehaviour = composite;
            else
                inputSourceBehaviour = FindFirstObjectByType<ToolInputSource>();
        }

        _input = inputSourceBehaviour as IToolInputSource;

        if (showTutorial)
        {
            if (HandsOnlySession.Active)
                tutorialText = "Pellizco = avanzar por el acceso. Pellizco con la otra mano = retirar. La imagen queda en el monitor.";
            Transform anchor = screenUI != null ? screenUI.transform : transform;
            _guide = SurgicalGuideBeacon.Attach(anchor, "Endoscopio", tutorialText, new Vector3(0f, 0.1f, 0f));
        }
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
        if (cameraFollower == null)
            return;

        if (HeldEndoscope() != null)
        {
            UpdateTutorial();
            return;
        }

        if (_input != null)
        {
            if (_input.PrimaryHeld)
            {
                isMovingForward = true;
                isMovingBackward = false;
            }
            else if (_input.SecondaryHeld)
            {
                isMovingForward = false;
                isMovingBackward = true;
            }
            else if (Mathf.Abs(_input.ScrollDelta) < 0.001f)
            {
                isMovingForward = false;
                isMovingBackward = false;
            }
        }

        if (isMovingForward) {
            double _currentPercent = cameraFollower.result.percent;
            double _newPercent = Mathf.Clamp01((float)(_currentPercent + speedMovement * Time.deltaTime));
            cameraFollower.SetPercent(_newPercent);
            if (endoscopyTail != null)
                endoscopyTail.SetClipRange(0, _newPercent);
        }

        if (isMovingBackward)
        {
            double _currentPercent = cameraFollower.result.percent;
            double _newPercent = Mathf.Clamp01((float)(_currentPercent - speedMovement * Time.deltaTime));
            cameraFollower.SetPercent(_newPercent);
            if (endoscopyTail != null)
                endoscopyTail.SetClipRange(0, _newPercent);
        }

        UpdateTutorial();
    }

    void LateUpdate()
    {
        if (cameraFollower == null || cameraFollower.spline == null)
            return;

        XRGrabInteractable held = HeldEndoscope();
        if (held == null)
        {
            _pathPercent = (float)cameraFollower.result.percent;
            if (_forcedKinematic && _heldBody != null)
            {
                _heldBody.isKinematic = false;
                _forcedKinematic = false;
                _heldBody = null;
            }
            return;
        }

        Transform hand = held.interactorsSelecting.Count > 0
            ? held.interactorsSelecting[0].transform
            : held.transform;

        SplineSample projected = cameraFollower.spline.Project(hand.position);
        float target = Mathf.Clamp01((float)projected.percent);
        _pathPercent = Mathf.MoveTowards(_pathPercent, target, 1.8f * Time.deltaTime);
        cameraFollower.SetPercent(_pathPercent);
        if (endoscopyTail != null)
            endoscopyTail.SetClipRange(0, _pathPercent);

        SplineSample pose = cameraFollower.result;
        Vector3 forward = pose.forward.sqrMagnitude > 0.0001f ? pose.forward : held.transform.forward;
        Vector3 up = hand.up;
        if (Mathf.Abs(Vector3.Dot(forward.normalized, up.normalized)) > 0.96f)
            up = hand.right;
        Quaternion aim = Quaternion.LookRotation(forward, up);

        var body = held.GetComponent<Rigidbody>();
        if (body != null && !body.isKinematic)
        {
            body.isKinematic = true;
            _heldBody = body;
            _forcedKinematic = true;
        }

        held.transform.position = pose.position;
        held.transform.rotation = Quaternion.Slerp(held.transform.rotation, aim, 1f - Mathf.Exp(-18f * Time.deltaTime));
    }

    XRGrabInteractable HeldEndoscope()
    {
        if (_held != null && _held.isSelected && _held.gameObject.activeInHierarchy)
            return _held;

        _held = null;
        var handlers = FindObjectsByType<EndoscopioHandler>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < handlers.Length; i++)
        {
            if (handlers[i] == null)
                continue;
            var grab = handlers[i].GetComponent<XRGrabInteractable>();
            if (grab != null && grab.isSelected)
            {
                _held = grab;
                return _held;
            }
        }

        return null;
    }

    void UpdateTutorial()
    {
        if (_guide == null || cameraFollower == null)
            return;

        int depth = Mathf.RoundToInt((float)cameraFollower.result.percent * 100f);
        _guide.SetText("Endoscopio", $"Profundidad {depth} %.\n{tutorialText}");
    }
}