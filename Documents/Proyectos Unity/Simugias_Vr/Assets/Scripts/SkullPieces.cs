using UnityEngine;

public class SkullPieces : MonoBehaviour
{
    [Header("Variables requeridas")]
    [Tooltip("string key para la deteccion, colisionador necesario, efecto visual outline")]
    [SerializeField] string keyTag = "Anchor";
    [SerializeField] Collider col;
    [SerializeField] Outline outlineEffct;

    void Start()
    { Init(); }

    void Init()
    {
        EnableOutline(false);
        SetOutlineColor(Color.gold);
    }

    void EnableOutline(bool _b)
    {
        if (outlineEffct != null)
            outlineEffct.enabled = _b;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(keyTag))
            EnableOutline(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(keyTag))
            EnableOutline(false);
    }

    void OnDisable()
    {
        EnableOutline(false);
    }

    public void SetOutlineColor(Color _col)
    {
        if (outlineEffct != null)
            outlineEffct.OutlineColor = _col;
    }
}
