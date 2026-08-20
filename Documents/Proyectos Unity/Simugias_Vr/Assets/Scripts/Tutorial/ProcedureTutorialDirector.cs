using System.Collections;
using System.Collections.Generic;
using Logic.SurgicalProcedure;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Orquesta el entrenamiento interactivo. Reutiliza Outline, las zonas ya
/// marcadas en escena y los flags reales de cada herramienta.
/// </summary>
[DefaultExecutionOrder(200)]
public class ProcedureTutorialDirector : MonoBehaviour
{
    [Header("Modo")]
    [SerializeField] bool startAutomatically = true;
    [SerializeField] bool resumeSavedProgress = true;
    [SerializeField] bool hideLocalToolBeacons = true;
    [SerializeField] bool pauseWithMenuButton = true;

    [Header("Curriculum")]
    [Tooltip("Catálogo opcional. Si está vacío, se usan los módulos de abajo o el currículum por defecto.")]
    [SerializeField] TutorialCatalogAsset catalog;
    [Tooltip("Si está vacío, se usa el currículum por defecto de trigonocefalia.")]
    [SerializeField] TutorialModuleAsset[] modulesOverride;

    [Header("Audio (opcional — si falta, se generan tonos)")]
    [SerializeField] AudioClip attentionClip;
    [SerializeField] AudioClip selectClip;
    [SerializeField] AudioClip confirmClip;
    [SerializeField] AudioClip errorClip;
    [SerializeField] AudioClip transitionClip;

    [Header("Ayuda progresiva (segundos)")]
    [SerializeField] float hintDelay1 = 6f;
    [SerializeField] float hintDelay2 = 14f;
    [SerializeField] float hintDelay3 = 22f;

    [Header("Debug")]
    [SerializeField] bool enableDebugTools = true;

    [Header("Eventos")]
    public UnityEvent OnTutorialStarted;
    public UnityEvent<string> OnStepStarted;
    public UnityEvent<string> OnStepCompleted;
    public UnityEvent<string> OnModuleCompleted;
    public UnityEvent OnTutorialCompleted;
    public UnityEvent OnEnteredFreeMode;
    public UnityEvent OnWrongAction;

    TutorialFollowHud _hud;
    TutorialWorldMarker _marker;
    TutorialAudioPlayer _audio;
    TutorialModuleConfig[] _modules;
    readonly List<(int module, int step)> _index = new List<(int, int)>();
    int _cursor;
    int _activeModule = -1;
    float _stepStart;
    float _lookTimer;
    float _nudgeLevel;
    bool _holdingLogged;
    bool _awaitingRelease;
    bool _advancing;
    bool _wrongFlash;
    Vector3 _startForward;
    Vector3 _holdOrigin;
    Outline _activeOutline;
    readonly List<Outline> _ownedOutlines = new List<Outline>();
    TutorialPlayMode _mode = TutorialPlayMode.Tutorial;
    float _lastErrorTime;
    bool _menuWasPressed;

    ShaveVRTool _shave;
    MarkerVRTool _markerTool;
    ScalpelVRTool _scalpel;
    BisturiCutControl _bisturi;
    RetractorVRTool _retractor;
    DiseccionSubcutaneaFontanelaVR _diseccion;
    DrillVRTool _drill;
    EndoscopeVRTool _endoVr;
    Endoscopio _endo;
    Kerrison _kerrison;
    CoagulacionOseaHemostasiaVR _coag;
    FinSuturectomiaVR _suture;
    PlasticaCutaneaVR _plasty;
    Hemostasico _hemo;
    SurgicalProcedureManager _procedure;

    public TutorialPlayMode Mode => _mode;
    public string CurrentStepId => HasStep ? CurrentStep.id : string.Empty;
    public string CurrentModuleId => HasStep ? CurrentModule.id : string.Empty;
    public string CurrentTargetName => CurrentFocus() != null ? CurrentFocus().name : "(ninguno)";
    public string CurrentCompletion => HasStep ? CurrentStep.completeWhen.ToString() : string.Empty;

    bool HasStep => _modules != null && _cursor >= 0 && _cursor < _index.Count;
    TutorialModuleConfig CurrentModule => _modules[_index[_cursor].module];
    TutorialStepConfig CurrentStep => CurrentModule.steps[_index[_cursor].step];

    bool DebugOn => enableDebugTools && (Application.isEditor || Debug.isDebugBuild);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindFirstObjectByType<ProcedureTutorialDirector>() != null)
            return;

        bool hasSim = FindFirstObjectByType<ScalpelVRTool>() != null
                      || FindFirstObjectByType<Kerrison>() != null
                      || FindFirstObjectByType<Endoscopio>() != null
                      || FindFirstObjectByType<RetractorVRTool>() != null
                      || FindFirstObjectByType<SurgicalProcedureManager>() != null;
        if (!hasSim)
            return;

        GameObject root = GameObject.Find("Tutorial_Root");
        if (root == null)
            root = new GameObject("Tutorial_Root");
        root.AddComponent<ProcedureTutorialDirector>();
    }

    void Start()
    {
        CacheTools();
        _modules = BuildModules();
        Flatten();

        _hud = TutorialFollowHud.Create(transform);
        _marker = TutorialWorldMarker.Create(transform);
        _audio = TutorialAudioPlayer.Create(transform);
        _audio.BindClips(attentionClip, selectClip, confirmClip, errorClip, transitionClip);

        _startForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;

        if (hideLocalToolBeacons)
            SilenceLocalBeacons();

        if (!startAutomatically)
        {
            EnterFreeMode(false);
            return;
        }

        if (resumeSavedProgress && TutorialProgressStore.Finished)
        {
            EnterFreeMode(true);
            return;
        }

        if (resumeSavedProgress)
            RestoreProgress();

        TutorialProgressStore.Started = true;
        _mode = TutorialPlayMode.Tutorial;
        OnTutorialStarted?.Invoke();
        ApplyStep(true);
    }

    void OnDestroy()
    {
        ClearOutline();
    }

    void Update()
    {
        HandlePauseInput();
        HandleDebugInput();

        if (_mode == TutorialPlayMode.Free)
        {
            RefreshFreeHud();
            return;
        }

        if (_mode == TutorialPlayMode.Paused)
        {
            RefreshHud(true, false, false);
            return;
        }

        if (!HasStep || _advancing)
            return;

        var step = CurrentStep;
        RefreshHud(false, false, _wrongFlash);
        UpdateFocus(step);
        PulseOutline();
        WatchWrongTool(step);
        WatchHold(step);

        if (_wrongFlash && Time.time - _lastErrorTime > 1.2f)
            _wrongFlash = false;

        if (step.skipIfTargetMissing && ((NeedsTarget(step) && CurrentFocus() == null) || GateToolMissing(step)))
        {
            SkipMissing();
            return;
        }

        if (IsStepComplete(step))
        {
            CompleteCurrent();
            return;
        }

        if (step.allowTimeout && step.timeoutSeconds > 0f && Time.time - _stepStart > step.timeoutSeconds)
            CompleteCurrent();
    }

    #region Public API

    public void PauseTutorial()
    {
        if (_mode != TutorialPlayMode.Tutorial) return;
        _mode = TutorialPlayMode.Paused;
        _marker.SetVisible(false);
        RefreshHud(true, false, false);
    }

    public void ResumeTutorial()
    {
        if (_mode != TutorialPlayMode.Paused) return;
        _mode = TutorialPlayMode.Tutorial;
        ApplyStep(false);
    }

    public void TogglePause()
    {
        if (_mode == TutorialPlayMode.Paused) ResumeTutorial();
        else if (_mode == TutorialPlayMode.Tutorial) PauseTutorial();
    }

    public void RestartTutorial()
    {
        TutorialProgressStore.Clear();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RestartModule()
    {
        if (!HasStep) return;
        int module = _index[_cursor].module;
        for (int i = 0; i < _index.Count; i++)
        {
            if (_index[i].module == module)
            {
                _cursor = i;
                ApplyStep(true);
                return;
            }
        }
    }

    public void EnterFreeMode(bool alreadyFinished)
    {
        _mode = TutorialPlayMode.Free;
        _stepStart = Time.time;
        ClearOutline();
        _marker.SetVisible(false);
        if (alreadyFinished)
            TutorialProgressStore.Finished = true;
        OnEnteredFreeMode?.Invoke();
        if (_hud != null)
        {
            _hud.SetCard(
                "MODO LIBRE",
                alreadyFinished ? "Entrenamiento completado" : "Práctica sin guía",
                "Practique sin asistencia",
                "Las flechas se ocultan. Puede operar con libertad.",
                "Reinicie la escena para repetir el tutorial. Menú del control no reabre la guía.",
                1f, false, alreadyFinished, false);
        }
    }

    public void ForceCompleteStep()
    {
        if (_mode == TutorialPlayMode.Tutorial && HasStep)
            CompleteCurrent();
    }

    public void SkipToNextStep()
    {
        if (!HasStep) return;
        AdvanceCursor();
        ApplyStep(true);
    }

    public void GoToPreviousStep()
    {
        if (_cursor <= 0) return;
        _cursor--;
        ApplyStep(true);
    }

    #endregion

    void CompleteCurrent()
    {
        if (!HasStep || _advancing) return;
        StartCoroutine(CompleteRoutine());
    }

    IEnumerator CompleteRoutine()
    {
        _advancing = true;
        var step = CurrentStep;
        int moduleIndex = _index[_cursor].module;
        _audio.PlayConfirm(step.successSound);
        OnStepCompleted?.Invoke(step.id);
        RefreshHud(false, true, false);

        float delay = Mathf.Max(0.25f, step.delayBeforeNext);
        yield return new WaitForSeconds(delay);

        bool moduleEnded = _cursor + 1 >= _index.Count || _index[_cursor + 1].module != moduleIndex;
        if (moduleEnded)
        {
            TutorialProgressStore.MarkModuleComplete(CurrentModule.id);
            OnModuleCompleted?.Invoke(CurrentModule.id);
        }

        if (_cursor >= _index.Count - 1 || step.completeWhen == TutorialCompleteWhen.EnterFreeMode)
        {
            TutorialProgressStore.Finished = true;
            OnTutorialCompleted?.Invoke();
            EnterFreeMode(true);
            _advancing = false;
            yield break;
        }

        _audio.PlayTransition();
        AdvanceCursor();
        ApplyStep(true);
        _advancing = false;
    }

    void AdvanceCursor()
    {
        if (_cursor < _index.Count - 1)
            _cursor++;
    }

    void SkipMissing()
    {
        int guard = 0;
        while (HasStep && guard++ < 64)
        {
            var step = CurrentStep;
            bool missing = step.skipIfTargetMissing
                           && ((NeedsTarget(step) && ResolveTarget(step.target) == null) || GateToolMissing(step));
            if (!missing)
                break;

            if (_cursor >= _index.Count - 1)
            {
                TutorialProgressStore.Finished = true;
                OnTutorialCompleted?.Invoke();
                EnterFreeMode(true);
                return;
            }

            AdvanceCursor();
        }

        if (HasStep)
            ApplyStep(true);
    }

    void ApplyStep(bool playAudio)
    {
        if (!HasStep) return;

        var step = CurrentStep;
        int moduleIndex = _index[_cursor].module;
        _stepStart = Time.time;
        _lookTimer = 0f;
        _nudgeLevel = 0f;
        _holdingLogged = false;
        _awaitingRelease = false;
        _wrongFlash = false;
        _holdOrigin = CurrentFocus() != null ? CurrentFocus().position : Vector3.zero;

        if (_activeModule != moduleIndex)
            _activeModule = moduleIndex;

        TutorialProgressStore.LastStepId = step.id;
        TutorialProgressStore.LastModuleId = CurrentModule.id;

        ClearOutline();
        Transform focus = ResolveTarget(step.target);
        if (focus != null)
            EnableOutline(focus);
        UpdateFocus(step);
        RefreshHud(false, false, false);

        if (playAudio)
        {
            _audio.PlayAttention(step.instructionSound);
            if (step.narration != null)
                _audio.PlayNarration(step.narration);
        }

        OnStepStarted?.Invoke(step.id);
    }

    void RefreshHud(bool paused, bool success, bool warn)
    {
        if (_hud == null || !HasStep) return;

        var step = CurrentStep;
        var module = CurrentModule;
        int local = _index[_cursor].step + 1;
        int localTotal = module.steps != null ? module.steps.Length : 1;
        string progress = $"{module.title}  ·  {local} / {localTotal}";
        float fill = _index.Count <= 1 ? 1f : _cursor / (float)(_index.Count - 1);

        string instruction = success ? "Perfecto. Siguiente paso." : step.instruction;
        if (!success && !string.IsNullOrEmpty(step.detail) && Time.time - _stepStart < 4f)
            instruction = step.instruction;

        string hint = paused ? string.Empty : CurrentHint(step);
        if (_holdingLogged && !success && IsToolStep(step))
            hint = "Bien: ya la tiene en la mano. Ahora úsela como indica la instrucción.";

        _hud.SetVisible(true);
        _hud.SetCard(
            "ENTRENAMIENTO  ·  TRIGONOCEFALIA ENDOSCÓPICA",
            progress,
            step.title,
            instruction,
            hint,
            fill,
            paused,
            success,
            warn);
    }

    void RefreshFreeHud()
    {
        if (_hud == null) return;
        if (Time.time - _stepStart > 8f)
            _hud.SetVisible(false);
    }

    string CurrentHint(TutorialStepConfig step)
    {
        float elapsed = Time.time - _stepStart;
        int level = 0;
        if (elapsed >= hintDelay3) level = 3;
        else if (elapsed >= hintDelay2) level = 2;
        else if (elapsed >= hintDelay1) level = 1;

        _nudgeLevel = level;
        _marker.SetStrongPulse(level >= 2);

        if (step.hints == null || step.hints.Length == 0)
            return level > 0 ? "Siga la flecha. Haga exactamente lo que indica el texto." : string.Empty;

        if (level <= 0) return string.Empty;
        int i = Mathf.Clamp(level - 1, 0, step.hints.Length - 1);
        return step.hints[i];
    }

    void UpdateFocus(TutorialStepConfig step)
    {
        Transform focus = ResolveTarget(step.target);
        if (focus == null)
        {
            _marker.SetVisible(false);
            return;
        }

        _marker.SetTarget(focus, step.markerLabel);
    }

    #region Completion

    bool IsStepComplete(TutorialStepConfig step)
    {
        Transform focus = ResolveTarget(step.target);
        switch (step.completeWhen)
        {
            case TutorialCompleteWhen.None:
                return false;
            case TutorialCompleteWhen.TimeoutOnly:
                return false;
            case TutorialCompleteWhen.LookedAround:
                return Camera.main != null && Vector3.Angle(_startForward, Camera.main.transform.forward) > 35f;
            case TutorialCompleteWhen.ControllersMoved:
                return AnyControllerMoved();
            case TutorialCompleteWhen.GrabbedAny:
                return AnyGrabSelected();
            case TutorialCompleteWhen.GrabbedTarget:
                return IsHolding(focus);
            case TutorialCompleteWhen.LookedOrGrabbedTarget:
                return IsHolding(focus) || IsLookingLongEnough(focus, 0.85f);
            case TutorialCompleteWhen.MovedHeldTarget:
                return IsHolding(focus) && Vector3.Distance(focus.position, _holdOrigin) > 0.11f;
            case TutorialCompleteWhen.ReleasedTarget:
                return _awaitingRelease && !IsHolding(focus);
            case TutorialCompleteWhen.TriggerWhileHoldingTarget:
                return (focus == null ? AnyGrabSelected() : IsHolding(focus)) && PrimaryPressed();
            case TutorialCompleteWhen.PressedSecondary:
                return SecondaryPressed();
            case TutorialCompleteWhen.LookedAtTarget:
                return IsLookingLongEnough(focus, 1.0f);
            case TutorialCompleteWhen.ShaveComplete:
                return _shave != null && _shave.IsComplete;
            case TutorialCompleteWhen.MarkerPainted:
                return _markerTool != null && _markerTool.HasPainted && (_shave == null || _shave.IsComplete);
            case TutorialCompleteWhen.IncisionComplete:
                return IncisionComplete();
            case TutorialCompleteWhen.RetractorAttached:
                return _retractor != null && _retractor.WasEverAttached;
            case TutorialCompleteWhen.RetractorOpened:
                return _retractor != null && _retractor.currentOpenNormalized > 0.15f;
            case TutorialCompleteWhen.DissectionOnFontanelle:
                return _diseccion != null && (_diseccion.IsOnFontanelle || _diseccion.IsComplete);
            case TutorialCompleteWhen.DissectionComplete:
                return _diseccion != null && _diseccion.IsComplete;
            case TutorialCompleteWhen.DrillSucceeded:
                return _drill != null && _drill.Succeeded;
            case TutorialCompleteWhen.EndoscopeActivated:
                return EndoscopeDepth() > 0.01f || (_endoVr != null && _endoVr.IsActive) || IsHolding(focus);
            case TutorialCompleteWhen.EndoscopeDepthLow:
                return EndoscopeDepth() > 0.08f || (_endoVr != null && _endoVr.IsActive && EndoscopeDepth() > 0.02f);
            case TutorialCompleteWhen.EndoscopeDepthWork:
                return EndoscopeDepth() > 0.35f;
            case TutorialCompleteWhen.KerrisonHolding:
                return _kerrison != null && _kerrison.IsHoldingFragment;
            case TutorialCompleteWhen.KerrisonDeposited:
                return _kerrison != null && !_kerrison.IsHoldingFragment && _kerrison.HasDeposited;
            case TutorialCompleteWhen.CoagulationDone:
                return _coag != null && (_coag.CurrentPaso == CoagulacionOseaHemostasiaVR.Paso.Hemostasia || _coag.IsComplete);
            case TutorialCompleteWhen.HemostasisComplete:
                return (_coag != null && _coag.IsComplete) || (_hemo != null && _hemo.IsActivated);
            case TutorialCompleteWhen.SutureComplete:
                return _suture != null && _suture.IsComplete;
            case TutorialCompleteWhen.PlastyComplete:
                return _plasty != null && _plasty.IsComplete;
            case TutorialCompleteWhen.HemostaticPlaced:
                return _hemo != null && _hemo.IsActivated;
            case TutorialCompleteWhen.EnterFreeMode:
                return false;
            default:
                return false;
        }
    }

    bool GateToolMissing(TutorialStepConfig step)
    {
        switch (step.completeWhen)
        {
            case TutorialCompleteWhen.ShaveComplete: return _shave == null;
            case TutorialCompleteWhen.MarkerPainted: return _markerTool == null;
            case TutorialCompleteWhen.IncisionComplete: return _scalpel == null && _bisturi == null;
            case TutorialCompleteWhen.RetractorAttached:
            case TutorialCompleteWhen.RetractorOpened: return _retractor == null;
            case TutorialCompleteWhen.DissectionOnFontanelle:
            case TutorialCompleteWhen.DissectionComplete: return _diseccion == null;
            case TutorialCompleteWhen.DrillSucceeded: return _drill == null;
            case TutorialCompleteWhen.EndoscopeActivated:
            case TutorialCompleteWhen.EndoscopeDepthLow:
            case TutorialCompleteWhen.EndoscopeDepthWork: return _endoVr == null && _endo == null && EndoscopeUnlock() == null;
            case TutorialCompleteWhen.KerrisonHolding:
            case TutorialCompleteWhen.KerrisonDeposited: return _kerrison == null;
            case TutorialCompleteWhen.CoagulationDone: return _coag == null;
            case TutorialCompleteWhen.HemostasisComplete: return _coag == null && _hemo == null;
            case TutorialCompleteWhen.SutureComplete: return _suture == null;
            case TutorialCompleteWhen.PlastyComplete: return _plasty == null;
            case TutorialCompleteWhen.HemostaticPlaced: return _hemo == null;
            default: return false;
        }
    }

    bool NeedsTarget(TutorialStepConfig step)
    {
        switch (step.completeWhen)
        {
            case TutorialCompleteWhen.GrabbedTarget:
            case TutorialCompleteWhen.LookedOrGrabbedTarget:
            case TutorialCompleteWhen.MovedHeldTarget:
            case TutorialCompleteWhen.ReleasedTarget:
            case TutorialCompleteWhen.LookedAtTarget:
                return true;
            default:
                return false;
        }
    }

    bool IsToolStep(TutorialStepConfig step)
    {
        return step.target != TutorialTargetKind.None
               && step.target != TutorialTargetKind.InstrumentTable
               && step.target != TutorialTargetKind.PatientField
               && step.target != TutorialTargetKind.AnyGrabbable;
    }

    bool IsLookingLongEnough(Transform t, float seconds)
    {
        if (t == null || Camera.main == null)
        {
            _lookTimer = 0f;
            return false;
        }

        Vector3 dir = t.position - Camera.main.transform.position;
        if (dir.sqrMagnitude < 0.0001f)
            return true;

        if (Vector3.Angle(Camera.main.transform.forward, dir) < 28f)
            _lookTimer += Time.deltaTime;
        else
            _lookTimer = 0f;

        return _lookTimer >= seconds;
    }

    #endregion

    #region Targets

    Transform CurrentFocus() => HasStep ? ResolveTarget(CurrentStep.target) : null;

    Transform ResolveTarget(TutorialTargetKind kind)
    {
        switch (kind)
        {
            case TutorialTargetKind.None:
                return null;
            case TutorialTargetKind.InstrumentTable:
                return FindNamed("Mesa de operaciones", "Mesa de operaciones_sup");
            case TutorialTargetKind.PatientField:
                if (_shave != null && _shave.targetRenderer != null) return _shave.targetRenderer.transform;
                if (_markerTool != null && _markerTool.targetRenderer != null) return _markerTool.targetRenderer.transform;
                return FindNamed("Mesa de operaciones");
            case TutorialTargetKind.AnyGrabbable:
                return FirstGrabbable();
            case TutorialTargetKind.Shave:
                return _shave != null ? _shave.transform : FirstGrabbable();
            case TutorialTargetKind.Marker:
                return _markerTool != null ? _markerTool.transform : null;
            case TutorialTargetKind.Scalpel:
                return _scalpel != null ? _scalpel.transform : (_bisturi != null ? _bisturi.transform : null);
            case TutorialTargetKind.ScalpelNextPoint:
                return NextIncisionPoint();
            case TutorialTargetKind.Retractor:
                return _retractor != null ? _retractor.transform : null;
            case TutorialTargetKind.RetractorSnap:
                return RetractorSnap();
            case TutorialTargetKind.Dissection:
                return _diseccion != null ? _diseccion.transform : RetractorSnap();
            case TutorialTargetKind.DissectionHalo:
                if (_diseccion != null && _diseccion.precisionHalo != null) return _diseccion.precisionHalo;
                return _diseccion != null ? _diseccion.transform : null;
            case TutorialTargetKind.Drill:
                return _drill != null ? _drill.transform : null;
            case TutorialTargetKind.DrillSnap:
                if (_drill != null && _drill.snapPoint != null) return _drill.snapPoint;
                return FindNamed("SnapCheckUp", "SnapCheckDown") ?? (_drill != null ? _drill.transform : null);
            case TutorialTargetKind.Endoscope:
                if (_endo != null) return _endo.transform;
                if (_endoVr != null) return _endoVr.transform;
                return FindNamed("Endoscopio Handler", "Endoscopio Unlock");
            case TutorialTargetKind.EndoscopeScreen:
                if (_endoVr != null && _endoVr.endoscopeScreen != null) return _endoVr.endoscopeScreen.transform;
                return FindNamed("EndoscopioScreen", "Endoscopio_UI") ?? EndoscopeUnlock();
            case TutorialTargetKind.EndoscopeUnlock:
                return EndoscopeUnlock() ?? (_endo != null ? _endo.transform : (_endoVr != null ? _endoVr.transform : null));
            case TutorialTargetKind.Kerrison:
                return _kerrison != null ? _kerrison.transform : null;
            case TutorialTargetKind.Coagulator:
                return _coag != null ? _coag.transform : FindNamed("Coagulador");
            case TutorialTargetKind.CoagPath:
                return CoagPathFocus();
            case TutorialTargetKind.Hemostatic:
                return _hemo != null ? _hemo.transform : FindNamed("Hemostasico");
            case TutorialTargetKind.Suture:
                return _suture != null ? _suture.transform : null;
            case TutorialTargetKind.SuturePoint:
                return _suture != null ? _suture.NextSuturePoint : FindNamed("Sutura Helper ");
            case TutorialTargetKind.SutureHelper:
                return FindNamed("Sutura Helper ", "Sutura Helper");
            case TutorialTargetKind.Plasty:
                return _plasty != null ? _plasty.transform : null;
            case TutorialTargetKind.PlastyPath:
                if (_plasty != null && _plasty.pathPoints != null && _plasty.pathPoints.Length > 0 && _plasty.pathPoints[0] != null)
                    return _plasty.pathPoints[0];
                return _plasty != null ? _plasty.transform : null;
            case TutorialTargetKind.ClearCol:
                return FindNamed("ClearCol") ?? (_kerrison != null ? _kerrison.transform : null);
            default:
                return null;
        }
    }

    Transform NextIncisionPoint()
    {
        if (_scalpel != null && _scalpel.pathPoints != null && _scalpel.pathPoints.Length > 0)
        {
            int i = Mathf.Clamp(_scalpel.NextPointIndex, 0, _scalpel.pathPoints.Length - 1);
            if (_scalpel.pathPoints[i] != null)
                return _scalpel.pathPoints[i];
        }

        if (_bisturi != null)
            return _bisturi.NextCutPoint;

        return _scalpel != null ? _scalpel.transform : null;
    }

    bool IncisionComplete()
    {
        if (_scalpel != null && _scalpel.IsCompleted) return true;
        if (_bisturi != null && _bisturi.AllCutsDone) return true;
        return false;
    }

    Transform RetractorSnap()
    {
        if (_retractor != null)
        {
            if (_retractor.snapFrontal != null) return _retractor.snapFrontal;
            if (_retractor.snapTrasera != null) return _retractor.snapTrasera;
            return _retractor.transform;
        }

        return FindNamed("Retractor_1_CheckUP", "SnapCheckUp", "Retractor1_Check");
    }

    Transform EndoscopeUnlock() => FindNamed("Endoscopio Unlock");

    Transform CoagPathFocus()
    {
        if (_coag != null)
        {
            Transform p = _coag.CurrentPathFocus;
            if (p != null) return p;
            return _coag.transform;
        }

        if (_hemo != null)
            return _hemo.transform;

        return FindNamed("Coagulador Tip", "Coagulador", "Hemostasico");
    }

    float EndoscopeDepth()
    {
        if (_endoVr != null && _endoVr.IsActive) return _endoVr.Depth01;
        if (_endo != null) return _endo.Depth01;
        return 0f;
    }

    static Transform FindNamed(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            var go = GameObject.Find(names[i]);
            if (go != null) return go.transform;
        }
        return null;
    }

    #endregion

    #region Input / grab

    void WatchHold(TutorialStepConfig step)
    {
        Transform focus = ResolveTarget(step.target);
        bool holding = IsHolding(focus);
        if (holding && !_holdingLogged)
        {
            _holdingLogged = true;
            _holdOrigin = focus != null ? focus.position : _holdOrigin;
            _audio.PlaySelect();
        }

        if (step.completeWhen == TutorialCompleteWhen.ReleasedTarget)
        {
            if (holding)
                _awaitingRelease = true;
        }
    }

    void WatchWrongTool(TutorialStepConfig step)
    {
        if (!IsToolStep(step))
            return;
        if (step.completeWhen == TutorialCompleteWhen.GrabbedAny
            || step.completeWhen == TutorialCompleteWhen.LookedAtTarget
            || step.completeWhen == TutorialCompleteWhen.LookedAround
            || step.completeWhen == TutorialCompleteWhen.ControllersMoved
            || step.completeWhen == TutorialCompleteWhen.PressedSecondary
            || step.completeWhen == TutorialCompleteWhen.TimeoutOnly)
            return;
        if (Time.time - _lastErrorTime < 2.4f)
            return;

        Transform focus = ResolveTarget(step.target);
        var focusGrab = GrabOf(focus);
        var grabs = FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < grabs.Length; i++)
        {
            var g = grabs[i];
            if (g == null || !g.isSelected) continue;
            if (focusGrab != null && g == focusGrab) continue;
            if (focus != null && g.transform.IsChildOf(focus)) continue;

            _lastErrorTime = Time.time;
            _wrongFlash = true;
            _audio.PlayError(step.errorSound);
            OnWrongAction?.Invoke();
            string toolName = string.IsNullOrEmpty(step.markerLabel) ? "la herramienta indicada" : step.markerLabel.ToLowerInvariant();
            if (_hud != null)
            {
                _hud.SetCard(
                    "ENTRENAMIENTO  ·  TRIGONOCEFALIA ENDOSCÓPICA",
                    CurrentModule.title,
                    step.title,
                    "Esa no es la herramienta de este paso.",
                    "Use " + toolName + ".",
                    _index.Count <= 1 ? 1f : _cursor / (float)(_index.Count - 1),
                    false, false, true);
            }
            if (focus != null)
                EnableOutline(focus);
            return;
        }
    }

    Transform FirstGrabbable()
    {
        var grabs = FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        return grabs.Length > 0 ? grabs[0].transform : null;
    }

    bool AnyGrabSelected()
    {
        var grabs = FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < grabs.Length; i++)
        {
            if (grabs[i] != null && grabs[i].isSelected)
                return true;
        }
        return false;
    }

    bool AnyControllerMoved()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand, devices);
        foreach (var d in devices)
        {
            if (d.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 vel) && vel.sqrMagnitude > 0.05f)
                return true;
        }
        return false;
    }

    bool PrimaryPressed()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
        foreach (var d in devices)
        {
            if (d.TryGetFeatureValue(CommonUsages.triggerButton, out bool t) && t) return true;
            if (d.TryGetFeatureValue(CommonUsages.trigger, out float tf) && tf > 0.6f) return true;
        }
        return Input.GetMouseButton(0);
    }

    bool SecondaryPressed()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
        foreach (var d in devices)
        {
            if (d.TryGetFeatureValue(CommonUsages.secondaryButton, out bool b) && b) return true;
        }
        return false;
    }

    bool IsHolding(Transform target)
    {
        var grab = GrabOf(target);
        return grab != null && grab.isSelected;
    }

    static XRGrabInteractable GrabOf(Transform target)
    {
        if (target == null) return null;
        var grab = target.GetComponentInParent<XRGrabInteractable>();
        if (grab == null) grab = target.GetComponentInChildren<XRGrabInteractable>();
        return grab;
    }

    #endregion

    #region Outline

    void EnableOutline(Transform t)
    {
        if (t == null) return;
        var outline = t.GetComponent<Outline>();
        if (outline == null) outline = t.GetComponentInChildren<Outline>();
        if (outline == null)
        {
            var rend = t.GetComponentInChildren<Renderer>();
            if (rend == null) return;
            outline = rend.gameObject.AddComponent<Outline>();
            _ownedOutlines.Add(outline);
        }

        outline.enabled = true;
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = new Color(0.55f, 0.82f, 0.84f, 1f);
        outline.OutlineWidth = 6f;
        _activeOutline = outline;
    }

    void ClearOutline()
    {
        if (_activeOutline != null)
        {
            _activeOutline.OutlineWidth = 2f;
            if (_ownedOutlines.Contains(_activeOutline))
                _activeOutline.enabled = false;
        }
        _activeOutline = null;
    }

    void PulseOutline()
    {
        if (_activeOutline == null) return;
        float amp = _nudgeLevel >= 2f ? 3.2f : 2.2f;
        _activeOutline.OutlineWidth = 4.5f + Mathf.Sin(Time.time * 3.4f) * amp;
    }

    #endregion

    #region Setup

    void CacheTools()
    {
        _shave = FindFirstObjectByType<ShaveVRTool>(FindObjectsInactive.Include);
        _markerTool = FindFirstObjectByType<MarkerVRTool>(FindObjectsInactive.Include);
        _scalpel = FindFirstObjectByType<ScalpelVRTool>(FindObjectsInactive.Include);
        _bisturi = FindFirstObjectByType<BisturiCutControl>(FindObjectsInactive.Include);
        _retractor = FindFirstObjectByType<RetractorVRTool>(FindObjectsInactive.Include);
        _diseccion = FindFirstObjectByType<DiseccionSubcutaneaFontanelaVR>(FindObjectsInactive.Include);
        _drill = FindFirstObjectByType<DrillVRTool>(FindObjectsInactive.Include);
        _endoVr = FindFirstObjectByType<EndoscopeVRTool>(FindObjectsInactive.Include);
        _endo = FindFirstObjectByType<Endoscopio>(FindObjectsInactive.Include);
        _kerrison = FindFirstObjectByType<Kerrison>(FindObjectsInactive.Include);
        _coag = FindFirstObjectByType<CoagulacionOseaHemostasiaVR>(FindObjectsInactive.Include);
        _suture = FindFirstObjectByType<FinSuturectomiaVR>(FindObjectsInactive.Include);
        _plasty = FindFirstObjectByType<PlasticaCutaneaVR>(FindObjectsInactive.Include);
        _hemo = FindFirstObjectByType<Hemostasico>(FindObjectsInactive.Include);
        _procedure = FindFirstObjectByType<SurgicalProcedureManager>(FindObjectsInactive.Include);
    }

    void SilenceLocalBeacons()
    {
        foreach (var beacon in FindObjectsByType<SurgicalGuideBeacon>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (beacon == null) continue;
            beacon.SetVisible(false);
        }
    }

    TutorialModuleConfig[] BuildModules()
    {
        TutorialModuleAsset[] source = catalog != null ? catalog.modules : modulesOverride;
        if (source == null || source.Length == 0)
        {
            var loaded = Resources.Load<TutorialCatalogAsset>("TutorialCatalog");
            if (loaded != null)
                source = loaded.modules;
        }

        if (source != null && source.Length > 0)
        {
            var list = new List<TutorialModuleConfig>();
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == null) continue;
                list.Add(source[i].ToConfig());
            }
            if (list.Count > 0)
                return list.ToArray();
        }

        return TutorialCurriculum.Build();
    }

    void Flatten()
    {
        _index.Clear();
        if (_modules == null) return;
        for (int m = 0; m < _modules.Length; m++)
        {
            var steps = _modules[m].steps;
            if (steps == null) continue;
            for (int s = 0; s < steps.Length; s++)
                _index.Add((m, s));
        }
    }

    void RestoreProgress()
    {
        if (TutorialProgressStore.IsModuleComplete("anatomy")
            || TutorialProgressStore.IsModuleComplete("tools")
            || TutorialProgressStore.LastModuleId == "procedure"
            || TutorialProgressStore.LastModuleId == "finish")
        {
            JumpToModule("procedure");
            return;
        }

        string id = TutorialProgressStore.LastStepId;
        if (string.IsNullOrEmpty(id)) return;
        for (int i = 0; i < _index.Count; i++)
        {
            var step = _modules[_index[i].module].steps[_index[i].step];
            if (step != null && step.id == id)
            {
                _cursor = i;
                return;
            }
        }
    }

    void JumpToModule(string moduleId)
    {
        for (int i = 0; i < _index.Count; i++)
        {
            if (_modules[_index[i].module].id == moduleId)
            {
                _cursor = i;
                return;
            }
        }
    }

    void HandlePauseInput()
    {
        if (!pauseWithMenuButton || _mode == TutorialPlayMode.Free) return;

        bool menuHeld = false;
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
        foreach (var d in devices)
        {
            if (d.TryGetFeatureValue(CommonUsages.menuButton, out bool m) && m)
                menuHeld = true;
        }

        if ((menuHeld && !_menuWasPressed) || Input.GetKeyDown(KeyCode.P))
            TogglePause();

        _menuWasPressed = menuHeld;
    }

    void HandleDebugInput()
    {
        if (!DebugOn) return;
        if (Input.GetKeyDown(KeyCode.F8)) ForceCompleteStep();
        if (Input.GetKeyDown(KeyCode.F9)) SkipToNextStep();
        if (Input.GetKeyDown(KeyCode.F7)) GoToPreviousStep();
        if (Input.GetKeyDown(KeyCode.F10)) RestartTutorial();
        if (Input.GetKeyDown(KeyCode.F6)) EnterFreeMode(false);
        if (Input.GetKeyDown(KeyCode.F5)) TogglePause();
        if (Input.GetKeyDown(KeyCode.F4)) RestartModule();
    }

    void OnGUI()
    {
        if (!DebugOn || !HasStep) return;
        const int w = 420;
        GUI.Box(new Rect(12, 12, w, 150), "Tutorial debug");
        GUI.Label(new Rect(24, 36, w - 24, 22), "Paso: " + CurrentStep.id);
        GUI.Label(new Rect(24, 56, w - 24, 22), "Módulo: " + CurrentModule.id + "  ·  " + CurrentStep.title);
        GUI.Label(new Rect(24, 76, w - 24, 22), "Objetivo: " + CurrentTargetName);
        GUI.Label(new Rect(24, 96, w - 24, 22), "Condición: " + CurrentCompletion + "  ·  " + _mode + (_procedure != null ? "  ·  proc" : ""));
        GUI.Label(new Rect(24, 116, w - 24, 22), "F8 completar  F9 siguiente  F7 atrás  F5 pausa  F6 libre  F10 reiniciar");
    }

    [ContextMenu("Debug/Completar paso")]
    void CtxComplete() => ForceCompleteStep();

    [ContextMenu("Debug/Siguiente paso")]
    void CtxNext() => SkipToNextStep();

    [ContextMenu("Debug/Reiniciar tutorial")]
    void CtxRestart() => RestartTutorial();

    [ContextMenu("Debug/Modo libre")]
    void CtxFree() => EnterFreeMode(false);

    #endregion
}
