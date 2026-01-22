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
        outlineEffct.enabled = _b;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == keyTag)
        { EnableOutline(true); }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == keyTag)
        { EnableOutline(false); }
    }

    public void SetOutlineColor(Color _col)
    {
        outlineEffct.OutlineColor = _col;
    }
}
