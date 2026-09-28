using System;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class OrRoomSetup
{
    static readonly (string token, string label)[] Labels =
    {
        ("Bistur", "BISTURÍ"),
        ("Cortadora", "RASURADORA"),
        ("Fibron", "MARCADOR"),
        ("Fibrón", "MARCADOR"),
        ("Marker", "MARCADOR"),
        ("Retractor", "RETRACTOR"),
        ("Drill", "TALADRO"),
        ("Taladro", "TALADRO"),
        ("Endoscop", "ENDOSCOPIO"),
        ("Kerrison", "KERRISON"),
        ("Coagul", "COAGULADOR"),
        ("Hemost", "HEMOSTÁTICO")
    };

    public static void Apply(XROrigin origin)
    {
        SilenceTeleportAndRays(origin);
        KeepCameraOnTheHead(origin);
        HideHeadLockedBoards(origin);
        HideDebugHelpers();
        ReplaceBrokenMaterials();
        StopEndoscopeMeshPath();
        HeadPaintHands.Ensure();
        EnsureFibron();
        PrepareTools();
        if (origin.GetComponent<HeadSettleAnchor>() == null)
            origin.gameObject.AddComponent<HeadSettleAnchor>();
    }

    static void SilenceTeleportAndRays(XROrigin origin)
    {
        Transform[] transforms = origin.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            string name = transforms[i].name;
            if (name.IndexOf("Teleport", StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            if (transforms[i] == origin.transform)
                continue;
            transforms[i].gameObject.SetActive(false);
        }

        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null)
                continue;
            string typeName = behaviours[i].GetType().Name;
            if (typeName.IndexOf("Teleport", StringComparison.OrdinalIgnoreCase) >= 0)
                behaviours[i].enabled = false;
            if (typeName.IndexOf("LineVisual", StringComparison.OrdinalIgnoreCase) >= 0)
                behaviours[i].enabled = false;
        }

        LineRenderer[] lines = origin.GetComponentsInChildren<LineRenderer>(true);
        for (int i = 0; i < lines.Length; i++)
        {
            if (IsUnderController(lines[i].transform))
                lines[i].enabled = false;
        }
    }

    static void KeepCameraOnTheHead(XROrigin origin)
    {
        if (origin.Camera == null)
            return;

        TrackedPoseDriver driver = origin.Camera.GetComponent<TrackedPoseDriver>();
        if (driver != null)
            driver.enabled = true;
    }

    static void HideHeadLockedBoards(XROrigin origin)
    {
        if (origin.Camera == null)
            return;

        Canvas[] canvases = origin.Camera.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
            KeepOff(canvases[i].gameObject);

        StayOffByName("Pantalla Tutorial");
        StayOffByName("TutorialPanel UI");
        Transform[] named = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < named.Length; i++)
        {
            if (named[i].name.IndexOf("Liberar", StringComparison.OrdinalIgnoreCase) >= 0)
                KeepOff(named[i].gameObject);
        }
    }

    static void StayOffByName(string objectName)
    {
        Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == objectName)
                KeepOff(all[i].gameObject);
        }
    }

    static void KeepOff(GameObject target)
    {
        if (target.GetComponent<StayHidden>() == null)
            target.AddComponent<StayHidden>();
        target.SetActive(false);
    }

    static void HideDebugHelpers()
    {
        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            string name = renderers[i].gameObject.name;
            if (name.IndexOf("Ghost", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Liberar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("ValidZone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("ColliderAux", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                renderers[i].enabled = false;
            }
        }
    }

    static void ReplaceBrokenMaterials()
    {
        Shader urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp == null)
            return;

        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].sharedMaterials;
            bool changed = false;
            for (int m = 0; m < materials.Length; m++)
            {
                Material source = materials[m];
                if (source == null || !IsBrokenShader(source))
                    continue;

                var replacement = new Material(urp);
                if (source.HasProperty("_Color"))
                    replacement.color = source.color;
                materials[m] = replacement;
                changed = true;
            }

            if (changed)
                renderers[i].sharedMaterials = materials;
        }
    }

    static bool IsBrokenShader(Material material)
    {
        if (material.shader == null)
            return true;
        string shaderName = material.shader.name;
        if (shaderName.Contains("TextMeshPro") || shaderName.StartsWith("UI/") || shaderName.Contains("Universal Render Pipeline"))
            return false;
        return shaderName == "Standard" ||
               shaderName.StartsWith("Standard ") ||
               shaderName.Contains("InternalError") ||
               shaderName.Contains("Hidden/InternalError");
    }

    static void StopEndoscopeMeshPath()
    {
        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null)
                continue;
            string typeName = behaviours[i].GetType().Name;
            if (typeName != "SplineAnimate" && typeName != "SplineFollower")
                continue;
            if (IsCameraPath(behaviours[i]))
                continue;

            XRGrabInteractable grab = behaviours[i].GetComponentInParent<XRGrabInteractable>();
            if (grab == null)
                continue;
            if (grab.name.IndexOf("Endoscop", StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            behaviours[i].enabled = false;
            HandTrackingLog.Write("Endoscope", $"{behaviours[i].name}: el recorrido queda en la cámara, no en la malla.");
        }
    }

    static bool IsCameraPath(MonoBehaviour behaviour)
    {
        if (behaviour.GetComponent<Camera>() != null)
            return true;
        string name = behaviour.gameObject.name.ToLowerInvariant();
        return name.Contains("camera") || name.Contains("camara") || name.Contains("cámara");
    }

    static void EnsureFibron()
    {
        XRGrabInteractable[] grabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        XRGrabInteractable original = null;
        for (int i = 0; i < grabs.Length; i++)
        {
            if (grabs[i].name.IndexOf("Marker Blue", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                original = grabs[i];
                break;
            }

            if (original == null && IsMarkerName(grabs[i].name))
                original = grabs[i];
        }

        if (original == null)
        {
            HandTrackingLog.Write("Marker", "No está el marcador original de la escena. No se crea una copia.");
            return;
        }

        original.gameObject.SetActive(true);
        for (int i = 0; i < grabs.Length; i++)
        {
            if (grabs[i] == original || !IsMarkerName(grabs[i].name))
                continue;
            grabs[i].gameObject.SetActive(false);
        }
    }

    static bool IsMarkerName(string objectName)
    {
        return objectName.IndexOf("Fibron", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Fibrón", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Marcador", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Marker", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static void PrepareTools()
    {
        XRGrabInteractable[] grabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < grabs.Length; i++)
        {
            XRGrabInteractable grab = grabs[i];
            grab.throwOnDetach = false;
            grab.forceGravityOnDetach = false;

            Transform grip = FindGrip(grab.transform);
            if (grip != null)
                grab.attachTransform = grip;

            Transform[] zones = grab.GetComponentsInChildren<Transform>(true);
            for (int z = 0; z < zones.Length; z++)
            {
                if (zones[z].name == "GrabZone")
                    zones[z].gameObject.SetActive(false);
            }

            if (grab.GetComponent<ToolHome>() == null)
            {
                ToolHome home = grab.gameObject.AddComponent<ToolHome>();
                if (grab.name.IndexOf("Drill", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    grab.name.IndexOf("Taladro", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    grab.name.IndexOf("Fibron", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    grab.name.IndexOf("Fibrón", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    SeatOnSurface(grab.transform);
                    home.Capture();
                }
            }

            string label = LabelFor(grab.name);
            Transform existingLabel = grab.transform.Find("ToolLabel");
            if (existingLabel != null)
                existingLabel.gameObject.SetActive(false);

            if (label == "BISTURÍ" && grab.GetComponent<ScalpelHighlight>() == null)
                grab.gameObject.AddComponent<ScalpelHighlight>();
        }
    }

    static void PlaceTutorial(XROrigin origin)
    {
        if (origin.Camera == null)
            return;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");
        var root = new GameObject("Guia Quirofano");
        Vector3 boardPosition = origin.transform.position + origin.transform.forward * 0.48f + origin.transform.right * 0.34f + Vector3.up * 1.02f;
        Vector3 towardUser = origin.transform.position - boardPosition;
        towardUser.y = 0f;
        if (towardUser.sqrMagnitude < 0.001f)
            towardUser = -origin.transform.forward;
        root.transform.SetPositionAndRotation(boardPosition, Quaternion.LookRotation(towardUser.normalized, Vector3.up));

        var canvasObject = new GameObject("Cartel");
        canvasObject.transform.SetParent(root.transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(760f, 460f);
        rect.localScale = Vector3.one * 0.00085f;

        var background = canvasObject.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(0.05f, 0.08f, 0.1f, 0.82f);

        TMP_Text title = CreateText(canvasObject.transform, font, "Titulo", new Vector2(0f, 150f), 52f, FontStyles.Bold);
        TMP_Text body = CreateText(canvasObject.transform, font, "Texto", new Vector2(0f, -30f), 30f, FontStyles.Normal);
        body.rectTransform.sizeDelta = new Vector2(700f, 300f);

        root.AddComponent<OrTutorialBoard>().Build(origin.Camera, root.transform, title, body);
    }

    static TMP_Text CreateText(Transform parent, TMP_FontAsset font, string name, Vector2 anchoredPosition, float size, FontStyles style)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        RectTransform rect = text.rectTransform;
        rect.sizeDelta = new Vector2(700f, 80f);
        rect.anchoredPosition = anchoredPosition;
        return text;
    }

    static void CreateLabel(Transform tool, string label)
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");
        var canvasObject = new GameObject("ToolLabel");
        canvasObject.transform.SetParent(tool, false);
        canvasObject.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(420f, 90f);
        rect.localScale = Vector3.one * 0.0007f;
        var textObject = new GameObject("Texto");
        textObject.transform.SetParent(canvasObject.transform, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        RectTransform textRect = text.rectTransform;
        textRect.sizeDelta = new Vector2(420f, 90f);
        textRect.anchoredPosition = Vector2.zero;
        if (font != null)
            text.font = font;
        text.text = label;
        text.fontSize = 64f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.95f, 0.75f, 1f);
        canvasObject.AddComponent<LabelBillboard>();
    }

    static string LabelFor(string objectName)
    {
        for (int i = 0; i < Labels.Length; i++)
        {
            if (objectName.IndexOf(Labels[i].token, StringComparison.OrdinalIgnoreCase) >= 0)
                return Labels[i].label;
        }

        return null;
    }

    static Transform FindGrip(Transform root)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string name = children[i].name;
            if (name == "AttachPoint" || name == "HandAttach" || name == "Mango" || name == "Handle")
                return children[i];
        }

        return null;
    }

    static void SeatOnSurface(Transform tool)
    {
        Renderer[] renderers = tool.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i].enabled)
                bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 origin = new Vector3(bounds.center.x, bounds.max.y + 0.35f, bounds.center.z);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        bool found = false;
        float surfaceY = bounds.min.y;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.transform == tool || hits[i].collider.transform.IsChildOf(tool))
                continue;
            if (hits[i].distance < best)
            {
                best = hits[i].distance;
                surfaceY = hits[i].point.y;
                found = true;
            }
        }

        if (!found)
            return;

        float lift = surfaceY - bounds.min.y;
        if (lift > 0.002f)
            tool.position += Vector3.up * (lift + 0.004f);
    }

    static bool IsUnderController(Transform target)
    {
        while (target != null)
        {
            if (target.name == "Left Controller" || target.name == "Right Controller")
                return true;
            target = target.parent;
        }

        return false;
    }
}

public class StayHidden : MonoBehaviour
{
    void OnEnable()
    {
        gameObject.SetActive(false);
    }
}

public class HeadSettleAnchor : MonoBehaviour
{
    XROrigin origin;
    Transform cameraTransform;
    Vector3 desiredForward;
    float stable;
    bool done;

    void Start()
    {
        origin = GetComponent<XROrigin>();
        if (origin == null || origin.Camera == null)
        {
            done = true;
            return;
        }

        cameraTransform = origin.Camera.transform;
        desiredForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (desiredForward.sqrMagnitude < 0.001f)
            desiredForward = Vector3.forward;
        desiredForward.Normalize();
    }

    void LateUpdate()
    {
        if (done || cameraTransform == null)
            return;

        if (cameraTransform.localPosition.sqrMagnitude < 0.0001f)
        {
            stable = 0f;
            return;
        }

        stable += Time.deltaTime;
        if (stable < 0.4f)
            return;

        Vector3 headForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
        if (headForward.sqrMagnitude < 0.01f)
            return;

        float yaw = Vector3.SignedAngle(headForward.normalized, desiredForward, Vector3.up);
        if (Mathf.Abs(yaw) > 1f)
            transform.RotateAround(cameraTransform.position, Vector3.up, yaw);
        done = true;
    }
}

public class LabelBillboard : MonoBehaviour
{
    XRGrabInteractable grab;
    Canvas canvas;

    void Awake()
    {
        grab = GetComponentInParent<XRGrabInteractable>();
        canvas = GetComponent<Canvas>();
    }

    void LateUpdate()
    {
        if (canvas != null && grab != null)
            canvas.enabled = !grab.isSelected;

        Camera camera = Camera.main;
        if (camera == null)
            return;
        Vector3 towardUser = camera.transform.position - transform.position;
        towardUser.y = 0f;
        if (towardUser.sqrMagnitude < 0.0001f)
            return;
        transform.rotation = Quaternion.LookRotation(towardUser.normalized, Vector3.up);
    }
}

public class ScalpelHighlight : MonoBehaviour
{
    Outline outline;

    void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline == null)
            outline = gameObject.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = new Color(1f, 0.82f, 0.15f, 1f);
        outline.OutlineWidth = 5f;
        outline.enabled = true;
    }

    void OnEnable()
    {
        HandGestureGrabber.Grabbed += OnGrabbed;
    }

    void OnDisable()
    {
        HandGestureGrabber.Grabbed -= OnGrabbed;
    }

    void OnGrabbed(XRGrabInteractable grab)
    {
        if (grab.gameObject != gameObject || outline == null)
            return;
        outline.enabled = false;
    }
}
