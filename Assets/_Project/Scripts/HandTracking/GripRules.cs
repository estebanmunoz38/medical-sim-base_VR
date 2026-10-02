using UnityEngine;

public static class GripRules
{
    public static bool StepPinch(bool held, float distance, float pinchOn, float pinchOff)
    {
        if (!held && distance <= pinchOn)
            return true;
        if (held && distance >= pinchOff)
            return false;
        return held;
    }

    public static void StepLimits(float maxLinearSpeed, float maxAngularSpeed, float dt, out float stepLimit, out float angleLimit)
    {
        float time = dt > 0.005f ? dt : 0.005f;
        stepLimit = (maxLinearSpeed > 0.01f ? maxLinearSpeed : 0.01f) * time;
        float angular = maxAngularSpeed > 20f ? maxAngularSpeed : 20f;
        angleLimit = angular * time;
    }

    public static bool PoseStepPlausible(float stepMeters, float angleDegrees, float maxLinearSpeed, float maxAngularSpeed, float dt)
    {
        StepLimits(maxLinearSpeed, maxAngularSpeed, dt, out float stepLimit, out float angleLimit);
        return stepMeters <= stepLimit && angleDegrees <= angleLimit;
    }

    public static bool ReleaseImmediatelyOnLoss(bool driving, float releaseTimer)
    {
        return driving && releaseTimer > 0f;
    }

    public static bool HoldDuringLoss(bool driving, float lostSeconds, float graceSeconds)
    {
        return driving && lostSeconds < graceSeconds;
    }
}
