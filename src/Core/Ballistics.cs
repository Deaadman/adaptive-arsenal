namespace AdaptiveArsenal.Core;

/// <summary>
/// The numbers behind each shot: accuracy, spread, muzzle velocity and damage.
/// </summary>
/// <remarks>
/// Everything in <c>src/Core</c> is plain C#, with nothing from Unity, the game or MelonLoader, so the tests in
/// <c>tests</c> can run it without the game. The test project compiles these files itself, so using a game type here
/// breaks the test build.
///
/// The numbers players can change in the mod's settings are properties, which <c>Settings</c> sets. Their defaults
/// are the mod's own values, which the tests check.
/// </remarks>
internal static class Ballistics
{
    /// <summary>The damage a bullet does at the muzzle when its weapon doesn't say, before the hit location is taken into account.</summary>
    internal const float DefaultMuzzleDamage = 100f;

    /// <summary>
    /// How much a bullet's speed when it hits sets its damage, from 0 to 1: at 1 the damage is its share of the
    /// muzzle energy, at 0 it's the full muzzle damage however slow the bullet is, and between them it's a blend.
    /// </summary>
    internal static float DamageFromSpeed { get; set; } = 1f;

    /// <summary>
    /// The share of its muzzle damage a projectile with linear drag still does at <see cref="LinearDragFalloffRange"/>
    /// and beyond: 1.1.0's default, which shots fired through the API with linear drag keep.
    /// </summary>
    private const float LinearDragMinDamageFraction = 0.1f;

    /// <summary>
    /// The distance, in metres, over which a projectile with linear drag falls to
    /// <see cref="LinearDragMinDamageFraction"/> of its muzzle damage: 1.1.0's default.
    /// </summary>
    private const float LinearDragFalloffRange = 500f;

    /// <summary>Every shot's accuracy is multiplied by this.</summary>
    internal static float BaseAccuracyMultiplier { get; set; } = 1.2f;

    /// <summary>Accuracy is multiplied by this when firing from the hip.</summary>
    internal static float HipFireAccuracyMultiplier { get; set; } = 0.7f;

    /// <summary>Accuracy is multiplied by this when standing rather than crouching.</summary>
    internal static float StandingAccuracyMultiplier { get; set; } = 0.8f;

    /// <summary>Accuracy is multiplied by this when moving.</summary>
    internal static float MovingAccuracyMultiplier { get; set; } = 0.9f;

    /// <summary>The accuracy at which a shot lands exactly where it was aimed.</summary>
    internal const float FullAccuracy = 100f;

    /// <summary>
    /// How far, in degrees, a shot with an accuracy of 0 can land from where it was aimed, on top of the gun's own
    /// spread.
    /// </summary>
    internal static float MaxInaccuracyDegrees { get; set; } = DefaultMaxInaccuracyDegrees;

    /// <summary>The default for <see cref="MaxInaccuracyDegrees"/>.</summary>
    internal const float DefaultMaxInaccuracyDegrees = 3f;

    /// <summary>
    /// How far, in degrees, a rifle's shots can land from where they were aimed even at full accuracy: 2 MOA, about
    /// the spread of a real hunting rifle.
    /// </summary>
    internal const float RifleSpreadFloorDegrees = 2f / 60f;

    /// <summary>
    /// How far, in degrees, a revolver's shots can land from where they were aimed even at full accuracy: 5 MOA, as
    /// a revolver's short barrel and sights spread its shots more than a rifle's.
    /// </summary>
    internal const float RevolverSpreadFloorDegrees = 5f / 60f;

    /// <summary>
    /// How much of the spread circle's radius the spread's standard deviation is, in <see cref="SpreadOffset"/>: half,
    /// so the circle's edge is two standard deviations out.
    /// </summary>
    private const float SpreadDeviationShare = 0.5f;

    /// <summary>
    /// How much muzzle velocity changes for each °C the powder is from <see cref="Atmosphere.StandardTemperature"/>, as a
    /// share of it: colder powder burns slower. 0.07% a degree is about 0.5 m/s for a rifle at 740 m/s, typical of
    /// hunting cartridges, which differ a lot.
    /// </summary>
    private const float PowderTemperatureSensitivity = 0.0007f;

    /// <summary>The lowest share of its muzzle velocity the cold can leave a shot, so a broken reading can't stop it.</summary>
    private const float MinPowderTemperatureFactor = 0.5f;

    /// <summary>Every weapon's muzzle velocity is multiplied by this.</summary>
    internal static float MuzzleVelocityMultiplier { get; set; } = 1f;

    /// <summary>The base accuracy of a revolver at each Revolver skill tier.</summary>
    private static readonly int[] RevolverAccuracyBySkillTier = [40, 50, 60, 80, 100];

    /// <summary>
    /// The muzzle velocity of each weapon, in metres per second, keyed by part of its gear name.
    /// </summary>
    /// <remarks>
    /// <see cref="GetMuzzleVelocity"/> uses the first entry whose name the gear name contains, so the more specific
    /// names come before <c>GEAR_Rifle</c> and <c>GEAR_Revolver</c>, which every rifle and revolver name contains. It's
    /// an array rather than a dictionary because the order matters.
    ///
    /// The speeds are about those of real rifles and revolvers like the game's. Guns firing the same ammo differ mostly
    /// through the length of their barrels, so the stub-nosed revolver is the slowest.
    /// </remarks>
    private static readonly (string GearName, float Velocity)[] MuzzleVelocities =
    [
        ("GEAR_Rifle_Barbs", 740f),
        ("GEAR_Rifle_Curators", 780f),
        ("GEAR_Rifle_Traders", 820f),
        ("GEAR_Rifle_Vaughns", 700f),
        ("GEAR_Rifle", 740f),
        ("GEAR_RevolverFancy", 330f),
        ("GEAR_RevolverGreen", 270f),
        ("GEAR_RevolverStubNosed", 240f),
        ("GEAR_Revolver", 270f)
    ];

    /// <summary>
    /// The G1 ballistic coefficient of each kind of ammo's bullet, in lb/in², keyed by part of its gear name.
    /// </summary>
    /// <remarks>
    /// The bullet is the same whichever gun fires it, so this goes by the ammo, while muzzle velocity goes by the gun.
    /// The game doesn't say what calibres its guns fire, so these are typical of a round-nosed .303 or .30-06 hunting
    /// bullet and a lead .38 revolver bullet.
    /// </remarks>
    private static readonly (string GearName, float BallisticCoefficient)[] BallisticCoefficients =
    [
        ("GEAR_RifleAmmo", 0.40f),
        ("GEAR_RevolverAmmo", 0.15f)
    ];

    /// <summary>The distance, in metres, at which rifle shots cross the line of sight.</summary>
    internal const float RifleZeroDistance = 100f;

    /// <summary>The distance, in metres, at which revolver shots cross the line of sight.</summary>
    internal const float RevolverZeroDistance = 25f;

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
    /// How far, in degrees, a shot can land from where it was aimed: the gun's own <paramref name="floorDegrees"/>, plus
    /// <see cref="MaxInaccuracyDegrees"/> at an accuracy of 0 or less, falling steadily to nothing more at
    /// <see cref="FullAccuracy"/> or more.
    /// </summary>
    /// <param name="accuracy">The shot's accuracy.</param>
    /// <param name="floorDegrees">
    /// The gun's own spread, which even a perfect shooter gets, like <see cref="RifleSpreadFloorDegrees"/>.
    /// </param>
    internal static float MaxSpreadDegrees(float accuracy, float floorDegrees) =>
        MathF.Max(0f, floorDegrees) + Lerp(0f, MaxInaccuracyDegrees, 1f - accuracy / FullAccuracy);

    /// <summary>
    /// Turns two random numbers into how far a shot lands from where it was aimed, within a circle of
    /// <paramref name="maxSpread"/> degrees, bunched towards the middle as real shots are.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Real shots scatter in a normal distribution around the point of aim, so their distance from it follows a
    /// Rayleigh distribution. This picks the distance from one cut off at the circle's edge, which is two standard
    /// deviations out, so no shot lands further out than <paramref name="maxSpread"/>. About 45% of shots land within
    /// half the radius, where an even spread puts 25%.
    /// </para>
    /// <para>
    /// The distance comes from the Rayleigh distribution's inverse: <c>σ√(−2 ln(1 − p))</c>, with <c>p</c> scaled so
    /// that 1 lands exactly on the edge.
    /// </para>
    /// </remarks>
    /// <param name="maxSpread">The radius of the circle, in degrees, from <see cref="MaxSpreadDegrees"/>.</param>
    /// <param name="random1">A random number from 0 to 1, which picks the direction.</param>
    /// <param name="random2">A random number from 0 to 1, which picks how far out.</param>
    /// <returns>How far to turn the shot up or down (<c>Pitch</c>) and left or right (<c>Yaw</c>), in degrees.</returns>
    internal static (float Pitch, float Yaw) SpreadOffset(float maxSpread, float random1, float random2)
    {
        if (maxSpread <= 0f)
            return (0f, 0f);

        var direction = 2f * MathF.PI * random1;
        var deviation = maxSpread * SpreadDeviationShare;

        // The share of the whole distribution inside the circle, so random2 = 1 maps to its edge.
        var insideCircle = 1f - MathF.Exp(-0.5f / (SpreadDeviationShare * SpreadDeviationShare));
        var distance = deviation * MathF.Sqrt(-2f * MathF.Log(1f - Clamp01(random2) * insideCircle));
        return (distance * MathF.Sin(direction), distance * MathF.Cos(direction));
    }

    /// <summary>
    /// A muzzle velocity changed for the powder's temperature: lower in the cold, as slower-burning powder pushes the
    /// bullet less, and a little higher in the warm.
    /// </summary>
    /// <param name="muzzleVelocity">The muzzle velocity at <see cref="Atmosphere.StandardTemperature"/>, in m/s.</param>
    /// <param name="celsius">The powder's temperature, taken to be the air's, in °C.</param>
    internal static float MuzzleVelocityAtTemperature(float muzzleVelocity, float celsius) =>
        muzzleVelocity * MathF.Max(MinPowderTemperatureFactor,
            1f + PowderTemperatureSensitivity * (celsius - Atmosphere.StandardTemperature));

    /// <summary>
    /// Turns two random numbers into how far a shot lands from where it was aimed, anywhere within a circle of
    /// <paramref name="maxSpread"/> degrees, with every point in the circle as likely as any other, for shots fired
    /// through the API, like a shotgun's pellets.
    /// </summary>
    /// <remarks>
    /// Picking the two angles separately would spread shots over a square, whose corners are further out than its
    /// sides. The square root of the distance keeps shots from bunching up in the middle of the circle.
    /// </remarks>
    /// <param name="maxSpread">The radius of the circle, in degrees.</param>
    /// <param name="random1">A random number from 0 to 1, which picks the direction.</param>
    /// <param name="random2">A random number from 0 to 1, which picks how far out.</param>
    /// <returns>How far to turn the shot up or down (<c>Pitch</c>) and left or right (<c>Yaw</c>), in degrees.</returns>
    internal static (float Pitch, float Yaw) EvenSpreadOffset(float maxSpread, float random1, float random2)
    {
        var direction = 2f * MathF.PI * random1;
        var distance = MathF.Max(0f, maxSpread) * MathF.Sqrt(Clamp01(random2));
        return (distance * MathF.Sin(direction), distance * MathF.Cos(direction));
    }

    /// <summary>
    /// The damage a projectile does when it hits: by its speed for G1 drag, see <see cref="DamageAtSpeed"/>, and by the
    /// distance it travelled for linear drag, see <see cref="DamageAtDistance"/>.
    /// </summary>
    /// <remarks>
    /// Linear drag barely slows a projectile, so damage by speed would hardly fall with range. Shots fired through the
    /// API with linear drag keep the falloff by distance they had in 1.1.0 instead.
    /// </remarks>
    /// <param name="drag">How the air slowed the projectile.</param>
    /// <param name="lowestSpeed">The slowest it went before it hit, in metres per second.</param>
    /// <param name="muzzleSpeed">How fast it left the muzzle, in metres per second.</param>
    /// <param name="distance">How far it travelled, in metres.</param>
    /// <param name="muzzleDamage">The weapon's damage, or 0 or less for <see cref="DefaultMuzzleDamage"/>.</param>
    internal static float DamageOnHit(DragModel drag, float lowestSpeed, float muzzleSpeed, float distance, float muzzleDamage) =>
        drag.IsG1 ? DamageAtSpeed(lowestSpeed, muzzleSpeed, muzzleDamage) : DamageAtDistance(distance, muzzleDamage);

    /// <summary>
    /// The damage a projectile with linear drag does after travelling <paramref name="distance"/> metres: its muzzle
    /// damage, falling steadily to <see cref="LinearDragMinDamageFraction"/> of it at
    /// <see cref="LinearDragFalloffRange"/> and beyond, as in 1.1.0. <see cref="DamageFromSpeed"/> blends it towards the
    /// full damage, as it does damage by speed, so 0 turns every drop-off off.
    /// </summary>
    /// <param name="distance">How far the projectile travelled, in metres.</param>
    /// <param name="muzzleDamage">The weapon's damage, or 0 or less for <see cref="DefaultMuzzleDamage"/>.</param>
    internal static float DamageAtDistance(float distance, float muzzleDamage)
    {
        if (muzzleDamage <= 0f)
            muzzleDamage = DefaultMuzzleDamage;

        var share = Lerp(1f, LinearDragMinDamageFraction, distance / LinearDragFalloffRange);
        return muzzleDamage * Lerp(1f, share, DamageFromSpeed);
    }

    /// <summary>
    /// The <see cref="DamageFromSpeed"/> that best matches 1.1.0's Long-range damage setting, for carrying a player's
    /// choice over: its default of 0.1 becomes 1, full drop-off, and 1, no drop-off, becomes 0.
    /// </summary>
    /// <param name="longRangeDamage">The share of its damage a bullet kept at long range, from 0 to 1.</param>
    internal static float DamageFromSpeedForLongRangeDamage(float longRangeDamage) =>
        Clamp01((1f - longRangeDamage) / (1f - LinearDragMinDamageFraction));

    /// <summary>
    /// The damage a bullet does when it hits after slowing to <paramref name="impactSpeed"/>:
    /// <paramref name="muzzleDamage"/> times the share of its muzzle energy it still has,
    /// <c>(impact speed ÷ muzzle speed)²</c>, blended towards the full damage by <see cref="DamageFromSpeed"/>.
    /// </summary>
    /// <remarks>
    /// A real bullet's effect depends on its energy when it hits, which goes with the square of its speed. A bullet that
    /// speeds up, like one fired steeply downhill, does no more than the full damage.
    /// </remarks>
    /// <param name="impactSpeed">
    /// How fast the bullet was going, in metres per second. Pass the slowest it went before it hit, so speed gravity
    /// adds late in a long, high arc doesn't raise the damage again.
    /// </param>
    /// <param name="muzzleSpeed">
    /// How fast it left the muzzle, in metres per second. 0 or less does the full damage, for a bullet with no speed to
    /// compare against.
    /// </param>
    /// <param name="muzzleDamage">
    /// The weapon's damage. <see cref="DefaultMuzzleDamage"/> is used if it's 0 or less, for a weapon that doesn't say.
    /// </param>
    internal static float DamageAtSpeed(float impactSpeed, float muzzleSpeed, float muzzleDamage)
    {
        if (muzzleDamage <= 0f)
            muzzleDamage = DefaultMuzzleDamage;

        if (muzzleSpeed <= 0f)
            return muzzleDamage;

        var speedShare = Clamp01(impactSpeed / muzzleSpeed);
        return muzzleDamage * Lerp(1f, speedShare * speedShare, DamageFromSpeed);
    }

    /// <summary>
    /// The muzzle velocity for a weapon, in metres per second, from the first entry in <see cref="MuzzleVelocities"/>
    /// whose name its gear name contains, times <see cref="MuzzleVelocityMultiplier"/>.
    /// </summary>
    /// <param name="gearItemName">The weapon's gear name, which may have a suffix like <c>(Clone)</c>.</param>
    /// <returns>The muzzle velocity, or 0 for a weapon that isn't listed.</returns>
    internal static float GetMuzzleVelocity(string gearItemName) =>
        MuzzleVelocities.FirstOrDefault(entry => gearItemName.Contains(entry.GearName)).Velocity * MuzzleVelocityMultiplier;

    /// <summary>
    /// The G1 ballistic coefficient of an ammo's bullet, from the first entry in <see cref="BallisticCoefficients"/>
    /// whose name its gear name contains.
    /// </summary>
    /// <param name="ammoGearName">The ammo's gear name, like <c>GEAR_RifleAmmoSingle</c>.</param>
    /// <returns>The coefficient, or 0 for ammo that isn't listed.</returns>
    internal static float GetBallisticCoefficient(string ammoGearName) =>
        BallisticCoefficients.FirstOrDefault(entry => ammoGearName.Contains(entry.GearName)).BallisticCoefficient;

    /// <summary>The same as Unity's <c>Mathf.Clamp01</c>.</summary>
    private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;

    /// <summary>The same as Unity's <c>Mathf.Lerp</c>, which clamps <paramref name="t"/> to 0 to 1.</summary>
    private static float Lerp(float from, float to, float t) => from + (to - from) * Clamp01(t);
}
