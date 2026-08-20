using System;
using UnityEngine;

public enum TutorialPlayMode
{
    Tutorial,
    Paused,
    Free
}

public enum TutorialTargetKind
{
    None,
    InstrumentTable,
    PatientField,
    AnyGrabbable,
    Shave,
    Marker,
    Scalpel,
    ScalpelNextPoint,
    Retractor,
    RetractorSnap,
    Dissection,
    DissectionHalo,
    Drill,
    DrillSnap,
    Endoscope,
    EndoscopeScreen,
    EndoscopeUnlock,
    Kerrison,
    Coagulator,
    CoagPath,
    Hemostatic,
    Suture,
    SuturePoint,
    SutureHelper,
    Plasty,
    PlastyPath,
    ClearCol
}

public enum TutorialCompleteWhen
{
    None,
    TimeoutOnly,
    LookedAround,
    ControllersMoved,
    GrabbedAny,
    GrabbedTarget,
    LookedOrGrabbedTarget,
    MovedHeldTarget,
    ReleasedTarget,
    TriggerWhileHoldingTarget,
    PressedSecondary,
    LookedAtTarget,
    ShaveComplete,
    MarkerPainted,
    IncisionComplete,
    RetractorAttached,
    RetractorOpened,
    DissectionOnFontanelle,
    DissectionComplete,
    DrillSucceeded,
    EndoscopeActivated,
    EndoscopeDepthLow,
    EndoscopeDepthWork,
    KerrisonHolding,
    KerrisonDeposited,
    CoagulationDone,
    HemostasisComplete,
    SutureComplete,
    PlastyComplete,
    HemostaticPlaced,
    EnterFreeMode
}

[Serializable]
public class TutorialStepConfig
{
    public string id;
    public string title;
    [TextArea(1, 3)] public string instruction;
    [TextArea(1, 4)] public string detail;
    public string[] hints;
    public TutorialTargetKind target = TutorialTargetKind.None;
    public string markerLabel;
    public TutorialCompleteWhen completeWhen = TutorialCompleteWhen.None;
    public bool allowTimeout;
    [Min(0f)] public float timeoutSeconds = 12f;
    [Min(0f)] public float delayBeforeNext = 0.8f;
    public AudioClip narration;
    public AudioClip instructionSound;
    public AudioClip successSound;
    public AudioClip errorSound;
    public bool skipIfTargetMissing = true;
}

[Serializable]
public class TutorialModuleConfig
{
    public string id;
    public string title;
    [TextArea(1, 3)] public string description;
    public TutorialStepConfig[] steps;
}
