using Godot;

namespace GDF.Util;

public struct CurveWalker
{
    private const float CurveXTolerance = 0.01f;
    private const float CurveYTolerance = 0.01f;
    
    public Curve Curve;
    public float X;
    public float Y;

    public CurveWalker(Curve curve, float x = 0)
    {
        X = x;
        SetCurveKeepX(curve);
    }

    public void SetCurveKeepX(Curve curve)
    {
        if (Curve == curve) return;
        float x = X;
        Curve = curve;
        SetX(x);
    }

    public void SetCurveKeepY(Curve curve)
    {
        if (Curve == curve) return;
        float y = Y;
        Curve = curve;
        SetY(y);
    }

    public void SetX(float x)
    {
        X = x;
        Y = Sample(x);
    }

    public void SetY(float y)
    {
        X = SampleInverse(y);
        Y = Sample(X);
    }

    public float Sample(float x)
    {
        return Curve?.SampleBaked(x) ?? 0;
    }

    public void StepX(float dx)
    {
        SetX(Mathf.Clamp(X + dx, Curve.MinDomain, Curve.MaxDomain));
    }

    public void StepY(float dy)
    {
        SetY(Mathf.Clamp(Y + dy, Curve.MinValue, Curve.MaxValue));
    }
    
    public void StepXTowardY(float dx, float targetY)
    {
        if (Mathf.Abs(Y - targetY) < CurveYTolerance)
        {
            Y = targetY;
            return;
        }
        
        int diffYSign = Mathf.Sign(targetY - Y);

        StepX(dx);
        if (Mathf.Sign(targetY - Y) == -diffYSign)
        {
            // overshot, snap to target
            SetY(targetY);
        }
    }
    
    public void StepYTowardX(float dy, float targetX)
    {
        if (Mathf.Abs(X - targetX) < CurveXTolerance)
        {
            X = targetX;
            return;
        }
        
        int diffXSign = Mathf.Sign(targetX - X);

        StepY(dy);
        if (Mathf.Sign(targetX - X) == -diffXSign)
        {
            // overshot, snap to target
            SetX(targetX);
        }
    }
    
    /// <summary>
    /// Given a curve, and a desired Y position, finds and returns an X value such that Curve(X) approximately equals Y.
    /// The curve is assumed to be a 1:1 function (if not, the first match will be returned),
    /// and that the wanted output exists within the domain of the curve (if not, the closest end point will be returned).
    /// </summary>
    public float SampleInverse(float y)
    {
        if (Curve == null)
        {
            GD.PrintErr("No curve is set!");
            return 0;
        }
        int pointCount = Curve.GetPointCount();

        for (var i = 0; i < pointCount - 1; i++)
        {
            var leftPoint = Curve.GetPointPosition(i);
            var rightPoint = Curve.GetPointPosition(i+1);

            if (Mathf.Abs(y - leftPoint.Y) < CurveYTolerance) return leftPoint.X;
            if (Mathf.Abs(y - rightPoint.Y) < CurveYTolerance) return rightPoint.X;

            float minY = Mathf.Min(leftPoint.Y, rightPoint.Y);
            float maxY = Mathf.Max(leftPoint.Y, rightPoint.Y);

            if (minY <= y && y <= maxY) // Y is within this range
                return SampleInverseInRange(y, leftPoint.X, rightPoint.X);
        }
        
        // Outside the value range
        float minCurveY = Curve.MinValue;
        float maxCurveY = Curve.MaxValue;
        float diffFromCurveStart = Mathf.Abs(y - Sample(minCurveY));
        float diffFromCurveEnd = Mathf.Abs(y - Sample(maxCurveY));
        return diffFromCurveStart < diffFromCurveEnd ? minCurveY : maxCurveY;
    }

    private float SampleInverseInRange(float y, float minX, float maxX)
    {
        var remainingIterations = 20;
        while (remainingIterations-- > 0)
        {
            float midX = (minX + maxX) / 2.0f;
            float midY = Sample(midX);
            
            if (Mathf.Abs(y - midY) < CurveYTolerance) return midX;

            float yAtMinX = Sample(minX);
            if (Mathf.Abs(y - yAtMinX) < CurveYTolerance) return minX;
            
            float yAtMaxX = Sample(maxX);
            if (Mathf.Abs(y - yAtMaxX) < CurveYTolerance) return maxX;

            if ((y < midY) == (yAtMaxX > yAtMinX))
                maxX = midX;
            else
                minX = midX;
        }
        GD.PrintErr("Too many iterations in SampleInverse!");
        return 0;
    }
}