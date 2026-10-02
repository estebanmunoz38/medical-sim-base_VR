using UnityEditor;
using UnityEngine;

public static class GripLogicChecks
{
    [MenuItem("Simugias/Probar lógica de agarre")]
    public static void Run()
    {
        int failed = 0;
        failed += Expect(PinchDoesNotOscillate(), "histéresis");
        failed += Expect(OutlierDoesNotPass(), "outlier");
        failed += Expect(ShortLossKeepsOwner(), "gracia");
        failed += Expect(FilterMatchesAcrossRates(), "filtro 72/90");
        failed += Expect(FilterDoesNotDrift(), "filtro sin drift");
        if (failed == 0)
            Debug.Log("Agarre: las 5 pruebas de lógica pasaron.");
        else
            Debug.LogError("Agarre: fallaron " + failed + " pruebas de lógica.");
    }

    static int Expect(bool ok, string name)
    {
        if (ok)
            return 0;
        Debug.LogError("Falló la prueba de " + name + ".");
        return 1;
    }

    static bool PinchDoesNotOscillate()
    {
        const float on = 0.02f;
        const float off = 0.04f;
        bool held = false;
        float[] around = { 0.03f, 0.025f, 0.035f, 0.021f, 0.039f };
        for (int i = 0; i < around.Length; i++)
        {
            held = GripRules.StepPinch(held, around[i], on, off);
            if (held)
                return false;
        }

        held = GripRules.StepPinch(held, 0.02f, on, off);
        if (!held)
            return false;

        held = GripRules.StepPinch(held, 0.03f, on, off);
        held = GripRules.StepPinch(held, 0.039f, on, off);
        if (!held)
            return false;

        held = GripRules.StepPinch(held, 0.04f, on, off);
        if (held)
            return false;

        held = GripRules.StepPinch(held, 0.03f, on, off);
        return !held;
    }

    static bool OutlierDoesNotPass()
    {
        const float dt = 1f / 72f;
        const float linear = 6.5f;
        const float angular = 800f;
        if (!GripRules.PoseStepPlausible(0.02f, 5f, linear, angular, dt))
            return false;
        if (GripRules.PoseStepPlausible(0.5f, 5f, linear, angular, dt))
            return false;
        if (GripRules.PoseStepPlausible(0.02f, 80f, linear, angular, dt))
            return false;

        GripRules.StepLimits(linear, angular, dt, out float stepLimit, out float angleLimit);
        if (!GripRules.PoseStepPlausible(stepLimit, angleLimit, linear, angular, dt))
            return false;
        return !GripRules.PoseStepPlausible(stepLimit + 0.001f, 0f, linear, angular, dt);
    }

    static bool ShortLossKeepsOwner()
    {
        if (!GripRules.HoldDuringLoss(true, 0f, 0.16f))
            return false;
        if (!GripRules.HoldDuringLoss(true, 0.15f, 0.16f))
            return false;
        if (GripRules.HoldDuringLoss(true, 0.16f, 0.16f))
            return false;
        if (GripRules.HoldDuringLoss(false, 0f, 0.16f))
            return false;
        if (!GripRules.ReleaseImmediatelyOnLoss(true, 0.01f))
            return false;
        return !GripRules.ReleaseImmediatelyOnLoss(true, 0f);
    }

    static bool FilterMatchesAcrossRates()
    {
        float at72 = Settle(72f, 0.08f);
        float at90 = Settle(90f, 0.08f);
        float at120 = Settle(120f, 0.08f);
        return Mathf.Abs(at72 - at90) < 0.06f &&
               Mathf.Abs(at72 - at120) < 0.06f &&
               at72 > 0.65f && at90 > 0.65f && at120 > 0.65f &&
               at72 < 0.92f;
    }

    static bool FilterDoesNotDrift()
    {
        var filter = new OneEuroVector3();
        Vector3 hold = new Vector3(0.2f, -0.05f, 0.1f);
        filter.Reset(hold);
        Vector3 value = hold;
        for (int i = 0; i < 90; i++)
            value = filter.Filter(hold, 1f / 90f, 3.1f, 12f, 0f);
        return (value - hold).sqrMagnitude < 0.0000001f;
    }

    static float Settle(float rate, float seconds)
    {
        var filter = new OneEuroVector3();
        filter.Reset(Vector3.zero);
        float dt = 1f / rate;
        int steps = Mathf.RoundToInt(seconds * rate);
        Vector3 value = Vector3.zero;
        for (int i = 0; i < steps; i++)
            value = filter.Filter(Vector3.one, dt, 3.1f, 0f, 0f);
        return value.x;
    }
}
