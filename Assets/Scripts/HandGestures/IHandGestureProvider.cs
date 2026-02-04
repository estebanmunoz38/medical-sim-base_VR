using System;

public interface IHandGestureProvider
{
    float Pinch { get; }
    float SecondaryPinch { get; }
    bool IsPinching { get; }
    bool IsGrasping { get; }
    float Pressure { get; }
    bool IsStable { get; }
    float Precision { get; }
    
    event Action OnSecondaryActivated;
    event Action OnSecondaryDeactivated;

    void UpdatePrecision(float distance);
}