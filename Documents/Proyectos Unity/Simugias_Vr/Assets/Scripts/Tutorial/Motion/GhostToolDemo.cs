using UnityEngine;

/// <summary>
/// Herramienta fantasma holográfica: mismo mesh, material ghost, no ejecuta acciones.
/// </summary>
public class GhostToolDemo : MonoBehaviour
{
    Transform _ghostRoot;
    Transform _tipProxy;
    LineRenderer _pathLine;
    LineRenderer _trailLine;
    Transform _startMarker;
    Transform _endMarker;
    Material _ghostMat;
    Material _pathMat;
    Material _trailGoodMat;
    Material _trailWarnMat;
    bool _built;
    Vector3[] _path;
    float _demoT;
    bool _playing;
    float _demoSpeed = 0.22f;
    float _pauseAtEnds = 0.55f;
    float _pauseTimer;
    bool _waitingUser;

    public bool IsPlaying => _playing;
    public bool WaitingUser => _waitingUser;
    public float DemoProgress => _demoT;

    public void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        _ghostMat = CreateGhostMat(new Color(0.25f, 0.85f, 1f, 0.38f));
        _pathMat = CreateUnlitMat(new Color(0.2f, 0.9f, 1f, 0.85f));
        _trailGoodMat = CreateUnlitMat(new Color(0.25f, 0.95f, 0.45f, 0.9f));
        _trailWarnMat = CreateUnlitMat(new Color(0.95f, 0.75f, 0.2f, 0.9f));

        _ghostRoot = new GameObject("GhostTool").transform;
        _ghostRoot.SetParent(transform, false);

        _tipProxy = new GameObject("GhostTip").transform;
        _tipProxy.SetParent(_ghostRoot, false);

        _pathLine = CreateLine("PathGuide", _pathMat, 0.006f, 0.004f);
        _trailLine = CreateLine("UserTrail", _trailGoodMat, 0.005f, 0.003f);
        _trailLine.positionCount = 0;

        _startMarker = CreateMarker("Start", new Color(0.3f, 1f, 0.5f, 0.7f));
        _endMarker = CreateMarker("End", new Color(1f, 0.45f, 0.25f, 0.7f));

        SetVisible(false);
    }

    public void BindSourceMesh(Transform sourceTool)
    {
        EnsureBuilt();
        for (int i = _ghostRoot.childCount - 1; i >= 0; i--)
        {
            var child = _ghostRoot.GetChild(i);
            if (child == _tipProxy) continue;
            Destroy(child.gameObject);
        }

        if (sourceTool == null) return;

        var renderers = sourceTool.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var src = renderers[i];
            var mf = src.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var go = new GameObject("GhostMesh_" + src.name);
            go.transform.SetParent(_ghostRoot, false);
            go.transform.position = src.transform.position;
            go.transform.rotation = src.transform.rotation;
            go.transform.localScale = src.transform.lossyScale;

            // Reparent in local space relative to source root
            go.transform.SetParent(sourceTool, true);
            Vector3 lp = go.transform.localPosition;
            Quaternion lr = go.transform.localRotation;
            Vector3 ls = go.transform.localScale;
            go.transform.SetParent(_ghostRoot, false);
            go.transform.localPosition = lp;
            go.transform.localRotation = lr;
            go.transform.localScale = ls;

            var nmf = go.AddComponent<MeshFilter>();
            nmf.sharedMesh = mf.sharedMesh;
            var nr = go.AddComponent<MeshRenderer>();
            nr.sharedMaterial = _ghostMat;
            nr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            nr.receiveShadows = false;
        }

        // Fallback: tip sphere if no meshes
        if (_ghostRoot.childCount <= 1)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "GhostFallback";
            sphere.transform.SetParent(_ghostRoot, false);
            sphere.transform.localScale = Vector3.one * 0.035f;
            Object.Destroy(sphere.GetComponent<Collider>());
            sphere.GetComponent<MeshRenderer>().sharedMaterial = _ghostMat;
        }
    }

    public void SetPath(Vector3[] worldPoints)
    {
        EnsureBuilt();
        _path = worldPoints;
        if (_path == null || _path.Length < 2)
        {
            _pathLine.positionCount = 0;
            return;
        }

        _pathLine.positionCount = _path.Length;
        _pathLine.SetPositions(_path);
        _startMarker.position = _path[0];
        _endMarker.position = _path[_path.Length - 1];
        _startMarker.gameObject.SetActive(true);
        _endMarker.gameObject.SetActive(true);
    }

    public void PlayDemo(float speed = 0.22f, bool loopUntilStart = true)
    {
        EnsureBuilt();
        _demoSpeed = Mathf.Max(0.05f, speed);
        _demoT = 0f;
        _pauseTimer = 0f;
        _playing = true;
        _waitingUser = loopUntilStart;
        SetVisible(true);
    }

    public void StopDemo()
    {
        _playing = false;
        _waitingUser = false;
        if (_ghostRoot != null)
            _ghostRoot.gameObject.SetActive(false);
    }

    public void SetVisible(bool visible)
    {
        EnsureBuilt();
        gameObject.SetActive(visible);
        if (_pathLine != null) _pathLine.enabled = visible && _path != null && _path.Length >= 2;
        if (_startMarker != null) _startMarker.gameObject.SetActive(visible && _path != null);
        if (_endMarker != null) _endMarker.gameObject.SetActive(visible && _path != null);
        if (_ghostRoot != null) _ghostRoot.gameObject.SetActive(visible && _playing);
    }

    public void SetAssistVisuals(MotionAssistLevel level)
    {
        EnsureBuilt();
        bool showPath = level != MotionAssistLevel.Free;
        bool showGhost = level == MotionAssistLevel.Guided || level == MotionAssistLevel.Assisted;
        bool showMarkers = level != MotionAssistLevel.Free && level != MotionAssistLevel.Minimal;
        if (_pathLine != null) _pathLine.enabled = showPath && _path != null;
        if (_ghostRoot != null && !_playing) _ghostRoot.gameObject.SetActive(false);
        if (_startMarker != null) _startMarker.gameObject.SetActive(showMarkers && _path != null);
        if (_endMarker != null) _endMarker.gameObject.SetActive(showMarkers && _path != null);
        if (!showGhost && _playing) StopDemo();
    }

    public void PushTrailPoint(Vector3 point, bool good)
    {
        EnsureBuilt();
        if (_trailLine == null) return;
        _trailLine.sharedMaterial = good ? _trailGoodMat : _trailWarnMat;
        int n = _trailLine.positionCount;
        if (n > 180)
        {
            // Compact: drop oldest chunk
            var buf = new Vector3[120];
            for (int i = 0; i < 120; i++)
                buf[i] = _trailLine.GetPosition(n - 120 + i);
            _trailLine.positionCount = 120;
            _trailLine.SetPositions(buf);
            n = 120;
        }
        _trailLine.positionCount = n + 1;
        _trailLine.SetPosition(n, point);
    }

    public void ClearTrail()
    {
        if (_trailLine != null)
            _trailLine.positionCount = 0;
    }

    public void TickDemo(float dt, System.Func<float, Vector3> posAt, System.Func<float, Quaternion> rotAt)
    {
        if (!_playing || _path == null || posAt == null) return;

        if (_pauseTimer > 0f)
        {
            _pauseTimer -= dt;
            return;
        }

        _demoT += dt * _demoSpeed;
        if (_demoT >= 1f)
        {
            _demoT = 1f;
            _pauseTimer = _pauseAtEnds;
            if (_waitingUser)
                _demoT = 0f;
            else
                _playing = false;
        }

        Vector3 p = posAt(_demoT);
        Quaternion r = rotAt != null ? rotAt(_demoT) : Quaternion.identity;
        if (_ghostRoot != null)
        {
            _ghostRoot.position = p;
            _ghostRoot.rotation = r;
            _ghostRoot.gameObject.SetActive(true);
        }
    }

    public void ShowCorrectionGhost(Vector3 pos, Quaternion rot)
    {
        EnsureBuilt();
        if (_ghostRoot == null) return;
        _ghostRoot.gameObject.SetActive(true);
        _ghostRoot.position = pos;
        _ghostRoot.rotation = rot;
    }

    LineRenderer CreateLine(string name, Material mat, float start, float end)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = mat;
        lr.widthMultiplier = 1f;
        lr.startWidth = start;
        lr.endWidth = end;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.numCapVertices = 4;
        lr.positionCount = 0;
        return lr;
    }

    Transform CreateMarker(string name, Color c)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * 0.028f;
        Object.Destroy(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = CreateGhostMat(c);
        go.SetActive(false);
        return go.transform;
    }

    static Material CreateGhostMat(Color c)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        m.renderQueue = 3000;
        return m;
    }

    static Material CreateUnlitMat(Color c)
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        return m;
    }
}
