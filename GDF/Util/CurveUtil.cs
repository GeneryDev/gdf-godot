using Godot;

namespace GDF.Util;

public static class CurveUtil
{
    public static Curve3D CubicSmoothCurve(Curve3D curve, int subdivisions = 10)
    {
        int pointCount = curve.PointCount;
        if (pointCount <= 2) return curve;
        var newCurve = new Curve3D();

        for (int i = 0; i < curve.PointCount - 1; i += 1)
        {
            var point0 = curve.GetPointPosition(Mathf.Clamp(i-1, 0, pointCount - 1));
            var point1 = curve.GetPointPosition(Mathf.Clamp(i, 0, pointCount - 1));
            var point2 = curve.GetPointPosition(Mathf.Clamp(i+1, 0, pointCount - 1));
            var point3 = curve.GetPointPosition(Mathf.Clamp(i+2, 0, pointCount - 1));

            for (var sub = 0; sub < subdivisions; sub++)
            {
                float t = (float)sub / subdivisions;
                var pos = point1.CubicInterpolate(point2, point0, point3, t);

                newCurve.AddPoint(pos);
            }
        }
        newCurve.AddPoint(curve.GetPointPosition(pointCount-1), curve.GetPointIn(pointCount-1), curve.GetPointOut(pointCount-1));

        return newCurve;
    }
}