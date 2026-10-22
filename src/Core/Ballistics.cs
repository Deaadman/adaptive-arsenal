namespace AdaptiveArsenal.Core;

/// <summary>
/// The numbers behind each shot: accuracy, spread, muzzle velocity and damage.
/// </summary>
/// <remarks>
/// Everything in <c>src/Core</c> is plain C#, with nothing from Unity, the game or MelonLoader, so the tests in
/// <c>tests</c> can run it without the game. The test project compiles these files itself, so using a game type here
/// breaks the test build.
/// </remarks>
internal static class Ballistics
{
    /// <summary>The damage a bullet does at the muzzle, before the hit location is taken into account.</summary>
    internal const float MaxDamage = 100f;

    /// <summary>The damage a bullet does at <see cref="DamageFalloffRange"/> and beyond.</summary>
    internal const float MinDamage = 10f;

    /// <summary>The distance, in metres, over which damage falls from <see cref="MaxDamage"/> to <see cref="MinDamage"/>.</summary>
    internal const float DamageFalloffRange = 500f;

    /// <summary>Converts a value from <see cref="GetMuzzleVelocity"/> to a speed in metres per second.</summary>
    internal const float MuzzleVelocityScale = 0.5f;

    /// <summary>Every shot's accuracy is multiplied by this.</summary>
    internal const float BaseAccuracyMultiplier = 1.2f;

    /// <summary>Accuracy is multiplied by this when firing from the hip.</summary>
    internal const float HipFireAccuracyMultiplier = 0.7f;

    /// <summary>Accuracy is multiplied by this when standing rather than crouching.</summary>
    internal const float StandingAccuracyMultiplier = 0.8f;

    /// <summary>Accuracy is multiplied by this when moving.</summary>
    internal const float MovingAccuracyMultiplier = 0.9f;

    /// <summary>The accuracy at which a shot lands exactly where it was aimed.</summary>
    internal const float FullAccuracy = 100f;

    /// <summary>How far, in degrees, a shot with an accuracy of 0 can land from where it was aimed.</summary>
    internal const float MaxInaccuracyDegrees = 10f;

    /// <summary>The base accuracy of a revolver at each Revolver skill tier.</summary>
    private static readonly int[] RevolverAccuracyBySkillTier = [40, 50, 60, 80, 100];

    /// <summary>
    /// The muzzle velocity of each weapon, keyed by part of its gear name. Scaled by <see cref="MuzzleVelocityScale"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="GetMuzzleVelocity"/> uses the first entry whose name the gear name contains, so the more specific
    /// names come before <c>GEAR_Rifle</c> and <c>GEAR_Revolver</c>, which every rifle and revolver name contains. It's
    /// an array rather than a dictionary because the order matters.
    /// </remarks>
    private static readonly (string GearName, int Velocity)[] MuzzleVelocities =
    [
        ("GEAR_Rifle_Barbs", 800),
        ("GEAR_Rifle_Curators", 1000),
        ("GEAR_Rifle_Traders", 1100),
        ("GEAR_Rifle_Vaughns", 700),
        ("GEAR_Rifle", 800),
        ("GEAR_RevolverFancy", 600),
        ("GEAR_RevolverGreen", 400),
        ("GEAR_RevolverStubNosed", 300),
        ("GEAR_Revolver", 400)
    ];

    /// <summary>
    /// What a shot's base accuracy is multiplied by: <see cref="BaseAccuracyMultiplier"/>, times the multiplier for
    /// each of the conditions that apply.
    /// </summary>
    /// <param name="isHipFire">Whether the gun is fired from the hip, rather than aimed.</param>
    /// <param name="isStanding">Whether the player is standing, rather than crouching.</param>
    /// <param name="isMoving">Whether the player is moving.</param>
    internal static float AccuracyMultiplier(bool isHipFire, bool isStanding, bool isMoving)
    {
        var multiplier = BaseAccuracyMultiplier;

        if (isHipFire) multiplier *= HipFireAccuracyMultiplier;
        if (isStanding) multiplier *= StandingAccuracyMultiplier;
        if (isMoving) multiplier *= MovingAccuracyMultiplier;

        return multiplier;
    }

    /// <summary>A revolver's base accuracy at a Revolver skill tier.</summary>
    /// <param name="skillTier">The tier, from 0 for level 1 to 4 for level 5.</param>
    internal static float RevolverSkillAccuracy(int skillTier) => RevolverAccuracyBySkillTier[skillTier];

    /// <summary>
    /// How far, in degrees, a shot can land from where it was aimed in each direction: <see cref="MaxInaccuracyDegrees"/>
    /// at an accuracy of 0 or less, falling steadily to none at <see cref="FullAccuracy"/> or more.
    /// </summary>
    /// <param name="accuracy">The shot's accuracy.</param>
    internal static float MaxSpreadDegrees(float accuracy) => Lerp(0f, MaxInaccuracyDegrees, 1f - accuracy / FullAccuracy);

    /// <summary>
    /// The damage a bullet does after travelling <paramref name="distance"/> metres: <see cref="MaxDamage"/> at the
    /// muzzle, falling steadily to <see cref="MinDamage"/> at <see cref="DamageFalloffRange"/> and beyond.
    /// </summary>
    /// <param name="distance">How far the bullet travelled, in metres.</param>
    internal static float DamageAtDistance(float distance) =>
        Lerp(MaxDamage, MinDamage, Clamp01(distance / DamageFalloffRange));

    /// <summary>
    /// The muzzle velocity for a weapon, from the first entry in <see cref="MuzzleVelocities"/> whose name its gear name
    /// contains.
    /// </summary>
    /// <param name="gearItemName">The weapon's gear name, which may have a suffix like <c>(Clone)</c>.</param>
    /// <returns>The muzzle velocity, or 0 for a weapon that isn't listed.</returns>
    internal static int GetMuzzleVelocity(string gearItemName) =>
        MuzzleVelocities.FirstOrDefault(entry => gearItemName.Contains(entry.GearName)).Velocity;

    /// <summary>The same as Unity's <c>Mathf.Clamp01</c>.</summary>
    private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;

    /// <summary>The same as Unity's <c>Mathf.Lerp</c>, which clamps <paramref name="t"/> to 0 to 1.</summary>
    private static float Lerp(float from, float to, float t) => from + (to - from) * Clamp01(t);
}
