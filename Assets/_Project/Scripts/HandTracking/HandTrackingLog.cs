using UnityEngine;

public static class HandTrackingLog
{
    public static bool Enabled = true;

    public static void Write(string category, string message)
    {
        if (!Enabled)
            return;

        Debug.Log($"[{category}] {message}");
    }
}
