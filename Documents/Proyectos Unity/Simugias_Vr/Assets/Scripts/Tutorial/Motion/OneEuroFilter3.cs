using UnityEngine;

/// <summary>
/// One Euro Filter para Vector3. Misma idea que el sample de XRI Hands,
/// sin depender del asmdef de samples.
/// </summary>
public sealed class OneEuroFilter3
{
    Vector3 _lastRaw;
    Vector3 _lastFiltered;
    float _minCutoff;
    float _beta;
    bool _initialized;

    public OneEuroFilter3(float minCutoff = 1.0f, float beta = 0.007f)
    {
        _minCutoff = minCutoff;
        _beta = beta;
    }

    public void Configure(float minCutoff, float beta)
    {
        _minCutoff = Mathf.Max(0.01f, minCutoff);
        _beta = Mathf.Max(0f, beta);
    }

    public void Reset(Vector3 value)
    {
        _lastRaw = value;
        _lastFiltered = value;
        _initialized = true;
    }

    public Vector3 Filter(Vector3 raw, float dt)
    {
        if (!_initialized)
        {
            Reset(raw);
            return raw;
        }

        dt = Mathf.Max(dt, 0.0001f);
        float freq = 1f / dt;
        Vector3 delta = (raw - _lastRaw) * freq;
        float speed = delta.magnitude;
        float cutoff = _minCutoff + _beta * speed;
        float te = 1f / (2f * Mathf.PI * cutoff);
        float alpha = 1f / (1f + te * freq);
        Vector3 filtered = Vector3.Lerp(_lastFiltered, raw, alpha);
        _lastRaw = raw;
        _lastFiltered = filtered;
        return filtered;
    }
}
