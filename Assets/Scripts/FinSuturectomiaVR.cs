using System.Collections;
using UnityEngine;

public class FinSuturectomiaVR : MonoBehaviour
{
    [Header("Hoja / punta de la herramienta")]
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

    [Header("Puntos visuales de sutura")]
    [SerializeField] GameObject stitchPoint1;
    [SerializeField] GameObject stitchPoint2;
    [SerializeField] GameObject stitchPoint3;


    private int lineIndex = 0;
    private bool sutureCompleted = false;

    private void Start()
    {
        if (bladeCollider == null)
        {
            Debug.LogError("FinSuturectomiaVR: falta asignar bladeCollider en el Inspector.");
            enabled = false;
            return;
        }

        if (initialPoint == null || midPoint == null || finalPoint == null)
        {
            Debug.LogError("FinSuturectomiaVR: faltan asignar uno o más waypoints.");
            enabled = false;
            return;
        }

    if (stitchPoint1) stitchPoint1.SetActive(false);
    if (stitchPoint2) stitchPoint2.SetActive(false);
    if (stitchPoint3) stitchPoint3.SetActive(false);
    
        initialPointDone = false;
        midPointDone = false;
        finalPointDone = false;
        sutureCompleted = false;

        initialPoint.SetActive(true);
        midPoint.SetActive(false);
        finalPoint.SetActive(false);

        if (suturaHelper != null)
            suturaHelper.SetActive(false);

        if (sutureLine != null)
        {
            sutureLine.useWorldSpace = true;
            sutureLine.positionCount = 0;
            sutureLine.startWidth = lineWidth;
            sutureLine.endWidth = lineWidth;
        }

        lineIndex = 0;

        Debug.Log("FinSuturectomiaVR iniciado correctamente.");
    }


public void OnToolGrab()
{
    Debug.Log("Herramienta agarrada");
  if (ProcedureManager.Instance != null)
    ProcedureManager.Instance.CompleteStep("take_suture");  

    if (suturaHelper != null && !sutureCompleted)
        suturaHelper.SetActive(true);
}

public void OnToolRelease()
{
    Debug.Log("Herramienta soltada");

    if (suturaHelper != null)
        suturaHelper.SetActive(false);
}


    private void OnTriggerEnter(Collider other)
    {
        if (other == null || sutureCompleted) return;

        Debug.Log("OnTriggerEnter con: " + other.name);

        if (!initialPointDone && other.gameObject == initialPoint)
        {
            Debug.Log("Waypoint inicial tocado.");

            initialPointDone = true;
            initialPoint.SetActive(false);
            midPoint.SetActive(true);
            if (stitchPoint1) stitchPoint1.SetActive(true);
            AddLinePoint(initialPoint.transform.position);
            CheckAllPoints();
            return;
        }

        if (initialPointDone && !midPointDone && other.gameObject == midPoint)
        {
            Debug.Log("Waypoint medio tocado.");

            midPointDone = true;
            midPoint.SetActive(false);
            finalPoint.SetActive(true);
             if (stitchPoint2) stitchPoint2.SetActive(true);
            AddLinePoint(midPoint.transform.position);
            CheckAllPoints();
            return;
        }

        if (initialPointDone && midPointDone && !finalPointDone && other.gameObject == finalPoint)
        {
            Debug.Log("Waypoint final tocado.");

            finalPointDone = true;
            finalPoint.SetActive(false);
            ChangeAnimations("Cerrado");
            if (stitchPoint3) stitchPoint3.SetActive(true);
            AddLinePoint(finalPoint.transform.position);
            CheckAllPoints();
            return;
        }

        Debug.Log("Entró a un trigger que no corresponde al paso actual: " + other.name);
    }

    private void AddLinePoint(Vector3 pos)
    {
        if (sutureLine == null) return;

        sutureLine.positionCount++;
        sutureLine.SetPosition(lineIndex, pos);
        lineIndex++;

        Debug.Log("Punto agregado al hilo de sutura: " + pos);
    }

    private void CheckAllPoints()
    {
        if (sutureCompleted) return;

        if (initialPointDone && midPointDone && finalPointDone)
        {
            sutureCompleted = true;
            StartCoroutine(CompleteSutureDeferred());
        }
    }

    private void ChangeAnimations(string key)
    {
        Debug.Log("Cambiando animaciones a: " + key);

        if (superiorClips != null)
            superiorClips.ChangeClip(key);

        if (inferiorClips != null)
            inferiorClips.ChangeClip(key);
    }

    private IEnumerator CompleteSutureDeferred()
    {
        yield return null;
        CompleteSuture();
    }

    private void CompleteSuture()
{
    if (suturaHelper != null)
        suturaHelper.SetActive(false);

    Debug.Log("SUTURA COMPLETA");

    if (ProcedureManager.Instance != null)
        ProcedureManager.Instance.CompleteStep("make_suture");
}

}