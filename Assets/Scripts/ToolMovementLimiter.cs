using UnityEngine;

public class ToolMovementLimiter : MonoBehaviour
{
    [Header("Position Limit")]
    public Transform zoneTransform;
    public Vector3 zoneSize = new Vector3(0.5f, 0.3f, 0.5f);

    [Header("Rotation Limit")]
    public bool limitRotation = true;

    public Vector3 minLocalEuler = new Vector3(-30f, -60f, -30f);
    public Vector3 maxLocalEuler = new Vector3(30f, 60f, 30f);

    [Header("Rotation Reference")]
    public Transform rotationReference;

    [Header("Options")]
    public bool useLateUpdate = true;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void LateUpdate()
    {
        if (useLateUpdate)
        {
            ApplyLimits();
        }
    }

    private void FixedUpdate()
    {
        if (!useLateUpdate)
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
        if (zoneTransform == null)
            return;

        Vector3 worldPosition = transform.position;

        // Pasamos la posición actual al espacio local de la zona
        Vector3 localPosition = zoneTransform.InverseTransformPoint(worldPosition);

        Vector3 halfSize = zoneSize * 0.5f;

        localPosition.x = Mathf.Clamp(localPosition.x, -halfSize.x, halfSize.x);
        localPosition.y = Mathf.Clamp(localPosition.y, -halfSize.y, halfSize.y);
        localPosition.z = Mathf.Clamp(localPosition.z, -halfSize.z, halfSize.z);

        Vector3 clampedWorldPosition = zoneTransform.TransformPoint(localPosition);

        if (rb != null && !rb.isKinematic)
        {
            rb.MovePosition(clampedWorldPosition);
        }
        else
        {
            transform.position = clampedWorldPosition;
        }
    }

    private void LimitRotation()
    {
        Quaternion referenceRotation = rotationReference != null
            ? rotationReference.rotation
            : Quaternion.identity;

        // Rotación de la herramienta relativa a la referencia
        Quaternion localRotation = Quaternion.Inverse(referenceRotation) * transform.rotation;

        Vector3 localEuler = localRotation.eulerAngles;

        localEuler.x = NormalizeAngle(localEuler.x);
        localEuler.y = NormalizeAngle(localEuler.y);
        localEuler.z = NormalizeAngle(localEuler.z);

        localEuler.x = Mathf.Clamp(localEuler.x, minLocalEuler.x, maxLocalEuler.x);
        localEuler.y = Mathf.Clamp(localEuler.y, minLocalEuler.y, maxLocalEuler.y);
        localEuler.z = Mathf.Clamp(localEuler.z, minLocalEuler.z, maxLocalEuler.z);

        Quaternion clampedLocalRotation = Quaternion.Euler(localEuler);
        Quaternion clampedWorldRotation = referenceRotation * clampedLocalRotation;

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
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    private void OnDrawGizmosSelected()
    {
        if (zoneTransform == null)
            return;

        Gizmos.color = Color.cyan;
        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(
            zoneTransform.position,
            zoneTransform.rotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(Vector3.zero, zoneSize);

        Gizmos.matrix = oldMatrix;
    }
}