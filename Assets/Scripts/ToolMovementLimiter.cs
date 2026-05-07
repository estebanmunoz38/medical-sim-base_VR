using UnityEngine;

public class ToolMovementLimiter : MonoBehaviour
{
    [Header("Position Limit")]
    public BoxCollider zoneCollider;
    public Transform constraintPoint;

    [Header("Rotation Limit")]
    public bool limitRotation = true;
    public Transform rotationReference;

    public Vector3 minLocalEuler = new Vector3(-30f, -45f, -20f);
    public Vector3 maxLocalEuler = new Vector3(30f, 45f, 20f);

    [Header("Options")]
    public bool applyInLateUpdate = true;
    public bool applyBeforeRender = true;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (applyBeforeRender)
        {
            Application.onBeforeRender += ApplyLimits;
        }
    }

    private void OnDisable()
    {
        if (applyBeforeRender)
        {
            Application.onBeforeRender -= ApplyLimits;
        }
    }

    private void LateUpdate()
    {
        if (applyInLateUpdate)
        {
            ApplyLimits();
        }
    }

    private void ApplyLimits()
    {
        LimitPosition();

        if (limitRotation)
        {
            LimitRotation();
        }
    }

    private void LimitPosition()
    {
        if (zoneCollider == null || constraintPoint == null)
            return;

        Transform zoneTransform = zoneCollider.transform;

        Vector3 localPoint = zoneTransform.InverseTransformPoint(constraintPoint.position);

        Vector3 center = zoneCollider.center;
        Vector3 halfSize = zoneCollider.size * 0.5f;

        Vector3 min = center - halfSize;
        Vector3 max = center + halfSize;

        Vector3 clampedLocalPoint = localPoint;

        clampedLocalPoint.x = Mathf.Clamp(clampedLocalPoint.x, min.x, max.x);
        clampedLocalPoint.y = Mathf.Clamp(clampedLocalPoint.y, min.y, max.y);
        clampedLocalPoint.z = Mathf.Clamp(clampedLocalPoint.z, min.z, max.z);

        Vector3 clampedWorldPoint = zoneTransform.TransformPoint(clampedLocalPoint);

        Vector3 correction = clampedWorldPoint - constraintPoint.position;

        Vector3 newPosition = transform.position + correction;

        if (rb != null && !rb.isKinematic)
        {
            rb.MovePosition(newPosition);
        }
        else
        {
            transform.position = newPosition;
        }
    }

    private void LimitRotation()
    {
        Transform reference = rotationReference != null ? rotationReference : zoneCollider != null ? zoneCollider.transform : null;

        if (reference == null)
            return;

        Quaternion desiredWorldRotation = transform.rotation;

        Quaternion localRotation = Quaternion.Inverse(reference.rotation) * desiredWorldRotation;

        Vector3 euler = localRotation.eulerAngles;

        euler.x = NormalizeAngle(euler.x);
        euler.y = NormalizeAngle(euler.y);
        euler.z = NormalizeAngle(euler.z);

        euler.x = Mathf.Clamp(euler.x, minLocalEuler.x, maxLocalEuler.x);
        euler.y = Mathf.Clamp(euler.y, minLocalEuler.y, maxLocalEuler.y);
        euler.z = Mathf.Clamp(euler.z, minLocalEuler.z, maxLocalEuler.z);

        Quaternion clampedLocalRotation = Quaternion.Euler(euler);
        Quaternion clampedWorldRotation = reference.rotation * clampedLocalRotation;

        if (rb != null && !rb.isKinematic)
        {
            rb.MoveRotation(clampedWorldRotation);
        }
        else
        {
            transform.rotation = clampedWorldRotation;
        }
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    private void OnDrawGizmosSelected()
    {
        if (zoneCollider == null)
            return;

        Gizmos.color = Color.cyan;

        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(
            zoneCollider.transform.position,
            zoneCollider.transform.rotation,
            zoneCollider.transform.lossyScale
        );

        Gizmos.DrawWireCube(zoneCollider.center, zoneCollider.size);

        Gizmos.matrix = oldMatrix;
    }
}