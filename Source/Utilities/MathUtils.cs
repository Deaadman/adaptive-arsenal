namespace AdaptiveArsenal.Utilities;

internal static class MathUtils
{
    internal static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        var u = 1 - t;
        var tt = t * t;
        var uu = u * u;
        return uu * p0 + 2 * u * t * p1 + tt * p2;
    }
}