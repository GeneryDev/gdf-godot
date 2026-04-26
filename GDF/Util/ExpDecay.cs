using System.Runtime.CompilerServices;
using Godot;

namespace GDF.Util;

public static class ExpDecay
{
    private const float LnHalf = -0.69314718055994530941723212145818f;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetDecayOverTime(float t)
    {
        return 1.0f - Mathf.Exp(LnHalf * t);
    }
    
    // float
    public static float LerpOverTime(float from, float to, float t)
    {
        return Mathf.Lerp(from, to, GetDecayOverTime(t));
    }
    public static float LerpAngleOverTime(float from, float to, float t)
    {
        return Mathf.LerpAngle(from, to, GetDecayOverTime(t));
    }
    
    // double
    public static double LerpOverTime(double from, double to, double t)
    {
        return Mathf.Lerp(from, to, GetDecayOverTime((float)t));
    }
    public static double LerpAngleOverTime(double from, double to, double t)
    {
        return Mathf.LerpAngle(from, to, GetDecayOverTime((float)t));
    }
    
    // Vector2
    public static Vector2 LerpOverTime(Vector2 from, Vector2 to, float t)
    {
        return from.Lerp(to, GetDecayOverTime(t));
    }
    public static Vector2 SlerpOverTime(Vector2 from, Vector2 to, float t)
    {
        return from.Slerp(to, GetDecayOverTime(t));
    }
    // Vector3
    public static Vector3 LerpOverTime(Vector3 from, Vector3 to, float t)
    {
        return from.Lerp(to, GetDecayOverTime(t));
    }
    public static Vector3 SlerpOverTime(Vector3 from, Vector3 to, float t)
    {
        return from.Slerp(to, GetDecayOverTime(t));
    }
    
    // Color
    public static Color LerpOverTime(Color from, Color to, float t)
    {
        return from.Lerp(to, GetDecayOverTime(t));
    }
    
    // Quaternion
    public static Quaternion SlerpOverTime(Quaternion from, Quaternion to, float t, bool closestPath = true)
    {
        return closestPath
            ? from.Slerp(to, GetDecayOverTime(t))
            : from.Slerpni(to, GetDecayOverTime(t));
    }
}