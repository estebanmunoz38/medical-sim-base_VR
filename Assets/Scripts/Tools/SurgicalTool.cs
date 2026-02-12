using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SurgicalTool : MonoBehaviour
{
    #region Fields
    [Header("Grab Settings")]
    public float releasePinchThreshold = 0.2f;
    public float minHoldTime = 0.15f;
    
    protected IHandGestureProvider activeGestures;
    protected XRGrabInteractable grab;
    
    private float holdTimer;
    private bool isLatched;
    private bool allowRelease;
    private bool isRotationLimited => isXRotationLimited || isYRotationLimited;
    private bool isXRotationLimited;
    private bool isYRotationLimited;
    private float minAngleX, maxAngleX, minAngleY, maxAngleY;
    private float referenceRotation;
    private Quaternion initialLocalRotation;
    private float baseAngleX, baseAngleY;
    private Quaternion grabRotationOffset;
    private Quaternion initialRotationAtLimit;
    private Vector3 initialLocalEuler;
    public IHandGestureProvider ActiveGestures => activeGestures;
    #endregion
    
    #region Unity Methods
    protected virtual void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        initialLocalRotation = transform.localRotation;
        if (!grab)
        {
            Debug.LogError($"{name} requires XRGrabInteractable");
            enabled = false;
        }
    }
    
    protected virtual void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleaseAttempt);
        
    }
    
    protected virtual void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleaseAttempt);
        
        if (activeGestures != null)
        {
            activeGestures.OnSecondaryActivated -= OnToolActivated;
            activeGestures.OnSecondaryDeactivated -= OnToolDeactivated;
        }
    }

    protected void Update()
    {
        if (!isLatched || activeGestures == null)
            return;

        holdTimer += Time.deltaTime;

        if (holdTimer > minHoldTime &&
            activeGestures.Pinch < releasePinchThreshold)
        {
            allowRelease = true;
            ForceRelease();
        }
    }

    protected void LateUpdate()
    {
    }

    #endregion
    
    #region Private Methods
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // El interactor que agarró el objeto
        var interactorGO = args.interactorObject.transform;

        activeGestures = interactorGO.GetComponentInParent<IHandGestureProvider>();
        grabRotationOffset = Quaternion.Inverse(args.interactorObject.transform.rotation) * transform.rotation;
        activeGestures.OnSecondaryActivated += OnToolActivated;
        activeGestures.OnSecondaryDeactivated += OnToolDeactivated;
        
        // Fallback si no hay gesture provider
        if (activeGestures == null)
        {
            return;
        }
        
        holdTimer = 0f;
        isLatched = true;
        allowRelease = false;
        
        OnToolGrabbed();
    }

    private void OnReleaseAttempt(SelectExitEventArgs args)
    {
        // Si no permitimos soltar, simplemente ignoramos
        if (!allowRelease)
        {
            // Volvemos a marcar el estado interno
            isLatched = true;
            return;
        }

        // Release real
        isLatched = false;
        activeGestures.OnSecondaryActivated -= OnToolActivated;
        activeGestures.OnSecondaryDeactivated -= OnToolDeactivated;
        activeGestures = null;
        
        OnToolReleased();
    }

    private void ForceRelease()
    {
        if (grab.firstInteractorSelecting == null)
        {
            isLatched = false;
            activeGestures.OnSecondaryActivated -= OnToolActivated;
            activeGestures.OnSecondaryDeactivated -= OnToolDeactivated;
            activeGestures = null;
            OnToolReleased();
            return;
        }

        isLatched = false;

        grab.interactionManager.SelectExit(
            grab.firstInteractorSelecting,
            grab
        );
        
        activeGestures = null;
        OnToolReleased();
    }

    private void OnToolActivated()
    {
        grab.activated.Invoke(new ActivateEventArgs());
    }

    private void OnToolDeactivated()
    {
        grab.deactivated.Invoke(new DeactivateEventArgs());
    }
    
    private float NormalizeAngle(float angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }
    
    // Hooks para hijos
    protected virtual void OnToolGrabbed() { }
    protected virtual void OnToolReleased() { }
    #endregion
    
    #region Public Methods

    public void LockPosition(bool locked)
    {
        var rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.constraints = locked ? RigidbodyConstraints.FreezePosition : RigidbodyConstraints.None;
    }
    
    public void LockPosition(bool lockPositionX, bool lockPositionY, bool lockPositionZ)
    {
        ConfigurableJoint oldJoint = GetComponent<ConfigurableJoint>();

        if (oldJoint != null)
        {
            // 1. Quitar el viejo
            Destroy(GetComponent<ConfigurableJoint>());
        }
        // 2. Crear el nuevo (se inicializa con la pose actual)
        ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();

        

        joint.xMotion = lockPositionX ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
        joint.yMotion = lockPositionY ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
        joint.zMotion = lockPositionZ ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
    }
    
    public void LockRotation(ConfigurableJointMotion lockRotationX, ConfigurableJointMotion lockRotationY, ConfigurableJointMotion lockRotationZ)
    {
        ConfigurableJoint oldJoint = GetComponent<ConfigurableJoint>();

        if (oldJoint != null)
        {
            // 1. Quitar el viejo
            Destroy(GetComponent<ConfigurableJoint>());
        }
        // 2. Crear el nuevo (se inicializa con la pose actual)
        ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
        
        if (lockRotationX != joint.angularXMotion)
        {
            joint.angularXMotion = lockRotationX;
        }

        if (lockRotationY != joint.angularYMotion)
        {
            joint.angularYMotion =  lockRotationY;
        }

        if (lockRotationZ != joint.angularZMotion)
        {
            joint.angularZMotion =  lockRotationZ;
        }
    }

    public void LimitRotation(ConfigurableJointMotion lockRotationX, ConfigurableJointMotion lockRotationY, ConfigurableJointMotion lockRotationZ, float max)
    {
        ConfigurableJoint oldJoint = GetComponent<ConfigurableJoint>();

        if (oldJoint != null)
        {
            DestroyImmediate(GetComponent<ConfigurableJoint>());
        }
        // 2. Crear el nuevo (se inicializa con la pose actual)
        ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();

        //joint.axis = Vector3.right; 
        //joint.secondaryAxis = Vector3.up;
        //joint.configuredInWorldSpace = false;
        
        if (lockRotationX == ConfigurableJointMotion.Limited)
        {
            // Configuramos el límite inferior (debe ser negativo generalmente)
            SoftJointLimit lowLimitX = new SoftJointLimit();
            lowLimitX.limit = -max; 
            joint.lowAngularXLimit = lowLimitX;

            // Configuramos el límite superior (positivo)
            SoftJointLimit highLimitX = new SoftJointLimit();
            highLimitX.limit = max;
            joint.highAngularXLimit = highLimitX;
        }

        if (lockRotationY == ConfigurableJointMotion.Limited)
        {
            joint.axis = Vector3.right; 
            joint.secondaryAxis = Vector3.up;
            joint.configuredInWorldSpace = false;
            SoftJointLimit angularLimitY = new SoftJointLimit();
            angularLimitY.limit = max;
            joint.angularYLimit = angularLimitY;
        }

        if (lockRotationZ == ConfigurableJointMotion.Limited)
        {
            SoftJointLimit angularLimitZ = new SoftJointLimit();
            angularLimitZ.limit = max;
            joint.angularZLimit = angularLimitZ;
        }

        // Activamos el movimiento limitado
        joint.angularXMotion = lockRotationX;
        joint.angularYMotion = lockRotationY;
        joint.angularZMotion = lockRotationZ;
    
    }

    public void LimiteYRotation(float min, float max)
    {
        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();
    
        /*SoftJointLimit lowLimitY = new SoftJointLimit();
        lowLimitY.limit = min;
        joint.lowAngularXLimit = lowLimitY;*/
        SoftJointLimit maxLimitY = new SoftJointLimit();
        maxLimitY.limit = max;
        joint.angularYLimit = maxLimitY;
        //joint.angularYMotion = ConfigurableJointMotion.Limited;
        
        isYRotationLimited = true;
    }
    
    public void LimitarRotacionFisica(float rangoX, float rangoY)
    {
        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();
        if (joint == null) joint = gameObject.AddComponent<ConfigurableJoint>();

        // Configuramos los límites
        if (rangoX > 0)
        {
            SoftJointLimit limitX = new SoftJointLimit();
            limitX.limit = rangoX; // Unity usa el mismo valor para min/max en este modo
            joint.highAngularXLimit = limitX;
            joint.angularXMotion = ConfigurableJointMotion.Limited;
        }

        if (rangoY > 0)
        {
            SoftJointLimit limitY = new SoftJointLimit();
            limitY.limit = rangoY;
            joint.angularYLimit = limitY;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
        }
        
    }

    public void ClearLimits()
    {
        isXRotationLimited = false;
        isYRotationLimited = false;
        ConfigurableJoint joint = GetComponent<ConfigurableJoint>();
        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Free;
    }
    #endregion
}
