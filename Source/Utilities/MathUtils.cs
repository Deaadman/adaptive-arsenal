namespace AdaptiveArsenal.Utilities;

internal static class MathUtils
{
    /// <summary>
    /// Calculates a point on a quadratic Bezier curve based on the specified control points and interpolation
    /// parameter.
    /// </summary>
    /// <param name="p0">The starting point of the curve.</param>
    /// <param name="p1">The control point that influences the curve's shape.</param>
    /// <param name="p2">The ending point of the curve.</param>
    /// <param name="t">The interpolation parameter, where 0 represents <paramref name="p0"/> and 1 represents <paramref name="p2"/>.
    /// Must be in the range [0, 1].</param>
    /// <returns>The calculated <see cref="Vector3"/> point on the quadratic Bezier curve at the specified interpolation
    /// parameter <paramref name="t"/>.</returns>
    internal static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        var u = 1 - t;
        var tt = t * t;
        var uu = u * u;
        return uu * p0 + 2 * u * t * p1 + tt * p2;
    }
}