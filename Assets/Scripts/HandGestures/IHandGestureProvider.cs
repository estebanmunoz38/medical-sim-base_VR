public interface IHandGestureProvider
{
    float Pinch { get; }
    bool IsPinching { get; }
    bool IsGrasping { get; }
    float Pressure { get; }
    bool IsStable { get; }
    float Precision { get; }

    void UpdatePrecision(float distance);
}