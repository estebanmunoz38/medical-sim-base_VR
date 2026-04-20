using UnityEngine;

public class Hemostasico : MonoBehaviour
{
    [Header("Materials Settings")]
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] Material ghostMaterial;
    [SerializeField] Material activeMaterial;

    [Header("Changing Settings")]
    [SerializeField] bool isChanging;
    [SerializeField] float changeTimer;

    float _timer = 0;
    bool _timerEnabled = false;

    void Awake()
    { Init(); }

    void Init()
    {
        GetMeshRenderer();
        GetMaterial();
    }

    private void GetMeshRenderer()
    { meshRenderer = this.GetComponent<MeshRenderer>(); }

    private void GetMaterial()
    { ghostMaterial = meshRenderer.sharedMaterial; }

    void ChangeMaterial(Material _mat)
    { meshRenderer.material = _mat; }

    void ChangeParent(Transform _parent)
    { this.gameObject.transform.SetParent(_parent); }

    void SetBackupMaterial()
    {
        activeMaterial = new Material(ghostMaterial);
        activeMaterial.color = Color.yellow;
        //activeMaterial.color.a = 1f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hemostasico"))
        {
            GameObject _other = other.gameObject;
            if (_other.name == "Hemostasico Tip")
            {
                HemostasicoTool _htool = _other.GetComponent<HemostasicoTool>();
                if (_htool.isPressing)
                {
                    isChanging = true; // <-- Activa el timer al presionar
                    Debug.Log("[Hemostasico] Contacto detectado. isChanging activado.");
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Hemostasico"))
        {
            isChanging = false;
            Debug.Log("[Hemostasico] exit");
        }
    }

    void Update()
    {
        if (isChanging && !_timerEnabled)
        {
            _timerEnabled = true;
            _timer = 0f;
            Debug.Log("[Hemostasico] Timer iniciado. Esperando " + changeTimer + " segundos...");
        }

        if (!isChanging && _timerEnabled)
        {
            _timerEnabled = false;
            _timer = 0f;
            Debug.Log("[Hemostasico] isChanging desactivado. Timer reiniciado.");
        }

        if (_timerEnabled)
        {
            _timer += Time.deltaTime;
            Debug.Log("[Hemostasico] Timer: " + _timer.ToString("F2") + " / " + changeTimer + "s");

            if (_timer >= changeTimer)
            {
                ChangeMaterial(activeMaterial);
                ChangeParent(null);
                _timerEnabled = false;
                _timer = 0f;
                isChanging = false;
                Debug.Log("[Hemostasico] Material cambiado a: " + activeMaterial.name + " | Parent reseteado.");
            }
        }
    }
}