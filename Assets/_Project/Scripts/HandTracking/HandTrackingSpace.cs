using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

public class HandTrackingSpace : MonoBehaviour
{
    XROrigin xrOrigin;
    Transform origin;
    Transform cameraOffset;
    Transform resolved;

    public Transform Origin => origin;

    public void Configure(XROrigin originComponent)
    {
        xrOrigin = originComponent;
        origin = originComponent.transform;
        cameraOffset = originComponent.CameraFloorOffsetObject != null
            ? originComponent.CameraFloorOffsetObject.transform
            : origin;
    }

    public bool TryGet(Vector3 palmLocal, out Transform space)
    {
        if (resolved != null)
        {
            space = resolved;
            return true;
        }

        space = null;
        if (xrOrigin == null)
            return false;

        TrackingOriginModeFlags mode = xrOrigin.CurrentTrackingOriginMode;
        if (mode == TrackingOriginModeFlags.Unknown)
            return false;

        bool floor = mode == TrackingOriginModeFlags.Floor;
        resolved = floor || cameraOffset == null ? origin : cameraOffset;
        space = resolved;

        string distance = "sin cámara";
        if (xrOrigin.Camera != null)
        {
            float meters = Vector3.Distance(space.TransformPoint(palmLocal), xrOrigin.Camera.transform.position);
            distance = $"{meters:0.00}m";
        }

        HandTrackingLog.Write("HandTracking",
            $"Espacio de las manos={space.name}. Modo={mode}. Palma a {distance} de la cámara.");
        return true;
    }
}
