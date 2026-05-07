using UnityEngine;
using UnityEngine.InputSystem;
using PaintIn3D;

public class MarkerBrushVariants : MonoBehaviour
{
    [Header("Paint in 3D")]
    [SerializeField] private CwPaintSphere paintSphere;

    [Header("Input")]
    [SerializeField] private InputActionProperty changeColorButton;   // Ej: X
    [SerializeField] private InputActionProperty changeSizeButton;    // Ej: Y

    [Header("Color Variants")]
    [SerializeField] private Color[] colors;

    [Header("Size Variants")]
    [SerializeField] private float[] radiusVariants;

    private int colorIndex;
    private int radiusIndex;

    private void Reset()
    {
        paintSphere = GetComponent<CwPaintSphere>();
    }

    private void OnEnable()
    {
        changeColorButton.action?.Enable();
        changeSizeButton.action?.Enable();
    }

    private void OnDisable()
    {
        changeColorButton.action?.Disable();
        changeSizeButton.action?.Disable();
    }

    private void Start()
    {
        ApplyColor();
        ApplyRadius();
    }

    private void Update()
    {
        if (paintSphere == null) return;

        if (changeColorButton.action != null && changeColorButton.action.WasPressedThisFrame())
        {
            NextColor();
        }

        if (changeSizeButton.action != null && changeSizeButton.action.WasPressedThisFrame())
        {
            NextRadius();
        }
    }

    private void NextColor()
    {
        if (colors == null || colors.Length == 0) return;

        colorIndex++;
        if (colorIndex >= colors.Length)
            colorIndex = 0;

        ApplyColor();
    }

    private void NextRadius()
    {
        if (radiusVariants == null || radiusVariants.Length == 0) return;

        radiusIndex++;
        if (radiusIndex >= radiusVariants.Length)
            radiusIndex = 0;

        ApplyRadius();
    }

    private void ApplyColor()
    {
        if (paintSphere == null || colors == null || colors.Length == 0) return;

        paintSphere.Color = colors[colorIndex];
    }

    private void ApplyRadius()
    {
        if (paintSphere == null || radiusVariants == null || radiusVariants.Length == 0) return;

        paintSphere.Radius = radiusVariants[radiusIndex];
    }
}