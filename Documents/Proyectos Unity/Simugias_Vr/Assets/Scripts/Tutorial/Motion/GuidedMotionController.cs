using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Orquesta OBSERVÁ → Tomar → Ejecutar → Completado para un movimiento guiado.
/// No ejecuta la acción quirúrgica: solo enseña y valida la trayectoria.
/// </summary>
[DefaultExecutionOrder(150)]
public class GuidedMotionController : MonoBehaviour
{
    public enum Phase
    {
        Idle,
        Observe,
        Take,
        Execute,
        Correcting,
        TrackingLost,
        Completed
    }

    [Header("Referencias")]
    [SerializeField] Transform pathAnchor;
    [SerializeField] Transform[] pathPoints;
    [SerializeField] Transform tipOverride;
    [SerializeField] Transform toolRoot;

    [Header("Tolerancias")]
    [SerializeField] MotionTolerancePreset tolerances = MotionTolerancePreset.Beginner;
    [SerializeField] bool useBeginnerPreset = true;

    [Header("Demo")]
    [SerializeField] bool playObserveDemo = true;
    [SerializeField] float demoSpeed = 0.2f;
    [SerializeField] float observeMinSeconds = 2.2f;
    [SerializeField] bool allowSkipObserveWhenGrabbed = true;

    [Header("Asistencia")]
    [SerializeField] MotionAssistLevel assistLevel = MotionAssistLevel.Guided;
    [SerializeField] bool autoLowerAssist = true;
    [SerializeField] int errorsBeforeFullReplay = 2;
    [SerializeField] int errorsBeforeExtraHelp = 3;

    [Header("Filtro tip")]
    [SerializeField] float oneEuroMinCutoff = 1.0f;
    [SerializeField] float oneEuroBeta = 0.01f;

    [Header("Eventos")]
    public UnityEvent OnObserveStarted;
    public UnityEvent OnExecuteStarted;
    public UnityEvent OnCompleted;
    public UnityEvent<string> OnPhaseMessage;
    public UnityEvent OnOffTrack;

    readonly MotionValidationCore _core = new MotionValidationCore();
    readonly OneEuroFilter3 _filter = new OneEuroFilter3();
    GhostToolDemo _ghost;
    Phase _phase = Phase.Idle;
    Vector3 _lastTip;
    bool _hasLastTip;
    float _phaseTimer;
    int _errorCount;
    int _attempt;
    string _message = string.Empty;
    bool _active;
    bool _sawHandsThisSession;

    public Phase CurrentPhase => _phase;
    public bool IsComplete => _phase == Phase.Completed;
    public bool IsActive => _active;
    public float Progress => _core.Progress;
    public MotionTrackState TrackState => _core.State;
    public string Message => _message;
    public int ErrorCount => _errorCount;
    public MotionAssistLevel AssistLevel => assistLevel;

    public void Configure(
        Transform tool,
        Transform tip,
        Transform[] points,
        Transform anchor,
        MotionTolerancePreset? tol = null,
        MotionAssistLevel level = MotionAssistLevel.Guided,
        float? demoSpeedOverride = null)
    {
        toolRoot = tool;
        tipOverride = tip;
        pathPoints = points;
        pathAnchor = anchor;
        assistLevel = level;
        if (demoSpeedOverride.HasValue)
            demoSpeed = Mathf.Max(0.05f, demoSpeedOverride.Value);
        if (tol.HasValue)
        {
            tolerances = tol.Value;
            useBeginnerPreset = false;
        }
        else if (useBeginnerPreset)
        {
            tolerances = MotionTolerancePreset.Beginner;
        }
    }

    public void Begin()
    {
        EnsureGhost();
        RebuildPath();
        _filter.Configure(oneEuroMinCutoff, oneEuroBeta);
        _errorCount = 0;
        _attempt = 0;
        _active = true;
        _hasLastTip = false;
        _ghost.ClearTrail();
        _ghost.BindSourceMesh(toolRoot);
        _ghost.SetPath(BuildWorldPoints());
        _ghost.SetAssistVisuals(assistLevel);

        if (playObserveDemo && assistLevel == MotionAssistLevel.Guided)
            EnterObserve();
        else
            EnterTake();
    }

    public void Abort()
    {
        _active = false;
        _phase = Phase.Idle;
        if (_ghost != null)
        {
            _ghost.StopDemo();
            _ghost.SetVisible(false);
            _ghost.ClearTrail();
        }
    }

    public void ReplayDemoSlower()
    {
        demoSpeed = Mathf.Max(0.08f, demoSpeed * 0.7f);
        EnterObserve();
    }

    public void ReplayDemo()
    {
        EnterObserve();
    }

    void Update()
    {
        if (!_active || _phase == Phase.Completed)
            return;

        float dt = Time.deltaTime;
        _phaseTimer += dt;

        if (_ghost != null && _phase == Phase.Observe)
            _ghost.TickDemo(dt, _core.GetPointAtProgress, _core.GetRotationAtProgress);

        bool holding = IsHoldingTool();
        bool trackingOk = EvaluateTracking();

        if (!trackingOk && holding)
        {
            if (_phase != Phase.TrackingLost)
            {
                _phase = Phase.TrackingLost;
                SetMessage("Mostrá las manos otra vez");
            }
            return;
        }

        Vector3 tip = GetTipPosition();
        Quaternion tipRot = GetTipRotation();
        if (_hasLastTip && dt > 0f)
            _core.SetSpeed(Vector3.Distance(tip, _lastTip) / dt);
        _lastTip = tip;
        _hasLastTip = true;

        switch (_phase)
        {
            case Phase.Observe:
                TickObserve(holding);
                break;
            case Phase.Take:
                if (holding)
                    EnterExecute();
                break;
            case Phase.Execute:
            case Phase.Correcting:
                TickExecute(tip, tipRot, dt, holding);
                break;
            case Phase.TrackingLost:
                if (trackingOk)
                    EnterTake();
                break;
        }
    }

    void TickObserve(bool holding)
    {
        if (allowSkipObserveWhenGrabbed && holding && _phaseTimer > 0.6f)
        {
            EnterExecute();
            return;
        }

        if (_phaseTimer >= observeMinSeconds && (_ghost == null || !_ghost.IsPlaying || _ghost.DemoProgress > 0.98f))
        {
            // Loop demo until user grabs; message stays
            if (holding)
                EnterExecute();
            else
                SetMessage("Ahora hacelo vos — tomá la herramienta");
        }
    }

    void TickExecute(Vector3 tip, Quaternion tipRot, float dt, bool holding)
    {
        if (!holding)
        {
            EnterTake();
            return;
        }

        var state = _core.Tick(tip, tipRot, dt, true);
        bool good = state == MotionTrackState.OnTrack
                    || state == MotionTrackState.OnStartZone
                    || state == MotionTrackState.Completed;
        if (_ghost != null && assistLevel != MotionAssistLevel.Free)
            _ghost.PushTrailPoint(tip, good);

        switch (state)
        {
            case MotionTrackState.Idle:
                SetMessage("Llevalo al punto de inicio");
                break;
            case MotionTrackState.OnStartZone:
                SetMessage("Bien — empezá el recorrido");
                break;
            case MotionTrackState.OnTrack:
                SetMessage("Seguí la línea");
                _phase = Phase.Execute;
                break;
            case MotionTrackState.Warning:
                SetMessage("Corregí un poco — volvé a la guía");
                if (_ghost != null && assistLevel != MotionAssistLevel.Minimal)
                    _ghost.ShowCorrectionGhost(_core.GetPointAtProgress(_core.Progress), _core.GetRotationAtProgress(_core.Progress));
                break;
            case MotionTrackState.OffTrack:
                HandleOffTrack();
                break;
            case MotionTrackState.Completed:
                Complete();
                break;
        }
    }

    void HandleOffTrack()
    {
        if (_phase == Phase.Correcting) return;
        _phase = Phase.Correcting;
        _errorCount++;
        OnOffTrack?.Invoke();
        SetMessage("Desvío — mirá la demo otra vez");

        if (_errorCount >= errorsBeforeExtraHelp)
        {
            assistLevel = MotionAssistLevel.Guided;
            _ghost?.SetAssistVisuals(assistLevel);
            SetMessage("Te ayudo más — observá y repetí");
            EnterObserve();
            return;
        }

        if (_errorCount >= errorsBeforeFullReplay)
        {
            EnterObserve();
            return;
        }

        if (_ghost != null)
            _ghost.ShowCorrectionGhost(_core.GetPointAtProgress(_core.Progress), _core.GetRotationAtProgress(_core.Progress));
    }

    void EnterObserve()
    {
        _phase = Phase.Observe;
        _phaseTimer = 0f;
        _core.Reset();
        RebuildPath();
        SetMessage("Observá cómo se hace");
        OnObserveStarted?.Invoke();
        OnPhaseMessage?.Invoke(_message);
        if (_ghost != null)
        {
            _ghost.ClearTrail();
            _ghost.SetPath(BuildWorldPoints());
            _ghost.SetAssistVisuals(MotionAssistLevel.Guided);
            _ghost.PlayDemo(demoSpeed, true);
        }
    }

    void EnterTake()
    {
        _phase = Phase.Take;
        _phaseTimer = 0f;
        _core.Reset();
        _ghost?.StopDemo();
        _ghost?.SetVisible(true);
        SetMessage("Tomá la herramienta resaltada");
        OnPhaseMessage?.Invoke(_message);
    }

    void EnterExecute()
    {
        _phase = Phase.Execute;
        _phaseTimer = 0f;
        _attempt++;
        RebuildPath();
        _core.Reset();
        if (_ghost != null)
        {
            _ghost.StopDemo();
            _ghost.SetPath(BuildWorldPoints());
            _ghost.SetAssistVisuals(assistLevel);
            _ghost.SetVisible(true);
            _ghost.ClearTrail();
        }
        SetMessage("Ahora hacelo vos");
        OnExecuteStarted?.Invoke();
        OnPhaseMessage?.Invoke(_message);

        if (autoLowerAssist && _attempt >= 2 && _errorCount == 0 && assistLevel == MotionAssistLevel.Guided)
        {
            assistLevel = MotionAssistLevel.Assisted;
            _ghost?.SetAssistVisuals(assistLevel);
        }
    }

    void Complete()
    {
        _phase = Phase.Completed;
        _active = false;
        SetMessage("¡Bien hecho!");
        _ghost?.StopDemo();
        OnCompleted?.Invoke();
        OnPhaseMessage?.Invoke(_message);
    }

    void RebuildPath()
    {
        var pts = BuildWorldPoints();
        _core.Configure(pts, tolerances);
    }

    Vector3[] BuildWorldPoints()
    {
        if (pathPoints != null && pathPoints.Length >= 2)
        {
            var list = new Vector3[pathPoints.Length];
            int n = 0;
            for (int i = 0; i < pathPoints.Length; i++)
            {
                if (pathPoints[i] == null) continue;
                list[n++] = pathPoints[i].position;
            }
            if (n >= 2)
            {
                if (n != list.Length)
                    Array.Resize(ref list, n);
                return list;
            }
        }

        // Fallback: trayectoria trivial frente a la herramienta / cámara
        Vector3 origin = toolRoot != null ? toolRoot.position : transform.position;
        Camera cam = Camera.main;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        Vector3 fwd = cam != null ? cam.transform.forward : Vector3.forward;
        origin += Vector3.up * 0.05f + fwd * 0.12f;
        return new[]
        {
            origin,
            origin + right * 0.08f + Vector3.up * 0.02f,
            origin + right * 0.16f
        };
    }

    Vector3 GetTipPosition()
    {
        Vector3 raw;
        if (tipOverride != null) raw = tipOverride.position;
        else if (toolRoot != null) raw = toolRoot.position;
        else raw = transform.position;
        return _filter.Filter(raw, Time.deltaTime);
    }

    Quaternion GetTipRotation()
    {
        if (tipOverride != null) return tipOverride.rotation;
        if (toolRoot != null) return toolRoot.rotation;
        return transform.rotation;
    }

    bool IsHoldingTool()
    {
        if (toolRoot == null) return false;
        var grab = toolRoot.GetComponentInParent<XRGrabInteractable>();
        if (grab == null) grab = toolRoot.GetComponent<XRGrabInteractable>();
        if (grab == null) return false;
        return grab.isSelected;
    }

    bool EvaluateTracking()
    {
        var hands = FindFirstObjectByType<HandPinchToolInput>();
        if (hands == null)
            return true;

        if (hands.AnyHandTracked)
        {
            _sawHandsThisSession = true;
            return true;
        }

        // Controllers: si nunca vimos manos, no bloquear.
        if (!_sawHandsThisSession)
            return true;

        return false;
    }

    void EnsureGhost()
    {
        if (_ghost != null) return;
        var go = new GameObject("GhostMotionDemo");
        go.transform.SetParent(transform, false);
        _ghost = go.AddComponent<GhostToolDemo>();
        _ghost.EnsureBuilt();
    }

    void SetMessage(string msg)
    {
        if (_message == msg) return;
        _message = msg;
        OnPhaseMessage?.Invoke(msg);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (pathPoints == null) return;
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (pathPoints[i] == null) continue;
            Gizmos.DrawWireSphere(pathPoints[i].position, tolerances.corridorRadius);
            if (i + 1 < pathPoints.Length && pathPoints[i + 1] != null)
                Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
        }
    }
#endif
}
