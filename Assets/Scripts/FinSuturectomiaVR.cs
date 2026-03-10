using UnityEngine;

public class FinSuturectomiaVR : MonoBehaviour
{
    [Header("Hoja / punta de la herramienta (igual que el bisturí)")]
    [SerializeField] Collider bladeCollider;

    [Header("Waypoints de sutura")]
    [SerializeField] GameObject initialPoint;
    private bool initialPointDone = false;

    [SerializeField] GameObject midPoint;
    private bool midPointDone = false;

    [SerializeField] GameObject finalPoint;
    private bool finalPointDone = false;

    [Header("Animaciones (cierre inverso)")]
    [SerializeField] BoneCutClip inferiorClips;
    [SerializeField] BoneCutClip superiorClips;

    [Header("Helper visual")]
    [SerializeField] GameObject suturaHelper;

    [Header("Hilo de sutura")]
    [SerializeField] LineRenderer sutureLine;
    [SerializeField] float lineWidth = 0.002f;

    private int lineIndex = 0;

    void Start()
    {
        if (bladeCollider == null)
        {
            Debug.LogError("FinSuturectomiaVR: falta bladeCollider.");
            enabled = false;
            return;
        }

        if (initialPoint != null) initialPoint.SetActive(true);
        if (midPoint != null) midPoint.SetActive(false);
        if (finalPoint != null) finalPoint.SetActive(false);

        if (suturaHelper != null)
            suturaHelper.SetActive(true);

        if (sutureLine != null)
        {
            sutureLine.useWorldSpace = true;
            sutureLine.positionCount = 0;
            sutureLine.startWidth = lineWidth;
            sutureLine.endWidth = lineWidth;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Solo reaccionar si el que tocó fue la punta real
        if (other != bladeCollider) return;

        if (!initialPointDone && other.gameObject.name == initialPoint.name)
        {
            initialPointDone = true;
            initialPoint.SetActive(false);
            if (midPoint != null) midPoint.SetActive(true);

            AddLinePoint(initialPoint.transform.position);
            ChangeAnimations("incision_2");
            CheckAllPoints();
            return;
        }

        if (!midPointDone && other.gameObject.name == midPoint.name)
        {
            midPointDone = true;
            midPoint.SetActive(false);
            if (finalPoint != null) finalPoint.SetActive(true);

            AddLinePoint(midPoint.transform.position);
            ChangeAnimations("incision_1");
            CheckAllPoints();
            return;
        }

        if (!finalPointDone && other.gameObject.name == finalPoint.name)
        {
            finalPointDone = true;
            finalPoint.SetActive(false);

            AddLinePoint(finalPoint.transform.position);
            CheckAllPoints();
        }
    }

    void AddLinePoint(Vector3 pos)
    {
        if (sutureLine == null) return;

        sutureLine.positionCount++;
        sutureLine.SetPosition(lineIndex, pos);
        lineIndex++;
    }

    void CheckAllPoints()
    {
        if (initialPointDone && midPointDone && finalPointDone)
        {
            CompleteSuture();
        }
    }

    void ChangeAnimations(string key)
    {
        if (superiorClips != null)
            superiorClips.ChangeClip(key);

        if (inferiorClips != null)
            inferiorClips.ChangeClip(key);
    }

    void CompleteSuture()
    {
        if (suturaHelper != null)
            suturaHelper.SetActive(false);

        Debug.Log("SUTURA COMPLETA");
    }
}