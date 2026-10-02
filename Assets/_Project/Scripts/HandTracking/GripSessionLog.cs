using System.IO;
using System.Text;
using UnityEngine;

public enum GripMark : byte
{
    Grab = 1,
    UserRelease = 2,
    UnexpectedRelease = 3,
    TrackingLoss = 4,
    TrackingRecovered = 5,
    Reacquisition = 6,
    Outlier = 7,
    FingerOcclusion = 8
}

public static class GripSessionLog
{
    const int SlotCount = 4;
    const int EventCount = 64;
    const string FileName = "SimugiasGripSession.txt";

    struct SlotData
    {
        public bool used;
        public bool hasData;
        public int id;
        public string name;
        public int grabs;
        public int userReleases;
        public int unexpectedReleases;
        public int trackingLosses;
        public int trackingRecoveries;
        public int reacquisitions;
        public int outliers;
        public int fingerOcclusions;
        public float maxPositionMm;
        public float maxAngle;
        public float sumPositionMm;
        public float sumAngle;
        public int samples;
    }

    struct MarkEvent
    {
        public float time;
        public GripMark kind;
        public byte slot;
        public byte hand;
    }

    static readonly SlotData[] Slots = new SlotData[SlotCount];
    static readonly MarkEvent[] Events = new MarkEvent[EventCount];
    static int listeners;
    static int eventHead;
    static int eventCount;
    static bool hooked;

    public static int Listeners => listeners;
    public static string LastPath { get; private set; }

    public static void Attach(ToolHandGrip grip)
    {
        if (grip == null)
            return;

        EnsureHook();
        int id = grip.GetInstanceID();
        if (Find(id) >= 0)
            return;

        if (listeners == 0)
            Clear();

        int slot = FreeSlot();
        if (slot < 0)
            return;

        Slots[slot].used = true;
        Slots[slot].id = id;
        Slots[slot].name = grip.name;
        listeners++;
    }

    public static void Detach(ToolHandGrip grip)
    {
        if (grip == null)
            return;

        int slot = Find(grip.GetInstanceID());
        if (slot < 0)
            return;

        Slots[slot].used = false;
        listeners--;
        if (listeners > 0)
            return;

        listeners = 0;
        Flush();
        Clear();
    }

    public static void Mark(ToolHandGrip grip, GripMark kind, bool leftHand)
    {
        if (listeners == 0 || grip == null)
            return;

        int slot = Find(grip.GetInstanceID());
        if (slot < 0)
            return;

        Slots[slot].hasData = true;
        switch (kind)
        {
            case GripMark.Grab: Slots[slot].grabs++; break;
            case GripMark.UserRelease: Slots[slot].userReleases++; break;
            case GripMark.UnexpectedRelease: Slots[slot].unexpectedReleases++; break;
            case GripMark.TrackingLoss: Slots[slot].trackingLosses++; break;
            case GripMark.TrackingRecovered: Slots[slot].trackingRecoveries++; break;
            case GripMark.Reacquisition: Slots[slot].reacquisitions++; break;
            case GripMark.Outlier: Slots[slot].outliers++; break;
            case GripMark.FingerOcclusion: Slots[slot].fingerOcclusions++; break;
        }

        Events[eventHead] = new MarkEvent
        {
            time = Time.realtimeSinceStartup,
            kind = kind,
            slot = (byte)slot,
            hand = (byte)(leftHand ? 1 : 2)
        };
        eventHead++;
        if (eventHead >= EventCount)
            eventHead = 0;
        if (eventCount < EventCount)
            eventCount++;
    }

    public static void Sample(ToolHandGrip grip, float positionMm, float angleDegrees)
    {
        if (listeners == 0 || grip == null)
            return;

        int slot = Find(grip.GetInstanceID());
        if (slot < 0)
            return;

        Slots[slot].hasData = true;
        Slots[slot].samples++;
        Slots[slot].sumPositionMm += positionMm;
        Slots[slot].sumAngle += angleDegrees;
        if (positionMm > Slots[slot].maxPositionMm)
            Slots[slot].maxPositionMm = positionMm;
        if (angleDegrees > Slots[slot].maxAngle)
            Slots[slot].maxAngle = angleDegrees;
    }

    public static string Summary()
    {
        var text = new StringBuilder(1024);
        text.Append("Simugias grip session\n");
        bool any = false;
        for (int i = 0; i < SlotCount; i++)
        {
            if (!Slots[i].hasData && !Slots[i].used)
                continue;
            any = true;
            AppendSlot(text, i);
        }

        if (!any)
            text.Append("Sin eventos.\n");

        if (eventCount > 0)
        {
            text.Append("\neventos:\n");
            int start = eventCount < EventCount ? 0 : eventHead;
            for (int i = 0; i < eventCount; i++)
            {
                int index = start + i;
                if (index >= EventCount)
                    index -= EventCount;
                MarkEvent mark = Events[index];
                string tool = mark.slot < SlotCount ? Slots[mark.slot].name : "?";
                string hand = mark.hand == 1 ? "izquierda" : "derecha";
                text.Append(mark.time.ToString("0.00"));
                text.Append("  ");
                text.Append(tool);
                text.Append("  ");
                text.Append(hand);
                text.Append("  ");
                text.Append(KindName(mark.kind));
                text.Append('\n');
            }
        }

        return text.ToString();
    }

    public static void Flush()
    {
        bool any = false;
        for (int i = 0; i < SlotCount; i++)
        {
            if (Slots[i].hasData)
            {
                any = true;
                break;
            }
        }

        if (!any)
            return;

        string path = Path.Combine(Application.persistentDataPath, FileName);
        try
        {
            File.WriteAllText(path, Summary());
            LastPath = path;
            HandTrackingLog.Write("Grip", "Resumen de agarre: " + path);
        }
        catch (IOException)
        {
            HandTrackingLog.Write("Grip", "No pude guardar el resumen de agarre.");
        }
    }

    static void AppendSlot(StringBuilder text, int index)
    {
        SlotData slot = Slots[index];
        text.Append('\n');
        text.Append(string.IsNullOrEmpty(slot.name) ? "Herramienta" : slot.name);
        text.Append('\n');
        Line(text, "agarradas", slot.grabs);
        Line(text, "sueltas por gesto", slot.userReleases);
        Line(text, "sueltas no pedidas", slot.unexpectedReleases);
        Line(text, "perdidas de tracking", slot.trackingLosses);
        Line(text, "recuperaciones", slot.trackingRecoveries);
        Line(text, "reacquisiones con blend", slot.reacquisitions);
        Line(text, "oclusiones de dedos", slot.fingerOcclusions);
        Line(text, "muestras descartadas", slot.outliers);
        text.Append("error posicion max mm: ");
        text.Append(slot.maxPositionMm.ToString("0.0"));
        text.Append('\n');
        text.Append("error angulo max grados: ");
        text.Append(slot.maxAngle.ToString("0.0"));
        text.Append('\n');
        float avgPos = slot.samples > 0 ? slot.sumPositionMm / slot.samples : 0f;
        float avgAng = slot.samples > 0 ? slot.sumAngle / slot.samples : 0f;
        text.Append("error posicion medio mm: ");
        text.Append(avgPos.ToString("0.0"));
        text.Append('\n');
        text.Append("error angulo medio grados: ");
        text.Append(avgAng.ToString("0.0"));
        text.Append('\n');
    }

    static void Line(StringBuilder text, string label, int value)
    {
        text.Append(label);
        text.Append(": ");
        text.Append(value);
        text.Append('\n');
    }

    static string KindName(GripMark kind)
    {
        switch (kind)
        {
            case GripMark.Grab: return "agarre";
            case GripMark.UserRelease: return "suelta";
            case GripMark.UnexpectedRelease: return "suelta no pedida";
            case GripMark.TrackingLoss: return "tracking perdido";
            case GripMark.TrackingRecovered: return "tracking recuperado";
            case GripMark.Reacquisition: return "reacquisicion";
            case GripMark.Outlier: return "muestra descartada";
            case GripMark.FingerOcclusion: return "dedos ocluidos";
            default: return "otro";
        }
    }

    static int Find(int id)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (Slots[i].used && Slots[i].id == id)
                return i;
        }

        return -1;
    }

    static int FreeSlot()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (!Slots[i].used && !Slots[i].hasData)
                return i;
        }

        return -1;
    }

    static void Clear()
    {
        for (int i = 0; i < SlotCount; i++)
            Slots[i] = default;
        eventHead = 0;
        eventCount = 0;
    }

    static void EnsureHook()
    {
        if (hooked)
            return;
        hooked = true;
        Application.quitting += Flush;
    }
}
