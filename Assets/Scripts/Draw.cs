using UnityEngine;

public class Draw : SurgicalTool
{
    #region Fields
    [Header("Pen Properties")]
    public Transform tip;
    public Material drawingMaterial;
    public Material tipMaterial;
    public float penWidth = 0.005f;
    public Color penColors;
    [SerializeField] float minDrawSpeed = 0.002f; // ajustable
    [SerializeField] float tipRadius = 0.0015f;
    [SerializeField] float tipCastDistance = 0.002f;
    [SerializeField] private float startDrawingOffsetFromTip = 0.001f;
    [SerializeField] float surfaceOffset = 0.0005f;

    [Header("Drawing Control")] 
    public bool isDrawing = false;
    public LayerMask drawingSurfaceLayer;

    private LineRenderer currentDrawing;
    private int index;
    private int currentColorIndex;
    private PenState state;
    private Vector3 lastTipPos;
    private float tipSpeed;
    RaycastHit[] hitBuffer = new RaycastHit[1];
    RaycastHit lastHit;
    bool hasValidContact;
    #endregion
    
    enum PenState
    {
        Idle,       // agarrado pero no listo
        Armed,      // punta en contacto
        Drawing
    }
    
    
    #region Unity Methods
    void Start()
    { Init(); }

    private void Init()
    {
        currentColorIndex = 0;
        tipMaterial.color = penColors;
    }

    void Update()
    {
        if (activeGestures == null)
            return;

        
        tipSpeed = Vector3.Distance(tip.position, lastTipPos) / Time.deltaTime;
        lastTipPos = tip.position;
        
        bool touchingSurface = IsTipTouchingSurface();
        print("Touching Surface: "+touchingSurface);
        bool inputActioned = activeGestures.Pinch > 0.7f;
        switch (state)
        {
            case PenState.Idle:
                if (touchingSurface)
                    state = PenState.Armed;
                break;

            case PenState.Armed:
                if (inputActioned)
                {
                    state = PenState.Drawing;
                }

                if (!touchingSurface)
                    state = PenState.Idle;
                break;

            case PenState.Drawing:
                if (!inputActioned || !touchingSurface)
                {
                    StopDrawing();
                    state = PenState.Armed;
                }
                else
                {
                    RenderDrawing();
                    
                }
                break;
        }
        
        
    }
    #endregion
    
    #region Surgical Tool Methods
    protected override void OnToolGrabbed()
    {
        base.OnToolGrabbed();
        lastTipPos = tip.position;
    }

    protected override void OnToolReleased()
    {
        base.OnToolReleased();
        StopDrawing();
    }
    #endregion
    
    #region Public Methods
    void RenderDrawing()
    {
        if (currentDrawing == null)
        {
            if (!hasValidContact)
                return;

            StartDrawing();
            return; // ← importante, evitamos usar index todavía
        }
        
        
        if (tipSpeed < minDrawSpeed)
            return; // mano quieta → no dibuja
        
        if (!hasValidContact || currentDrawing == null)
            return;

        Vector3 newPoint =
            lastHit.point + lastHit.normal * surfaceOffset;
        Vector3 prevPoint = currentDrawing.GetPosition(index);

        if (Vector3.Distance(prevPoint, newPoint) > 0.002f)
        {
            index++;
            currentDrawing.positionCount = index + 1;
            currentDrawing.SetPosition(index, newPoint);
        }
    }

    public void StartDrawing()
    {
        if (currentDrawing != null)
            return;
        
        if (!hasValidContact || lastHit.collider == null)
            return;
        
        
        index = 0;

        currentDrawing = new GameObject("Drawing").AddComponent<LineRenderer>();
        currentDrawing.material = drawingMaterial;
        currentDrawing.startColor = currentDrawing.endColor = penColors;
        currentDrawing.startWidth = currentDrawing.endWidth = penWidth;
        currentDrawing.useWorldSpace = true;

        currentDrawing.positionCount = 1;

        // Usamos el punto real de contacto
        currentDrawing.SetPosition(0,
            lastHit.point + lastHit.normal * surfaceOffset
        );
    }

    void StopDrawing()
    {
        currentDrawing = null;
        index = 0;
    }
    
    public void ClearDrawing()
    {
        if (currentDrawing != null)
        {
            Destroy(currentDrawing.gameObject);
            currentDrawing = null;
        }
    }
    #endregion
    
    #region Private Methods
    bool IsTipTouchingSurface()
    {
        RaycastHit hit;

        Vector3 origin = tip.position + (tip.right*startDrawingOffsetFromTip);
        Vector3 dir = -tip.right;

        // 🧪 DEBUG VISUAL
        Debug.DrawRay(origin, dir * tipCastDistance, Color.yellow);

        bool hasHit = Physics.Raycast(
            origin,
            dir,
            out hit,
            tipCastDistance,
            drawingSurfaceLayer,
            QueryTriggerInteraction.Ignore
        );

        if (!hasHit)
        {
            hasValidContact = false;
            return false;
        }

        // 🔒 Validación dura
        if (hit.collider == null)
        {
            hasValidContact = false;
            return false;
        }

        lastHit = hit;
        hasValidContact = true;

        // 📌 Debug del contacto
        Debug.DrawLine(
            hit.point,
            hit.point + hit.normal * 0.01f,
            Color.green
        );

        return true;
    }

    #endregion
    
    
}