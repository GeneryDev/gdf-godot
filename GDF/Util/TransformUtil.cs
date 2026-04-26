using Godot;

namespace GDF.Util;

public static class TransformUtil
{
    public static Quaternion RandomQuaternion(RandomNumberGenerator rand)
    {
        // From https://stackoverflow.com/a/44031492 (primary source is a dead link)
        float u = rand.Randf();
        float v = rand.Randf();
        float w = rand.Randf();

        return new Quaternion(Mathf.Sqrt(1 - u) * Mathf.Sin(Mathf.Tau * v),
            Mathf.Sqrt(1 - u) * Mathf.Cos(Mathf.Tau * v), Mathf.Sqrt(u) * Mathf.Sin(Mathf.Tau * w),
            Mathf.Sqrt(u) * Mathf.Cos(Mathf.Tau * w));
    }
}