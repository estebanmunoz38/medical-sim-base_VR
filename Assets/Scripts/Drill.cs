using UnityEngine;
using UnityEngine.InputSystem;

public class Drill : MonoBehaviour
{
    [Header("Resultado visual")]
    public GameObject skullCapToHideOnComplete;

    [Header("Materiales / Zona")]
    public Renderer targetRenderer;
    public Material baseMat;
    public Material[] interactableMat;

    [Header("Visual del taladro")]
    public Transform drillBit;
    public float drillBitRotationSpeed = 1500f;
    public Transform visualModel;
    public float vibrationAmount = 0.002f;

    [Header("Particulas")]
    public ParticleSystem drillParticles;

    [Header("Audio")]
    public AudioSource drillAudio;

    [Header("Gameplay")]
    public float drillProgressSpeed = 35f;
    public float requiredProgress = 100f;

    [Header("Debug")]
    public bool useKeyboardDebug = true;

    [SerializeField] private bool isDrilling;
    [SerializeField] private bool isTouchingValidZone;
    [SerializeField] private float currentProgress;
    [SerializeField] private bool drillingCompleted;

    private Vector3 originalVisualPosition;
    private Collider currentZone;
    private bool wasDrillingLastFrame;

    private void Start()
    {
        if (visualModel != null)
            originalVisualPosition = visualModel.localPosition;

        // IMPORTANTE:
        // NO desactivamos la tapita al iniciar.
        // Solo se apaga cuando la perforación se completa.

        StopDrillFeedback();
        ResetColor();
    }

    private void Update()
    {
        if (useKeyboardDebug && Keyboard.current != null)
        {
            EnableDrill(Keyboard.current.spaceKey.isPressed);
        }

        if (isDrilling && !drillingCompleted)
        {
            RotateDrillBit();
        }

        if (isDrilling && isTouchingValidZone && !drillingCompleted)
        {
            DrillProgress();
            PlayDrillFeedback();
            wasDrillingLastFrame = true;
        }
        else
        {
            if (wasDrillingLastFrame)
            {
                StopDrillFeedback();
                wasDrillingLastFrame = false;
            }
        }
    }

    public void EnableDrill(bool value)
    {
        isDrilling = value;

        if (!isDrilling)
            StopDrillFeedback();
    }

    public void ResetColor()
    {
        if (targetRenderer != null && baseMat != null)
            targetRenderer.material = baseMat;
    }

    public void ChangeColor(int id)
    {
        if (targetRenderer == null) return;
        if (interactableMat == null) return;
        if (id < 0 || id >= interactableMat.Length) return;

        targetRenderer.material = interactableMat[id];
    }

    private void RotateDrillBit()
    {
        if (drillBit == null) return;

        drillBit.Rotate(Vector3.up * drillBitRotationSpeed * Time.deltaTime, Space.Self);
    }

    private void DrillProgress()
    {
        currentProgress += drillProgressSpeed * Time.deltaTime;

        if (currentProgress >= requiredProgress)
        {
            currentProgress = requiredProgress;
            CompleteDrilling();
        }
    }

    private void CompleteDrilling()
    {
        drillingCompleted = true;
        isDrilling = false;

        if (skullCapToHideOnComplete != null)
            skullCapToHideOnComplete.SetActive(false);

        StopDrillFeedback();
        ResetColor();
    }

    private void PlayDrillFeedback()
    {
        if (drillingCompleted) return;

        if (drillParticles != null && !drillParticles.isPlaying)
        {
            drillParticles.Clear(true);
            drillParticles.Play(true);
        }

        if (drillAudio != null && !drillAudio.isPlaying)
            drillAudio.Play();

        if (visualModel != null)
            visualModel.localPosition = originalVisualPosition + Random.insideUnitSphere * vibrationAmount;
    }

    private void StopDrillFeedback()
    {
        if (drillParticles != null && drillParticles.isPlaying)
            drillParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (drillAudio != null && drillAudio.isPlaying)
            drillAudio.Stop();

        if (visualModel != null)
            visualModel.localPosition = originalVisualPosition;
    }

    private void OnTriggerStay(Collider other)
    {
        if (drillingCompleted) return;

        if (other.name == "low")
        {
            SetZone(other, 0);
        }
        else if (other.name == "ideal")
        {
            SetZone(other, 1);
        }
        else if (other.name == "high")
        {
            SetZone(other, 2);
        }
    }

    private void SetZone(Collider zone, int materialID)
    {
        currentZone = zone;
        isTouchingValidZone = true;
        ChangeColor(materialID);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == currentZone)
        {
            isTouchingValidZone = false;
            currentZone = null;
            ResetColor();
            StopDrillFeedback();
        }
    }

    public void ResetDrillProgress()
    {
        currentProgress = 0f;
        drillingCompleted = false;
        isTouchingValidZone = false;
        currentZone = null;

        if (skullCapToHideOnComplete != null)
            skullCapToHideOnComplete.SetActive(true);

        StopDrillFeedback();
        ResetColor();
    }
}