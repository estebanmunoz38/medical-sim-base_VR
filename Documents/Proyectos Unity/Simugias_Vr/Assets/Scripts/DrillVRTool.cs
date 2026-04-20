using UnityEngine;

public class DrillVRTool : MonoBehaviour
{
    [Header("Input VR")]
    public MonoBehaviour inputSourceBehaviour;
    private IToolInputSource input;

    [Header("Drill Model")]
    public Transform drillModel;
    public Transform drillTip;

    [Header("Snap")]
    public Transform snapPoint;
    public float snapDistance = 0.08f;

    [Header("Drill Motion")]
    public float maxDepth = 0.04f;      // profundidad máxima en metros
    public float duraMargin = 0.002f;   // margen para no romper la dura
    public float drillSpeed = 0.015f;

    [Header("HUD / Feedback")]
    public GameObject hudMessage;
    public GameObject successIcon;
    public GameObject failIcon;

    private bool isAnchored = false;
    private Vector3 anchoredPosition;
    private Quaternion anchoredRotation;
    private float tDepth = 0f;

    private bool mustWithdraw = false;
    private bool succeeded = false;
    private bool failed = false;


    void Start()
    {
        input = inputSourceBehaviour as IToolInputSource;

        if (input == null)
        {
            Debug.LogError("❌ DrillVRTool: inputSourceBehaviour NO implementa IToolInputSource");
            enabled = false;
            return;
        }

        if (drillModel == null || drillTip == null)
        {
            Debug.LogError("❌ DrillVRTool: drillModel o drillTip no asignados");
            enabled = false;
            return;
        }
    }


    void Update()
    {
        if (!isAnchored)
        {
            TrySnap();
        }
        else
        {
            DrillProgress();

            if (mustWithdraw && input.SecondaryDown)
                ResetTool();
        }
    }


    // =====================================================================================
    // INTENTAR ENGANCHAR AL SNAP POINT
    // =====================================================================================
    void TrySnap()
    {
        float dist = Vector3.Distance(drillTip.position, snapPoint.position);

        if (dist <= snapDistance && input.PrimaryDown)
        {
            isAnchored = true;

            anchoredPosition = snapPoint.position;
            anchoredRotation = snapPoint.rotation;

            drillModel.position = snapPoint.position;
            drillModel.rotation = snapPoint.rotation;

            ShowHUD("Enganchado. Mantén para perforar.");
        }
    }


    // =====================================================================================
    // PROGRESO DE PERFORACIÓN
    // =====================================================================================
    void DrillProgress()
    {
        if (failed || succeeded)
            return;

        // Mantener para perforar
        if (input.PrimaryHeld)
        {
            tDepth += drillSpeed * Time.deltaTime;
            tDepth = Mathf.Clamp(tDepth, 0f, maxDepth);

            // Mover la mecha hacia adelante
            drillModel.position = anchoredPosition + anchoredRotation * Vector3.forward * tDepth;

            // Check de profundidad
            if (tDepth >= maxDepth - duraMargin)
            {
                succeeded = true;
                mustWithdraw = true;

                ShowSuccess();
            }
        }
        else
        {
            // Si no está presionando, mostrar mensaje de soltar
            if (!succeeded)
                ShowHUD("Mantén para perforar…");
        }

        // Si se excede el margen → fallo
        if (tDepth > maxDepth)
        {
            failed = true;
            mustWithdraw = true;
            ShowFail();
        }
    }


    // =====================================================================================
    // HUD
    // =====================================================================================
    void ShowHUD(string msg)
    {
        if (hudMessage != null)
        {
            hudMessage.SetActive(true);
            hudMessage.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = msg;
        }
    }

    void ShowSuccess()
    {
        HideHUD();

        if (successIcon != null)
            successIcon.SetActive(true);

        ShowHUD("Perforación correcta.");
    }

    void ShowFail()
    {
        HideHUD();

        if (failIcon != null)
            failIcon.SetActive(true);

        ShowHUD("Fallo: demasiado profundo.");
    }

    void HideHUD()
    {
        if (hudMessage != null) hudMessage.SetActive(false);
        if (successIcon != null) successIcon.SetActive(false);
        if (failIcon != null) failIcon.SetActive(false);
    }


    // =====================================================================================
    // RESET
    // =====================================================================================
    void ResetTool()
    {
        isAnchored = false;
        mustWithdraw = false;
        succeeded = false;
        failed = false;

        tDepth = 0f;

        HideHUD();
    }
}
