using Godot;

namespace GDF.Util;

public static class Vector3Extensions
{
    public static void DecomposeAlongAxis(this Vector3 vec, Vector3 axis, out Vector3 orthogonal, out Vector3 parallel)
    {
        var plane = new Plane(axis);
        orthogonal = plane.Project(vec);
        parallel = vec - orthogonal;
    }
    public static void DecomposeAlongAxis(this Vector3 vec, Vector3 axis, out Vector3 orthogonal, out float parallel)
    {
        var plane = new Plane(axis);
        orthogonal = plane.Project(vec);
        parallel = plane.DistanceTo(vec);
    }
}