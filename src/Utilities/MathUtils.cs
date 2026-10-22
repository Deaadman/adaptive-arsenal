namespace AdaptiveArsenal.Utilities;

/// <summary>
/// Maths the game's own libraries don't provide.
/// </summary>
internal static class MathUtils
{
    /// <summary>
    /// Gets a point on a quadratic Bézier curve.
    /// </summary>
    /// <param name="start">Where the curve starts.</param>
    /// <param name="control">The control point, which the curve bends towards but doesn't pass through.</param>
    /// <param name="end">Where the curve ends.</param>
    /// <param name="t">How far along the curve, from 0 at <paramref name="start"/> to 1 at <paramref name="end"/>.</param>
    /// <returns>The point at <paramref name="t"/>.</returns>
    internal static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        var u = 1 - t;
        return u * u * start + 2 * u * t * control + t * t * end;
    }
}
