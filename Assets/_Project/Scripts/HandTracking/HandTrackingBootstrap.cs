using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity.XR.CoreUtils;

public class HandTrackingBootstrap : MonoBehaviour
{
    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Pinch")]
    public float pinchOnMeters = 0.02f;
    public float pinchOffMeters = 0.04f;
    public float pinchGrabRadius = 0.04f;

    public LayerMask grabLayers = ~0;

    XROrigin origin;
    HandTrackingSpace space;
    bool meshesReady;
    HandJointVisual leftVisual;
    HandJointVisual rightVisual;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryBoot();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBoot();
    }

    static void TryBoot()
    {
        XROrigin origin = ActiveOrigin();
        if (origin == null)
            return;
        if (FindFirstObjectByType<HandTrackingBootstrap>() != null)
            return;

        HandGestureGrabber.ResetSignals();
        DestroyOrphanHands();
        var root = new GameObject("HandTracking");
        root.AddComponent<HandTrackingBootstrap>();
    }

    public static XRHandSubsystem RunningHands()
    {
        var list = new System.Collections.Generic.List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(list);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].running)
                return list[i];
        }

        return null;
    }

    static XROrigin ActiveOrigin()
    {
        XROrigin[] origins = FindObjectsByType<XROrigin>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        XROrigin fallback = null;
        for (int i = 0; i < origins.Length; i++)
        {
            if (origins[i] == null || !origins[i].isActiveAndEnabled)
                continue;
            if (fallback == null)
                fallback = origins[i];
            if (origins[i].Camera != null && origins[i].Camera.enabled)
                return origins[i];
        }

        return fallback;
    }

    static void DestroyOrphanHands()
    {
        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = all.Length - 1; i >= 0; i--)
        {
            if (all[i] == null)
                continue;
            string name = all[i].name;
            if (name != "Hand Interactor Left" && name != "Hand Interactor Right" &&
                name != "Hand Mesh Left" && name != "Hand Mesh Right" &&
                name != "Ghost Hand Left" && name != "Ghost Hand Right")
                continue;
            DestroyImmediate(all[i].gameObject);
        }
    }

    void Awake()
    {
        HandTrackingLog.Enabled = debugLogs;
        origin = ActiveOrigin();
        if (origin == null)
        {
            HandTrackingLog.Write("HandTracking", "No hay XR Origin en la escena.");
            return;
        }

        OrRoomSetup.Apply(origin);
        space = gameObject.AddComponent<HandTrackingSpace>();
        space.Configure(origin);
        XRInteractionManager manager = FindFirstObjectByType<XRInteractionManager>();
        CreateHand(manager, true);
        CreateHand(manager, false);
    }

    void Update()
    {
        if (meshesReady || space == null || origin == null)
            return;

        if (!space.TryGet(Vector3.zero, out Transform parent) || parent == null)
            return;

        InstallMesh(parent, true, leftVisual);
        InstallMesh(parent, false, rightVisual);
        meshesReady = true;
    }

    void CreateHand(XRInteractionManager manager, bool left)
    {
        var handObject = new GameObject(left ? "Hand Interactor Left" : "Hand Interactor Right");
        Transform parent = origin != null ? origin.transform : transform;
        handObject.transform.SetParent(parent, false);

        var collider = handObject.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.012f;

        var interactor = handObject.AddComponent<XRDirectInteractor>();
        if (manager != null)
            interactor.interactionManager = manager;

        var grabber = handObject.AddComponent<HandGestureGrabber>();
        grabber.Configure(this, left, interactor, space);

        var visual = handObject.AddComponent<HandJointVisual>();
        visual.Configure(space, left);
        if (left)
            leftVisual = visual;
        else
            rightVisual = visual;

        var pokeObject = new GameObject(left ? "Index Poke Left" : "Index Poke Right");
        pokeObject.transform.SetParent(handObject.transform, false);
        var pokeCollider = pokeObject.AddComponent<SphereCollider>();
        pokeCollider.isTrigger = true;
        pokeCollider.radius = 0.008f;
        var poke = pokeObject.AddComponent<XRPokeInteractor>();
        if (manager != null)
            poke.interactionManager = manager;
        pokeObject.AddComponent<IndexPokeDriver>().Configure(space, left);
    }

    void InstallMesh(Transform parent, bool left, HandJointVisual visual)
    {
        string resource = left ? "ShowableHands/LeftHand" : "ShowableHands/RightHand";
        GameObject prefab = Resources.Load<GameObject>(resource);

        if (prefab == null)
        {
            HandTrackingLog.Write("HandTracking", $"No está Resources/{resource}. Queda el esqueleto.");
            if (visual != null)
                visual.meshIsShowing = false;
            return;
        }

        GameObject instance = Instantiate(prefab, parent);
        instance.name = left ? "Hand Mesh Left" : "Hand Mesh Right";
        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            Destroy(colliders[i]);

        var skin = instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
        bool healthy = skin != null && skin.sharedMesh != null;
        if (visual != null)
            visual.meshIsShowing = healthy;
        instance.AddComponent<HandMeshWatch>().visual = visual;

        HandTrackingLog.Write("HandTracking",
            healthy
                ? $"{instance.name} instanciada con malla."
                : $"{instance.name} sin malla. El esqueleto queda de respaldo.");
    }
}

public class HandMeshWatch : MonoBehaviour
{
    public HandJointVisual visual;

    void LateUpdate()
    {
        if (visual == null)
            return;

        var skin = GetComponentInChildren<SkinnedMeshRenderer>(true);
        bool showing = skin != null && skin.sharedMesh != null && skin.enabled && skin.gameObject.activeInHierarchy;
        Camera camera = Camera.main;
        if (showing && camera != null && Vector3.Distance(skin.bounds.center, camera.transform.position) > 1.25f)
            showing = false;
        visual.meshIsShowing = showing;
    }
}

public class IndexPokeDriver : MonoBehaviour
{
    HandTrackingSpace trackingSpace;
    bool isLeftHand;
    UnityEngine.XR.Hands.XRHandSubsystem subsystem;

    public void Configure(HandTrackingSpace space, bool left)
    {
        trackingSpace = space;
        isLeftHand = left;
    }

    void Update()
    {
        if (subsystem == null || !subsystem.running)
        {
            subsystem = HandTrackingBootstrap.RunningHands();
            if (subsystem == null)
                return;
        }

        var hand = isLeftHand ? subsystem.leftHand : subsystem.rightHand;
        if (!hand.isTracked)
            return;

        if (!hand.GetJoint(UnityEngine.XR.Hands.XRHandJointID.IndexTip).TryGetPose(out Pose pose))
            return;

        if (trackingSpace == null || !trackingSpace.TryGet(pose.position, out Transform space))
            return;

        transform.SetPositionAndRotation(space.TransformPoint(pose.position), space.rotation * pose.rotation);
    }
}
