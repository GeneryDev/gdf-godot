using Godot;

namespace GDF.Util;

public static class RandomNumberGeneratorExtension
{
    /// <summary>
    /// <para>Returns either a positive or negative 1 with equal probability.</para>
    /// </summary>
    public static int RandSign(this RandomNumberGenerator rand)
    {
        return rand.Randf() >= 0.5f ? 1 : -1;
    }
}