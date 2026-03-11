using UnityEngine;

public class Coagulador : MonoBehaviour
{
    [Header("Render & Materials")]
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] Material localMaterial;
    [SerializeField] Material backupMaterial;

    [Header("Timer Settings")]
    [SerializeField] private float timerDuration = 1.5f;
    private float currentTimer = 0f;
    private bool isCountingDown = false;

    void Awake()
    { Init(); }

    void Init()
    {
        GetMeshRenderer();
        GetMaterial();
        SetBackupMaterial();
    }

    private void GetMeshRenderer()
    { meshRenderer = this.GetComponent<MeshRenderer>(); }

    private void GetMaterial()
    { localMaterial = meshRenderer.sharedMaterial; }

    private void SetBackupMaterial()
    {
        backupMaterial = new Material(localMaterial);
        backupMaterial.color = Color.orange;
    }

    void Update()
    {
        if (isCountingDown)
        {
            currentTimer -= Time.deltaTime;
            if (currentTimer <= 0f)
            {
                currentTimer = 0f;
                isCountingDown = false;
                ChangeColor();
                Debug.Log("Time = 0f");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coagulador"))
        {
            Debug.Log("Trigger y PRENDE");
            EnableCounter();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Coagulador"))
        { DeactivateCounter(); }
    }

    void EnableCounter()
    {
        Debug.Log("ENABLE COUNTER");
        currentTimer = timerDuration;
        isCountingDown = true;
    }

    void DeactivateCounter()
    {
        Debug.Log("DEACTIVATE COUNTER");
        isCountingDown = false;
        currentTimer = 0;
    }

    void ChangeColor()
    { meshRenderer.material = backupMaterial; }
}