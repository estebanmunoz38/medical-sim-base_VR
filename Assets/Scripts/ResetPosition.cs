using UnityEngine;

public class ResetPosition : MonoBehaviour
{
    public float minY = -2f; // altura mínima antes de resetear
    public string floorTag = "Floor"; // tag del piso (opcional)

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Rigidbody rb;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Reset por altura
        if (transform.position.y < minY)
        {
            ResetObject();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        /// Reset si toca el piso

        if (collision.gameObject.tag == floorTag)
        {
            ResetObject();
        }
    }

    void ResetObject()
    {
        // resetear posición y rotación
        transform.position = startPosition;
        transform.rotation = startRotation;

        // resetear físicas
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}