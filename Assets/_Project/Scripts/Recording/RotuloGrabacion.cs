using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using System.Linq;
#endif

public enum EsquinaRotulo
{
    AbajoIzquierda,
    AbajoDerecha,
    ArribaIzquierda,
    ArribaDerecha
}

public enum FormatoContador
{
    [InspectorName("Minutos y segundos")]
    MinutosYSegundos,
    [InspectorName("Segundos")]
    Segundos,
    [InspectorName("Minutos, segundos y décimas")]
    MinutosSegundosYDecimas
}

/// <summary>
/// Rótulo de grabación. Vive en una esquina de la vista de juego:
/// logo, qué se está grabando y el tiempo transcurrido.
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class RotuloGrabacion : MonoBehaviour
{
    [Header("Contenido")]
    [Tooltip("Logo de Plagiocefalia.")]
    [SerializeField] Sprite logo;

    [Tooltip("Qué se está grabando. Conviene una sola línea.")]
    [TextArea(1, 3)]
    [SerializeField] string descripcion = "Simulación de corrección de plagiocefalia";

    [Tooltip("Línea secundaria. Si queda vacía, no se muestra.")]
    [SerializeField] string detalle = "";

    [Tooltip("En Play, la descripción pasa a ser el título del paso actual.")]
    [SerializeField] bool seguirPasoDelProcedimiento;

    [Header("Contador")]
    [Tooltip("Arranca el contador al entrar en Play.")]
    [SerializeField] bool contarAlIniciar = true;

    [SerializeField] FormatoContador formato = FormatoContador.MinutosYSegundos;

    [Tooltip("Punto rosa, el mismo del logo, mientras corre el tiempo.")]
    [SerializeField] bool mostrarMarca = true;

    [Header("Posición")]
    [SerializeField] EsquinaRotulo esquina = EsquinaRotulo.AbajoIzquierda;

    [Tooltip("Distancia a los bordes de la imagen, en píxeles de una referencia 1920×1080.")]
    [SerializeField] Vector2 margen = new Vector2(36f, 28f);

    [Range(0.7f, 1.6f)]
    [SerializeField] float escala = 1f;

    [SerializeField] bool visible = true;

    [Header("Apariencia")]
    [Range(16f, 32f)]
    [Tooltip("Tamaño del texto en la referencia 1920×1080. El logo crece con él.")]
    [SerializeField] float tamanoTexto = 22f;

    [SerializeField] Color colorFondo = new Color(0.07f, 0.11f, 0.15f, 0.94f);
    [SerializeField] Color colorAcento = new Color(0.22f, 0.74f, 0.91f, 1f);
    [SerializeField] Color colorTexto = new Color(0.96f, 0.97f, 0.98f, 1f);
    [SerializeField] Color colorMarca = new Color(0.93f, 0.40f, 0.62f, 1f);

    [Header("Visor")]
    [Tooltip("Cámara del casco. Si está vacía, usa la cámara del visor.")]
    [SerializeField] Camera camara;

    [Tooltip("Distancia a los ojos, en metros. El rótulo queda dentro de lo que se ve con los anteojos.")]
    [Range(0.45f, 1.4f)]
    [SerializeField] float distanciaAlVisor = 0.8f;

    [SerializeField] int orden = 400;

    GameObject uiRoot;
    Canvas canvas;
    CanvasScaler scaler;
    RectTransform panelRect;
    HorizontalLayoutGroup layout;
    Image panelImage;
    Shadow dropShadow;
    Image railImage;
    Image logoImage;
    Image dividerImage;
    Image markImage;
    TextMeshProUGUI descriptionText;
    TextMeshProUGUI detailText;
    TextMeshProUGUI timeText;
    LayoutElement logoElement;
    LayoutElement descriptionElement;
    LayoutElement detailElement;
    LayoutElement timeElement;
    LayoutElement railElement;
    LayoutElement markElement;
    LayoutElement dividerElement;

    float elapsed;
    bool running;
    string pasoAplicado;
    bool applyQueued;
    TMP_FontAsset font;

    static Sprite rounded;
    static Sprite quad;
    static Sprite circle;

    public bool Contando => running;
    public string TiempoVisible => Formatear(elapsed);

    void OnEnable()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating)
            return;
#endif
        Apply();
    }

    void OnDisable()
    {
        ClearUi();
    }

    void Start()
    {
        if (!Application.isPlaying)
            return;

        elapsed = 0f;
        running = contarAlIniciar;
        pasoAplicado = null;
        ApplyTime();
    }

    void Update()
    {
        if (!Application.isPlaying || uiRoot == null)
            return;

        if (running)
        {
            elapsed += Time.unscaledDeltaTime;
            ApplyTime();
        }

        if (seguirPasoDelProcedimiento)
            PullStepTitle();
    }

    void LateUpdate()
    {
        if (!Application.isPlaying || canvas == null)
            return;

        FollowHeadset();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        if (applyQueued)
            return;

        applyQueued = true;
        UnityEditor.EditorApplication.delayCall += FlushApply;
    }

    void FlushApply()
    {
        applyQueued = false;
        if (this == null || !isActiveAndEnabled)
            return;

        Apply();
    }
#endif

    [ContextMenu("Reiniciar contador")]
    public void ReiniciarContador()
    {
        elapsed = 0f;
        ApplyTime();
    }

    public void AlternarContador()
    {
        running = !running;
        ApplyTime();
    }

    public void EstablecerDescripcion(string texto)
    {
        descripcion = texto ?? string.Empty;
        pasoAplicado = null;
        Apply();
    }

    void Apply()
    {
        if (panelRect == null)
            Build();

        if (panelRect == null)
            return;

        uiRoot.SetActive(visible);
        canvas.sortingOrder = orden;

        panelImage.color = colorFondo;
        panelImage.sprite = Rounded();
        panelImage.type = Image.Type.Sliced;
        dropShadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        dropShadow.effectDistance = new Vector2(0f, -3f);
        railImage.color = colorAcento;
        dividerImage.color = new Color(colorTexto.r, colorTexto.g, colorTexto.b, 0.22f);

        float logoSize = Mathf.Max(76f, tamanoTexto * 4f);
        bool hasLogo = logo != null;
        logoImage.gameObject.SetActive(hasLogo);
        if (hasLogo)
        {
            logoImage.sprite = logo;
            logoImage.color = Color.white;
            logoImage.preserveAspect = true;
            logoImage.type = Image.Type.Simple;
            logoElement.preferredWidth = logoSize;
            logoElement.preferredHeight = logoSize;
        }

        int pad = Mathf.RoundToInt(tamanoTexto * 0.58f);
        layout.padding = new RectOffset(pad + 2, pad + 8, Mathf.Max(8, pad - 2), Mathf.Max(8, pad - 2));
        layout.spacing = Mathf.Max(10f, tamanoTexto * 0.5f);

        railElement.preferredWidth = 3f;
        railElement.preferredHeight = tamanoTexto * 1.35f;
        dividerElement.preferredWidth = 2f;
        dividerElement.preferredHeight = tamanoTexto * 1.2f;
        markElement.preferredWidth = tamanoTexto * 0.46f;
        markElement.preferredHeight = tamanoTexto * 0.46f;
        markImage.gameObject.SetActive(mostrarMarca);

        StyleText(descriptionText, tamanoTexto, FontStyles.Bold, colorTexto);
        StyleText(detailText, tamanoTexto * 0.68f, FontStyles.Normal, new Color(colorTexto.r, colorTexto.g, colorTexto.b, 0.72f));
        StyleText(timeText, tamanoTexto, FontStyles.Bold, colorTexto);
        timeText.richText = true;
        timeText.overflowMode = TextOverflowModes.Overflow;
        timeText.alignment = TextAlignmentOptions.MidlineLeft;

        bool hasDetail = !string.IsNullOrWhiteSpace(detalle);
        detailText.gameObject.SetActive(hasDetail);
        detailText.text = hasDetail ? detalle.Trim() : string.Empty;

        string linea = descripcion ?? string.Empty;
        if (Application.isPlaying && seguirPasoDelProcedimiento && !string.IsNullOrWhiteSpace(pasoAplicado))
            linea = pasoAplicado;

        descriptionText.text = linea.Trim();
        FitWidth(descriptionText, descriptionElement, descriptionText.text, 80f, 680f);
        if (hasDetail)
            FitWidth(detailText, detailElement, detailText.text, descriptionElement.preferredWidth, 680f);

        float timeFactor = 4f;
        if (formato == FormatoContador.Segundos)
            timeFactor = 4.5f;
        else if (formato == FormatoContador.MinutosSegundosYDecimas)
            timeFactor = 5.3f;

        float timeWidth = tamanoTexto * timeFactor;
        timeElement.preferredWidth = timeWidth;
        timeElement.minWidth = timeWidth;

        ApplyTime();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

        if (Application.isPlaying)
            FollowHeadset();
        else
            ShowInGameView();

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
    }

    void Build()
    {
        ClearUi();

        TMP_FontAsset face = ResolveFont();
        if (face == null)
        {
            Debug.LogWarning("Rótulo de grabación: no encontré una fuente de TextMesh Pro.", this);
            return;
        }

        uiRoot = CreateRoot();
        uiRoot.layer = 5;
        if (!Application.isPlaying)
            Stretch((RectTransform)uiRoot.transform);

        canvas = uiRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;
        canvas.overrideSorting = true;
        canvas.additionalShaderChannels =
            AdditionalCanvasShaderChannels.TexCoord1 |
            AdditionalCanvasShaderChannels.Normal |
            AdditionalCanvasShaderChannels.Tangent;

        scaler = uiRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        panelRect = NewRect("Placa", uiRoot.transform);
        panelImage = panelRect.gameObject.AddComponent<Image>();
        panelImage.raycastTarget = false;
        dropShadow = panelRect.gameObject.AddComponent<Shadow>();
        dropShadow.useGraphicAlpha = true;

        layout = panelRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelRect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        railImage = NewImage("Acento", panelRect);
        railElement = railImage.gameObject.AddComponent<LayoutElement>();

        logoImage = NewImage("Logo", panelRect);
        logoElement = logoImage.gameObject.AddComponent<LayoutElement>();

        RectTransform column = NewRect("Texto", panelRect);
        VerticalLayoutGroup columnLayout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        columnLayout.childAlignment = TextAnchor.MiddleLeft;
        columnLayout.spacing = 1f;
        columnLayout.childControlWidth = true;
        columnLayout.childControlHeight = true;
        columnLayout.childForceExpandWidth = false;
        columnLayout.childForceExpandHeight = false;

        descriptionText = NewText("Descripcion", column, face);
        descriptionElement = descriptionText.gameObject.AddComponent<LayoutElement>();
        detailText = NewText("Detalle", column, face);
        detailElement = detailText.gameObject.AddComponent<LayoutElement>();

        dividerImage = NewImage("Separador", panelRect);
        dividerElement = dividerImage.gameObject.AddComponent<LayoutElement>();

        markImage = NewImage("Marca", panelRect);
        markImage.sprite = Circle();
        markElement = markImage.gameObject.AddComponent<LayoutElement>();

        timeText = NewText("Tiempo", panelRect, face);
        timeElement = timeText.gameObject.AddComponent<LayoutElement>();

        Material plate = VisorPlate();
        if (plate != null)
        {
            panelImage.material = plate;
            railImage.material = plate;
            logoImage.material = plate;
            dividerImage.material = plate;
            markImage.material = plate;
        }
        UseOverlayFont(descriptionText);
        UseOverlayFont(detailText);
        UseOverlayFont(timeText);

        if (!Application.isPlaying)
            MarkTree(uiRoot);
    }

    void ShowInGameView()
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        Stretch((RectTransform)uiRoot.transform);
        Place(panelRect, esquina, margen, escala);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
    }

    void FollowHeadset()
    {
        Camera cam = HeadsetCamera();
        if (cam == null)
        {
            ShowInGameView();
            return;
        }

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
        }

        Vector2 size = panelRect.rect.size;
        if (size.x < 8f || size.y < 8f)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            size = panelRect.rect.size;
        }

        if (size.x < 8f || size.y < 8f)
            size = new Vector2(760f, 120f);

        RectTransform root = (RectTransform)uiRoot.transform;
        if (root.parent != transform)
            root.SetParent(transform, false);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = size;
        root.localScale = Vector3.one;

        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.localRotation = Quaternion.identity;
        panelRect.localScale = Vector3.one;

        Vector2 pivot = PivotFor(esquina);
        root.pivot = pivot;

        float distance = Mathf.Clamp(distanciaAlVisor, 0.45f, 1.4f);
        float vfov = cam.fieldOfView;
        if (vfov < 25f || vfov > 130f)
            vfov = 90f;

        float halfY = Mathf.Tan(vfov * 0.5f * Mathf.Deg2Rad) * distance;
        float aspect = cam.aspect;
        if (aspect < 0.6f || aspect > 2.2f)
            aspect = 1.1f;
        float halfX = halfY * aspect;

        float textMeters = Mathf.Tan(1.7f * Mathf.Deg2Rad) * distance;
        float worldScale = textMeters / Mathf.Max(12f, tamanoTexto) * Mathf.Max(0.2f, escala);
        float width = size.x * worldScale;
        float maxWidth = halfX * 0.92f;
        if (width > maxWidth && width > 0.001f)
            worldScale *= maxWidth / width;

        root.localScale = new Vector3(worldScale, worldScale, worldScale);

        float signX = pivot.x > 0.5f ? 1f : -1f;
        float signY = pivot.y > 0.5f ? 1f : -1f;
        float x = signX * halfX * 0.46f;
        float y = signY * halfY * 0.40f;
        float insetX = Mathf.Abs(margen.x) * worldScale;
        float insetY = Mathf.Abs(margen.y) * worldScale;
        x += signX > 0f ? -insetX : insetX;
        y += signY > 0f ? -insetY : insetY;

        root.SetPositionAndRotation(
            cam.transform.TransformPoint(new Vector3(x, y, distance)),
            cam.transform.rotation);
    }

    Camera HeadsetCamera()
    {
        if (camara != null)
            return camara;

        XROrigin[] origins = FindObjectsByType<XROrigin>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < origins.Length; i++)
        {
            if (origins[i] != null && origins[i].Camera != null)
                return origins[i].Camera;
        }

        return Camera.main;
    }

    static Vector2 PivotFor(EsquinaRotulo corner)
    {
        switch (corner)
        {
            case EsquinaRotulo.AbajoDerecha:
                return new Vector2(1f, 0f);
            case EsquinaRotulo.ArribaIzquierda:
                return new Vector2(0f, 1f);
            case EsquinaRotulo.ArribaDerecha:
                return Vector2.one;
            default:
                return Vector2.zero;
        }
    }

    void PullStepTitle()
    {
        ProcedureManager manager = ProcedureManager.Instance;
        if (manager == null)
            return;

        string title = manager.CurrentTitle;
        if (string.IsNullOrWhiteSpace(title) || title == pasoAplicado)
            return;

        pasoAplicado = title.Trim();
        if (descriptionText == null)
            return;

        descriptionText.text = pasoAplicado;
        FitWidth(descriptionText, descriptionElement, pasoAplicado, 80f, 680f);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
    }

    void ApplyTime()
    {
        if (timeText != null)
            timeText.text = "<mspace=0.62em>" + Formatear(elapsed) + "</mspace>";

        if (markImage == null)
            return;

        float alpha = 1f;
        if (Application.isPlaying)
        {
            if (running)
                alpha = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.4f));
            else
                alpha = 0.38f;
        }

        Color c = colorMarca;
        c.a *= alpha;
        markImage.color = c;
    }

    string Formatear(float t)
    {
        if (t < 0f)
            t = 0f;

        int total = Mathf.FloorToInt(t);
        int minutes = total / 60;
        int seconds = total % 60;

        switch (formato)
        {
            case FormatoContador.Segundos:
                return total + " s";
            case FormatoContador.MinutosSegundosYDecimas:
                int tenth = Mathf.FloorToInt((t - total) * 10f);
                return minutes.ToString("00") + ":" + seconds.ToString("00") + "." + tenth;
            default:
                return minutes.ToString("00") + ":" + seconds.ToString("00");
        }
    }

    void ClearUi()
    {
        panelRect = null;
        canvas = null;
        descriptionText = null;
        detailText = null;
        timeText = null;
        markImage = null;

        if (uiRoot != null)
        {
            if (Application.isPlaying)
                Destroy(uiRoot);
            else
                DestroyImmediate(uiRoot);
            uiRoot = null;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name != "Rotulo UI")
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    TMP_FontAsset ResolveFont()
    {
        if (font != null)
            return font;

        font = TMP_Settings.defaultFontAsset;
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        return font;
    }

    static void StyleText(TMP_Text text, float size, FontStyles style, Color color)
    {
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.extraPadding = true;
        text.richText = false;
    }

    static void FitWidth(TMP_Text text, LayoutElement element, string value, float min, float max)
    {
        float measured = string.IsNullOrEmpty(value) ? min : text.GetPreferredValues(value, 4000f, 80f).x;
        float width = Mathf.Clamp(measured, min, max);
        element.preferredWidth = width;
        element.minWidth = Mathf.Min(width, min);
        element.flexibleWidth = 0f;
    }

    static void Place(RectTransform rect, EsquinaRotulo corner, Vector2 margin, float scale)
    {
        Vector2 anchor;
        switch (corner)
        {
            case EsquinaRotulo.AbajoDerecha:
                anchor = new Vector2(1f, 0f);
                break;
            case EsquinaRotulo.ArribaIzquierda:
                anchor = new Vector2(0f, 1f);
                break;
            case EsquinaRotulo.ArribaDerecha:
                anchor = Vector2.one;
                break;
            default:
                anchor = Vector2.zero;
                break;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        float x = anchor.x > 0.5f ? -Mathf.Abs(margin.x) : Mathf.Abs(margin.x);
        float y = anchor.y > 0.5f ? -Mathf.Abs(margin.y) : Mathf.Abs(margin.y);
        rect.anchoredPosition = new Vector2(x, y);
        rect.localScale = Vector3.one * Mathf.Max(0.2f, scale);
        rect.localRotation = Quaternion.identity;
    }

    static GameObject CreateRoot()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return UnityEditor.EditorUtility.CreateGameObjectWithHideFlags(
                "Rotulo UI",
                HideFlags.HideAndDontSave,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
        }
#endif
        return new GameObject("Rotulo UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
    }

#if UNITY_EDITOR
    void Reset()
    {
        if (logo != null)
            return;

        Sprite[] sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Textures/Logos/Logo_Plaggio.png")
            .OfType<Sprite>()
            .ToArray();

        if (sprites.Length > 0)
            logo = sprites[0];
    }
#endif

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static Image NewImage(string name, Transform parent)
    {
        RectTransform rect = NewRect(name, parent);
        rect.gameObject.AddComponent<CanvasRenderer>();
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = Quad();
        image.raycastTarget = false;
        image.maskable = false;
        return image;
    }

    static TextMeshProUGUI NewText(string name, Transform parent, TMP_FontAsset face)
    {
        RectTransform rect = NewRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = face;
        text.text = string.Empty;
        text.raycastTarget = false;
        text.maskable = false;
        return text;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;
    }

    static void MarkTree(GameObject go)
    {
        go.hideFlags = HideFlags.HideAndDontSave;
        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null)
                components[i].hideFlags = HideFlags.HideAndDontSave;
        }

        for (int i = 0; i < go.transform.childCount; i++)
            MarkTree(go.transform.GetChild(i).gameObject);
    }

    static Material visorPlate;

    static Material VisorPlate()
    {
        if (visorPlate != null)
            return visorPlate;

        Shader shader = Shader.Find("Simugias/Rotulo Visor");
        if (shader == null)
            return null;

        visorPlate = new Material(shader);
        visorPlate.hideFlags = HideFlags.HideAndDontSave;
        return visorPlate;
    }

    static void UseOverlayFont(TMP_Text text)
    {
        Shader shader = Shader.Find("TextMeshPro/Distance Field Overlay");
        if (shader == null || text == null || text.fontSharedMaterial == null)
            return;

        Material mat = new Material(text.fontSharedMaterial);
        mat.shader = shader;
        mat.hideFlags = HideFlags.HideAndDontSave;
        text.fontMaterial = mat;
    }

    static Sprite Rounded()
    {
        if (rounded != null)
            return rounded;

        const int size = 96;
        const float radius = 28f;
        Texture2D tex = NewTexture(size, size, (x, y) =>
        {
            float px = x + 0.5f;
            float py = y + 0.5f;
            bool cornerX = px < radius || px > size - radius;
            bool cornerY = py < radius || py > size - radius;
            if (cornerX && cornerY)
            {
                float qx = px < radius ? radius : size - radius;
                float qy = py < radius ? radius : size - radius;
                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(qx, qy));
                return Mathf.Clamp01(radius - dist + 0.65f);
            }

            float edge = Mathf.Min(px, py, size - px, size - py);
            return edge >= 1f ? 1f : Mathf.Clamp01(edge + 0.35f);
        });

        rounded = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        rounded.hideFlags = HideFlags.HideAndDontSave;
        return rounded;
    }

    static Sprite Circle()
    {
        if (circle != null)
            return circle;

        const int size = 32;
        float radius = size * 0.5f;
        Texture2D tex = NewTexture(size, size, (x, y) =>
        {
            float dx = x + 0.5f - radius;
            float dy = y + 0.5f - radius;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(radius - dist);
        });

        circle = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        circle.hideFlags = HideFlags.HideAndDontSave;
        return circle;
    }

    static Sprite Quad()
    {
        if (quad != null)
            return quad;

        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32[] pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(pixels);
        tex.Apply(false, true);

        quad = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        quad.hideFlags = HideFlags.HideAndDontSave;
        return quad;
    }

    static Texture2D NewTexture(int width, int height, System.Func<int, int, float> alpha)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(alpha(x, y) * 255f), 0, 255);
                pixels[y * width + x] = new Color32(255, 255, 255, a);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }
}
