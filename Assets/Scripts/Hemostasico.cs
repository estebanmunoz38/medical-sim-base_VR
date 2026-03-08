using UnityEngine;

public class Hemostasico : MonoBehaviour
{
    [Header("Materials Settings")]
    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] Material ghostMaterial;
    [SerializeField] Material activeMaterial;

    [Header("Materials Settings")]
    [SerializeField] bool isChanging;

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


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hemostasico"))
        {
            GameObject _other = other.gameObject;
            if(_other.name == "Hemostasico Tip")
            {
                HemostasicoTool _htool = _other.GetComponent<HemostasicoTool>();
                if(_htool.isPressing)
                {
                    ChangeMaterial(activeMaterial);
                    ChangeParent(null);
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Hemostasico"))
        { Debug.Log("exit"); }
    }

    void Update()
    {
        
    }
}
