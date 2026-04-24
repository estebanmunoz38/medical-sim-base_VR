using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ProcedureManager : MonoBehaviour
{
    public static ProcedureManager Instance;

    [Header("Timeline")]
    [SerializeField] private ProcedureTimelineSO timeline;
    [SerializeField] private int startStepIndex = 0;

    [Header("Pantalla tutorial")]
    [SerializeField] private GameObject tutorialScreenRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Slider progressSlider;

    [Header("Botones")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button resetStepButton;
    [SerializeField] private Button resetProcedureButton;

    [Header("Insistencia")]
    [SerializeField] private AudioSource reminderAudio;
    [SerializeField] private float defaultInactivitySeconds = 12f;

    private int currentStepIndex;
    private float inactivityTimer;
    private ProcedureStepSO currentStep;
    private bool waitingForStepCompletion;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(CompleteCurrentStep);

        if (resetStepButton != null)
            resetStepButton.onClick.AddListener(RestartCurrentStep);

        if (resetProcedureButton != null)
            resetProcedureButton.onClick.AddListener(RestartProcedure);

        currentStepIndex = startStepIndex;
        LoadStep(currentStepIndex);
    }

    private void Update()
    {
        if (currentStep == null) return;
        if (!waitingForStepCompletion) return;

        inactivityTimer += Time.deltaTime;

        float limit = currentStep.inactivitySeconds > 0 ? currentStep.inactivitySeconds : defaultInactivitySeconds;

        if (inactivityTimer >= limit)
        {
            inactivityTimer = 0f;
            ShowReminder();
        }
    }

    public void NotifyUserAction()
    {
        inactivityTimer = 0f;
    }

    public void CompleteCurrentStep()
    {
        if (currentStep == null) return;

        DisableCurrentVisuals();

        currentStepIndex++;

        if (timeline == null || timeline.steps == null || currentStepIndex >= timeline.steps.Length)
        {
            ShowEnd();
            return;
        }

        LoadStep(currentStepIndex);
    }

    public void CompleteStep(string stepID)
    {
        if (currentStep == null) return;
        if (currentStep.stepID != stepID) return;

        CompleteCurrentStep();
    }

    private void LoadStep(int index)
    {
        if (timeline == null || timeline.steps == null || timeline.steps.Length == 0)
        {
            Debug.LogError("ProcedureManager: falta asignar Timeline.");
            return;
        }

        currentStep = timeline.steps[index];
        waitingForStepCompletion = true;
        inactivityTimer = 0f;

        if (tutorialScreenRoot != null)
            tutorialScreenRoot.SetActive(true);

        if (titleText != null)
            titleText.text = currentStep.title;

        if (instructionText != null)
            instructionText.text = currentStep.instruction;

        if (progressText != null)
            progressText.text = "Paso " + (index + 1) + " de " + timeline.steps.Length;

        if (progressSlider != null)
            progressSlider.value = (float)(index + 1) / timeline.steps.Length;

        ApplyStepVisuals(currentStep);

        if (continueButton != null)
            continueButton.gameObject.SetActive(!currentStep.waitForExternalComplete || currentStep.isCreditsStep);
    }

    private void ApplyStepVisuals(ProcedureStepSO step)
    {
        foreach (GameObject obj in step.objectsToDisable)
            if (obj != null) obj.SetActive(false);

        foreach (GameObject obj in step.objectsToEnable)
            if (obj != null) obj.SetActive(true);

        if (step.validActionZone != null)
            step.validActionZone.SetActive(true);

        if (step.movementPath != null)
            step.movementPath.gameObject.SetActive(true);
    }

    private void DisableCurrentVisuals()
    {
        if (currentStep == null) return;

        if (currentStep.validActionZone != null)
            currentStep.validActionZone.SetActive(false);

        if (currentStep.movementPath != null)
            currentStep.movementPath.gameObject.SetActive(false);
    }

    private void ShowReminder()
    {
        if (currentStep == null) return;

        if (instructionText != null && !string.IsNullOrEmpty(currentStep.inactivityReminder))
            instructionText.text = currentStep.inactivityReminder;

        if (reminderAudio != null)
            reminderAudio.Play();
    }

    public void RestartCurrentStep()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RestartProcedure()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ShowEnd()
    {
        waitingForStepCompletion = false;

        if (titleText != null)
            titleText.text = "Procedimiento finalizado";

        if (instructionText != null)
            instructionText.text = "El procedimiento fue completado correctamente.";

        if (progressText != null)
            progressText.text = "Completado";

        if (progressSlider != null)
            progressSlider.value = 1f;

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
    }
}