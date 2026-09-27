using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class OrTutorialBoard : MonoBehaviour
{
    const float MissingToolSeconds = 8f;

    struct Step
    {
        public string Title;
        public string Body;
        public string[] ToolNames;
        public bool RequiresGrab;
        public bool RequiresStroke;
        public bool BasicsLook;
        public bool BasicsHands;
        public bool BasicsPinch;
        public bool BasicsRelease;
        public bool BasicsReadPanel;
    }

    readonly List<Step> steps = new List<Step>();
    int index;
    float missingTimer;
    bool sawPinch;
    TMP_Text title;
    TMP_Text body;
    Transform board;
    Camera cam;
    Quaternion startLook;

    public void Build(Camera camera, Transform boardRoot, TMP_Text titleText, TMP_Text bodyText)
    {
        cam = camera;
        board = boardRoot;
        title = titleText;
        body = bodyText;
        if (cam != null)
            startLook = cam.transform.rotation;

        steps.Add(Basics("Mirar",
            "Girá la cabeza. La vista sigue al visor y no queda fija hacia adelante.",
            look: true));
        steps.Add(Basics("Las manos",
            "Las manos son el control. Los joysticks quedan solo de respaldo.",
            hands: true));
        steps.Add(Basics("Pellizco",
            "Juntá pulgar e índice para agarrar. Estar cerca no alcanza.",
            pinch: true));
        steps.Add(Basics("Soltar",
            "Abrí los dedos para soltar. La herramienta se queda donde la dejás.",
            release: true));
        steps.Add(Basics("El panel",
            "Este cartel está delante tuyo, abajo y al costado. No está pegado al visor.",
            panel: true));

        steps.Add(Tool("BISTURÍ",
            "Tomá el BISTURÍ de la mesa. El contorno marca el mango. El paso avanza al agarrarlo, no al mirarlo.",
            "Bistur", "Scalpel"));
        steps.Add(Tool("Rasurado",
            "RASURADORA. Pasala sobre el cuero cabelludo. Gesto: mantener el pellizco mientras recorrés.",
            "Cortadora", "Rasur", "Shaver"));
        steps.Add(Tool("Marcador",
            "Tomá el MARCADOR (fibrón) de la mesa, junto al hemostático.",
            "Fibron", "Fibrón", "Marker"));
        steps.Add(Stroke("Dibujar",
            "Apoyá la punta en la frente. El trazo sale solo en la piel. No atravieses la cabeza. Gesto: mantener."));
        steps.Add(Tool("Incisión",
            "BISTURÍ sobre la línea marcada. Gesto: mantener el corte. Si no hay línea activa, no aparece la flecha.",
            "Bistur", "Scalpel"));
        steps.Add(Tool("Retractor",
            "RETRACTOR en la incisión. Para sacarlo se vuelve a tomar: la piel vuelve y no hace falta un botón.",
            "Retractor"));
        steps.Add(Tool("Disección",
            "Separá el tejido subcutáneo con el instrumento de disección. Gesto: mantener.",
            "Diseccion", "Disección", "Espatul"));
        steps.Add(Tool("Taladro",
            "TALADRO apoyado en la mesa. Agarralo del mango y perforá. Gesto: mantener.",
            "Drill", "Taladro"));
        steps.Add(Tool("Endoscopio",
            "ENDOSCOPIO. La mano mueve el instrumento. La cámara queda limitada al recorrido.",
            "Endoscop"));
        steps.Add(Tool("Kerrison",
            "KERRISON. La rotación de la muñeca define el bocado. El hueso no desaparece solo. Gesto: pulsar el mordisco.",
            "Kerrison"));
        steps.Add(Tool("Coagulador",
            "COAGULADOR sobre el punto de sangrado. Gesto: mantener.",
            "Coagul"));
        steps.Add(Tool("Hemostático",
            "HEMOSTÁTICO en el lecho. Gesto: pulsar para dejarlo.",
            "Hemost"));
        steps.Add(Tool("Suturectomía",
            "Cerrá el hueso con el instrumental de suturectomía. Gesto: mantener el recorrido.",
            "Suture", "Forceps"));
        steps.Add(Tool("Plástica cutánea",
            "Cerrá la piel. Gesto: mantener la sutura. Con esto termina la guía.",
            "Plastic", "Aguja", "Needle", "Sutura"));

        HandGestureGrabber.Grabbed += OnGrabbed;
        HandGestureGrabber.Released += OnReleased;
        SkinStrokeGate.StrokeOnSkin += OnStroke;
        Show(0);
    }

    void OnDestroy()
    {
        HandGestureGrabber.Grabbed -= OnGrabbed;
        HandGestureGrabber.Released -= OnReleased;
        SkinStrokeGate.StrokeOnSkin -= OnStroke;
    }

    void Update()
    {
        if (steps.Count == 0 || index >= steps.Count)
            return;

        Step step = steps[index];
        if (step.BasicsLook && cam != null && Quaternion.Angle(startLook, cam.transform.rotation) > 18f)
            Advance();
        else if (step.BasicsHands && HandGestureGrabber.EitherHandTracked)
            Advance();
        else if (step.BasicsPinch && HandGestureGrabber.EitherPinching)
        {
            sawPinch = true;
            Advance();
        }
        else if (step.BasicsRelease && sawPinch && !HandGestureGrabber.EitherPinching)
            Advance();
        else if (step.BasicsReadPanel && cam != null && board != null)
        {
            Vector3 toBoard = board.position - cam.transform.position;
            if (Vector3.Dot(cam.transform.forward, toBoard.normalized) > 0.65f)
                Advance();
        }
        else if ((step.RequiresGrab || step.RequiresStroke) && !ToolExists(step))
        {
            missingTimer += Time.deltaTime;
            if (missingTimer >= MissingToolSeconds)
                Advance();
        }
        else if (step.BasicsLook || step.BasicsHands || step.BasicsPinch || step.BasicsRelease || step.BasicsReadPanel)
        {
            missingTimer += Time.deltaTime;
            if (missingTimer >= 20f)
                Advance();
        }
    }

    void OnGrabbed(XRGrabInteractable grab)
    {
        if (index >= steps.Count)
            return;
        Step step = steps[index];
        if (!step.RequiresGrab)
            return;
        if (Matches(grab.name, step.ToolNames))
            Advance();
    }

    void OnReleased(XRGrabInteractable grab)
    {
    }

    void OnStroke()
    {
        if (index >= steps.Count)
            return;
        if (steps[index].RequiresStroke)
            Advance();
    }

    void Advance()
    {
        index++;
        missingTimer = 0f;
        if (index >= steps.Count)
        {
            title.text = "Listo";
            body.text = "Recorriste la guía. Las herramientas siguen en la mesa y se agarran con el pellizco.";
            return;
        }

        Show(index);
    }

    void Show(int stepIndex)
    {
        Step step = steps[stepIndex];
        title.text = step.Title;
        string extra = "";
        if ((step.RequiresGrab || step.RequiresStroke) && !ToolExists(step))
            extra = "\n\nEsta herramienta no está en la escena. La guía sigue sola en unos segundos.";
        bool line = AnyActiveGuideLine();
        if (step.Title == "Incisión")
            extra += line ? "\n\nSeguí la línea." : "";
        body.text = step.Body + extra;
    }

    static bool AnyActiveGuideLine()
    {
        LineRenderer[] lines = FindObjectsByType<LineRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].enabled || !lines[i].gameObject.activeInHierarchy)
                continue;
            string name = lines[i].gameObject.name.ToLowerInvariant();
            if (name.Contains("spline") || name.Contains("linea") || name.Contains("línea") || name.Contains("path") || name.Contains("corte"))
                return true;
        }

        return false;
    }

    bool ToolExists(Step step)
    {
        if (step.ToolNames == null)
            return true;
        XRGrabInteractable[] grabs = FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < grabs.Length; i++)
        {
            if (Matches(grabs[i].name, step.ToolNames))
                return true;
        }

        return false;
    }

    static bool Matches(string objectName, string[] tokens)
    {
        if (tokens == null)
            return false;
        for (int i = 0; i < tokens.Length; i++)
        {
            if (objectName.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    static Step Basics(string title, string body, bool look = false, bool hands = false, bool pinch = false, bool release = false, bool panel = false)
    {
        return new Step
        {
            Title = title,
            Body = body,
            BasicsLook = look,
            BasicsHands = hands,
            BasicsPinch = pinch,
            BasicsRelease = release,
            BasicsReadPanel = panel
        };
    }

    static Step Tool(string title, string body, params string[] names)
    {
        return new Step
        {
            Title = title,
            Body = body,
            ToolNames = names,
            RequiresGrab = true
        };
    }

    static Step Stroke(string title, string body)
    {
        return new Step
        {
            Title = title,
            Body = body,
            ToolNames = new[] { "Fibron", "Fibrón", "Marker" },
            RequiresStroke = true
        };
    }
}
