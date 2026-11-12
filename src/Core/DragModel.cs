using Vec3 = System.Numerics.Vector3;

namespace AdaptiveArsenal.Core;

/// <summary>
/// How the air slows a projectile: the standard G1 drag curve with a ballistic coefficient, or a fixed share of its
/// speed each second like a Unity rigidbody's drag.
/// </summary>
/// <remarks>
/// <para>
/// Real drag grows with the square of the speed, and changes sharply around the speed of sound. The G1 curve gives the
/// drag of a standard flat-based, round-nosed bullet at each Mach number, and the ballistic coefficient says how much
/// less a real bullet slows than that standard one. Published coefficients are in pounds per square inch, as here.
/// </para>
/// <para>
/// Linear drag is how the mod's bullets flew before, as a rigidbody with drag. Shots fired through the API without a
/// ballistic coefficient still use it, and <see cref="Flight.Step"/> moves them the way a rigidbody would, so they fly
/// as they did.
/// </para>
/// <para>Make one with <see cref="G1"/> or <see cref="Linear"/>, so it's always one or the other.</para>
/// </remarks>
internal readonly record struct DragModel
{
    /// <summary>Makes a drag model. Use <see cref="G1"/> or <see cref="Linear"/>, which set only the one that applies.</summary>
    /// <param name="ballisticCoefficient">The G1 ballistic coefficient, or 0 for linear drag.</param>
    /// <param name="linearDrag">The linear drag, or 0 for G1 drag.</param>
    private DragModel(float ballisticCoefficient, float linearDrag)
    {
        BallisticCoefficient = ballisticCoefficient;
        LinearDrag = linearDrag;
    }

    /// <summary>The G1 ballistic coefficient, in lb/in², or 0 for linear drag.</summary>
    internal float BallisticCoefficient { get; }

    /// <summary>
    /// The share of its speed the projectile loses each second, like a rigidbody's drag, or 0 for G1 drag.
    /// </summary>
    internal float LinearDrag { get; }

    /// <summary>How many kg/m³ one lb/in² is, to use a published ballistic coefficient in metric units.</summary>
    private const float KilogramsPerSquareMetrePerPoundPerSquareInch = 703.0696f;

    /// <summary>
    /// The G1 drag curve: the drag coefficient of the standard bullet at each Mach number, in increasing Mach order.
    /// </summary>
    /// <remarks>The standard table that ballistics programs use, between which <see cref="G1DragCoefficient"/> interpolates.</remarks>
    private static readonly (float Mach, float DragCoefficient)[] G1Curve =
    [
        (0.00f, 0.2629f), (0.05f, 0.2558f), (0.10f, 0.2487f), (0.15f, 0.2413f), (0.20f, 0.2344f),
        (0.25f, 0.2278f), (0.30f, 0.2214f), (0.35f, 0.2155f), (0.40f, 0.2104f), (0.45f, 0.2061f),
        (0.50f, 0.2032f), (0.55f, 0.2020f), (0.60f, 0.2034f), (0.70f, 0.2165f), (0.725f, 0.2230f),
        (0.75f, 0.2313f), (0.775f, 0.2417f), (0.80f, 0.2546f), (0.825f, 0.2706f), (0.85f, 0.2901f),
        (0.875f, 0.3136f), (0.90f, 0.3415f), (0.925f, 0.3734f), (0.95f, 0.4084f), (0.975f, 0.4448f),
        (1.00f, 0.4805f), (1.025f, 0.5136f), (1.05f, 0.5427f), (1.075f, 0.5677f), (1.10f, 0.5883f),
        (1.125f, 0.6053f), (1.15f, 0.6191f), (1.20f, 0.6393f), (1.25f, 0.6518f), (1.30f, 0.6589f),
        (1.35f, 0.6621f), (1.40f, 0.6625f), (1.45f, 0.6607f), (1.50f, 0.6573f), (1.55f, 0.6528f),
        (1.60f, 0.6474f), (1.65f, 0.6413f), (1.70f, 0.6347f), (1.75f, 0.6280f), (1.80f, 0.6210f),
        (1.85f, 0.6141f), (1.90f, 0.6072f), (1.95f, 0.6003f), (2.00f, 0.5934f), (2.05f, 0.5867f),
        (2.10f, 0.5804f), (2.15f, 0.5743f), (2.20f, 0.5685f), (2.25f, 0.5630f), (2.30f, 0.5577f),
        (2.35f, 0.5527f), (2.40f, 0.5481f), (2.45f, 0.5438f), (2.50f, 0.5397f), (2.60f, 0.5325f),
        (2.70f, 0.5264f), (2.80f, 0.5211f), (2.90f, 0.5168f), (3.00f, 0.5133f), (3.10f, 0.5105f),
        (3.20f, 0.5084f), (3.30f, 0.5067f), (3.40f, 0.5054f), (3.50f, 0.5040f), (3.60f, 0.5030f),
        (3.70f, 0.5022f), (3.80f, 0.5016f), (3.90f, 0.5010f), (4.00f, 0.5006f), (4.20f, 0.4998f),
        (4.40f, 0.4995f), (4.60f, 0.4992f), (4.80f, 0.4990f), (5.00f, 0.4988f)
    ];

    /// <summary>
    /// Drag from the G1 curve, for a bullet with this ballistic coefficient. 0 or less gives linear drag of 0: no drag.
    /// </summary>
    /// <param name="ballisticCoefficient">The G1 ballistic coefficient, in lb/in².</param>
    internal static DragModel G1(float ballisticCoefficient) => new(MathF.Max(0f, ballisticCoefficient), 0f);

    /// <summary>Drag that takes a fixed share of the speed each second, like a Unity rigidbody's.</summary>
    /// <param name="drag">The share of its speed lost each second. Less than 0 counts as 0.</param>
    internal static DragModel Linear(float drag) => new(0f, MathF.Max(0f, drag));

    /// <summary>Whether this uses the G1 curve, rather than linear drag.</summary>
    internal bool IsG1 => BallisticCoefficient > 0f;

    /// <summary>The drag model for a log, like <c>G1 drag, BC 0.40</c> or <c>linear drag 0.10</c>.</summary>
    public override string ToString() =>
        IsG1 ? FormattableString.Invariant($"G1 drag, BC {BallisticCoefficient:F2}") : FormattableString.Invariant($"linear drag {LinearDrag:F2}");

    /// <summary>
    /// The acceleration G1 drag gives a projectile, in m/s², against its velocity through the air. Linear drag gives
    /// none here, since <see cref="Flight.Step"/> applies it the way a rigidbody does instead.
    /// </summary>
    /// <param name="airVelocity">The projectile's velocity through the air, in m/s: its velocity less the wind's.</param>
    /// <param name="atmosphere">The air, whose density and speed of sound set the drag.</param>
    internal Vec3 Acceleration(Vec3 airVelocity, Atmosphere atmosphere)
    {
        if (!IsG1)
            return Vec3.Zero;

        var speed = airVelocity.Length();
        if (speed <= 0f)
            return Vec3.Zero;

        // Drag is ρv²·Cd·A/2m, with A = πd²/4. The bullet's Cd is i times the G1 curve's, and the coefficient is
        // m/(i·d²), so the deceleration is ρv²·Cd(G1)·π/(8·BC).
        var deceleration = atmosphere.Density * speed * speed * G1DragCoefficient(atmosphere.Mach(speed)) * MathF.PI
                           / (8f * BallisticCoefficient * KilogramsPerSquareMetrePerPoundPerSquareInch);
        return -airVelocity / speed * deceleration;
    }

    /// <summary>
    /// The G1 drag coefficient at a Mach number, interpolated between the points of <see cref="G1Curve"/>, and held at
    /// its ends outside them.
    /// </summary>
    /// <param name="mach">The speed as a multiple of the speed of sound.</param>
    internal static float G1DragCoefficient(float mach)
    {
        if (mach <= G1Curve[0].Mach)
            return G1Curve[0].DragCoefficient;

        for (var i = 1; i < G1Curve.Length; i++)
        {
            var (upperMach, upperCoefficient) = G1Curve[i];
            if (mach > upperMach)
                continue;

            var (lowerMach, lowerCoefficient) = G1Curve[i - 1];
            var t = (mach - lowerMach) / (upperMach - lowerMach);
            return lowerCoefficient + (upperCoefficient - lowerCoefficient) * t;
        }

        return G1Curve[^1].DragCoefficient;
    }
}
