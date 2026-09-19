using System;
using UnityEngine;

public enum MotionTrackState
{
    Idle,
    OnStartZone,
    OnTrack,
    Warning,
    OffTrack,
    Completed
}

public enum MotionAssistLevel
{
    Guided,
    Assisted,
    Minimal,
    Free
}

[Serializable]
public struct MotionTolerancePreset
{
    [Min(0.005f)] public float corridorRadius;
    [Min(0.005f)] public float startZoneRadius;
    [Min(0.005f)] public float endZoneRadius;
    [Range(1f, 90f)] public float maxOrientationDegrees;
    [Min(0.01f)] public float minSpeed;
    [Min(0.05f)] public float maxSpeed;
    [Min(0.05f)] public float endHoldSeconds;
    [Min(0f)] public float warningPersistSeconds;
    [Min(0f)] public float offTrackPersistSeconds;

    public static MotionTolerancePreset Beginner => new MotionTolerancePreset
    {
        corridorRadius = 0.045f,
        startZoneRadius = 0.06f,
        endZoneRadius = 0.05f,
        maxOrientationDegrees = 35f,
        minSpeed = 0.01f,
        maxSpeed = 0.55f,
        endHoldSeconds = 0.35f,
        warningPersistSeconds = 0.35f,
        offTrackPersistSeconds = 0.7f
    };

    public static MotionTolerancePreset Standard => new MotionTolerancePreset
    {
        corridorRadius = 0.03f,
        startZoneRadius = 0.045f,
        endZoneRadius = 0.035f,
        maxOrientationDegrees = 25f,
        minSpeed = 0.015f,
        maxSpeed = 0.45f,
        endHoldSeconds = 0.45f,
        warningPersistSeconds = 0.25f,
        offTrackPersistSeconds = 0.55f
    };

    public static MotionTolerancePreset Expert => new MotionTolerancePreset
    {
        corridorRadius = 0.018f,
        startZoneRadius = 0.03f,
        endZoneRadius = 0.022f,
        maxOrientationDegrees = 15f,
        minSpeed = 0.02f,
        maxSpeed = 0.35f,
        endHoldSeconds = 0.55f,
        warningPersistSeconds = 0.2f,
        offTrackPersistSeconds = 0.4f
    };
}

/// <summary>
/// Lógica pura de validación de trayectoria. Testeable sin MonoBehaviour.
/// Espacio: coordenadas world (el caller convierte desde ancla local).
/// </summary>
public sealed class MotionValidationCore
{
    Vector3[] _points;
    float[] _cumLen;
    float _totalLen;
    MotionTolerancePreset _tol;
    MotionTrackState _state = MotionTrackState.Idle;
    float _progress;
    float _lateral;
    float _orientError;
    float _speed;
    float _endHold;
    float _badTimer;
    float _warnTimer;
    bool _started;

    public MotionTrackState State => _state;
    public float Progress => _progress;
    public float LateralDeviation => _lateral;
    public float OrientationErrorDegrees => _orientError;
    public float Speed => _speed;
    public bool HasPath => _points != null && _points.Length >= 2 && _totalLen > 0.0001f;

    public void Configure(Vector3[] worldPoints, MotionTolerancePreset tol)
    {
        _tol = tol;
        _points = worldPoints;
        RebuildLengths();
        Reset();
    }

    public void Reset()
    {
        _state = MotionTrackState.Idle;
        _progress = 0f;
        _lateral = 0f;
        _orientError = 0f;
        _speed = 0f;
        _endHold = 0f;
        _badTimer = 0f;
        _warnTimer = 0f;
        _started = false;
    }

    public MotionTrackState Tick(Vector3 tipPos, Quaternion tipRot, float dt, bool isHolding)
    {
        if (!HasPath)
        {
            _state = MotionTrackState.Idle;
            return _state;
        }

        dt = Mathf.Max(dt, 0f);
        SampleClosest(tipPos, out float s, out float lateral, out Vector3 tangent);
        _progress = Mathf.Clamp01(s / _totalLen);
        _lateral = lateral;

        Vector3 tipFwd = tipRot * Vector3.forward;
        if (tipFwd.sqrMagnitude < 0.0001f)
            tipFwd = tipRot * Vector3.up;
        _orientError = Vector3.Angle(tipFwd.normalized, tangent.normalized);

        if (!isHolding)
        {
            _started = false;
            _endHold = 0f;
            _state = MotionTrackState.Idle;
            return _state;
        }

        float distStart = Vector3.Distance(tipPos, _points[0]);
        float distEnd = Vector3.Distance(tipPos, _points[_points.Length - 1]);

        if (!_started)
        {
            if (distStart <= _tol.startZoneRadius)
            {
                _state = MotionTrackState.OnStartZone;
                if (_progress < 0.08f && lateral <= _tol.corridorRadius * 1.15f)
                    _started = true;
            }
            else
            {
                _state = MotionTrackState.Idle;
            }
            return _state;
        }

        bool inCorridor = lateral <= _tol.corridorRadius;
        bool orientOk = _orientError <= _tol.maxOrientationDegrees;
        bool nearEnd = distEnd <= _tol.endZoneRadius || _progress >= 0.92f;

        if (nearEnd && inCorridor)
        {
            _endHold += dt;
            if (_endHold >= _tol.endHoldSeconds)
            {
                _state = MotionTrackState.Completed;
                _progress = 1f;
                return _state;
            }
            _state = MotionTrackState.OnTrack;
            return _state;
        }

        _endHold = 0f;

        if (!inCorridor || !orientOk)
        {
            _badTimer += dt;
            _warnTimer += dt;
            if (_badTimer >= _tol.offTrackPersistSeconds)
                _state = MotionTrackState.OffTrack;
            else if (_warnTimer >= _tol.warningPersistSeconds)
                _state = MotionTrackState.Warning;
            else
                _state = MotionTrackState.OnTrack;
        }
        else
        {
            _badTimer = Mathf.Max(0f, _badTimer - dt * 1.5f);
            _warnTimer = Mathf.Max(0f, _warnTimer - dt * 1.5f);
            _state = MotionTrackState.OnTrack;
        }

        // No se puede completar yendo hacia atrás: el progreso solo avanza.
        return _state;
    }

    public void SetSpeed(float metersPerSecond) => _speed = Mathf.Max(0f, metersPerSecond);

    public Vector3 GetPointAtProgress(float t)
    {
        if (!HasPath) return Vector3.zero;
        t = Mathf.Clamp01(t);
        float target = t * _totalLen;
        for (int i = 0; i < _cumLen.Length - 1; i++)
        {
            if (target <= _cumLen[i + 1])
            {
                float seg = _cumLen[i + 1] - _cumLen[i];
                float u = seg > 0.0001f ? (target - _cumLen[i]) / seg : 0f;
                return Vector3.Lerp(_points[i], _points[i + 1], u);
            }
        }
        return _points[_points.Length - 1];
    }

    public Quaternion GetRotationAtProgress(float t)
    {
        if (!HasPath) return Quaternion.identity;
        t = Mathf.Clamp01(t);
        float target = t * _totalLen;
        for (int i = 0; i < _cumLen.Length - 1; i++)
        {
            if (target <= _cumLen[i + 1])
            {
                Vector3 dir = _points[i + 1] - _points[i];
                if (dir.sqrMagnitude < 0.000001f)
                    continue;
                return Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }
        Vector3 last = _points[_points.Length - 1] - _points[_points.Length - 2];
        if (last.sqrMagnitude < 0.000001f)
            return Quaternion.identity;
        return Quaternion.LookRotation(last.normalized, Vector3.up);
    }

    void RebuildLengths()
    {
        _totalLen = 0f;
        if (_points == null || _points.Length < 2)
        {
            _cumLen = Array.Empty<float>();
            return;
        }

        _cumLen = new float[_points.Length];
        _cumLen[0] = 0f;
        for (int i = 1; i < _points.Length; i++)
        {
            _totalLen += Vector3.Distance(_points[i - 1], _points[i]);
            _cumLen[i] = _totalLen;
        }
    }

    void SampleClosest(Vector3 pos, out float s, out float lateral, out Vector3 tangent)
    {
        s = 0f;
        lateral = float.MaxValue;
        tangent = Vector3.forward;
        float bestS = 0f;

        for (int i = 0; i < _points.Length - 1; i++)
        {
            Vector3 a = _points[i];
            Vector3 b = _points[i + 1];
            Vector3 ab = b - a;
            float abLenSq = ab.sqrMagnitude;
            float u = abLenSq > 0.0000001f ? Mathf.Clamp01(Vector3.Dot(pos - a, ab) / abLenSq) : 0f;
            Vector3 closest = a + ab * u;
            float d = Vector3.Distance(pos, closest);
            if (d < lateral)
            {
                lateral = d;
                bestS = _cumLen[i] + Mathf.Sqrt(abLenSq) * u;
                tangent = abLenSq > 0.0000001f ? ab.normalized : tangent;
            }
        }

        // Progreso monotónico: no permitir retroceso fuerte.
        s = Mathf.Max(_progress * _totalLen, bestS);
        if (bestS + 0.04f < _progress * _totalLen)
            s = _progress * _totalLen;
    }
}
