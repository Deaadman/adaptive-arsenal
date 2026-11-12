using AdaptiveArsenal.Api;
using AdaptiveArsenal.Core;
using AdaptiveArsenal.Utilities;

using Il2CppTLD.Stats;

namespace AdaptiveArsenal.Components;

/// <summary>
/// A bullet fired from a rifle or revolver, or by another mod through <see cref="Projectiles.Fire"/>. It flies through
/// the world under gravity and drag, instead of hitting instantly like the game's own shots, and draws a trail behind it.
/// </summary>
/// <remarks>
/// <see cref="ProjectileTemplates"/> builds an inactive template with this component for each kind of ammo, and
/// <see cref="Patches.vp_FPSShooterPatches"/> fires a copy of it with
/// <see cref="SpawnAndFire(GameObject, GunItem, Vector3, Quaternion, ShotWeather)"/> for each shot. When it hits something, it stops
/// and hides, and lingers for <see cref="ImpactLingerDuration"/> while its trail fades and its impact sound plays.
///
/// The bullet has no rigidbody: <see cref="Flight.Step"/> moves it each physics step, so its drag can grow with its
/// speed, and its sights can be zeroed with the same flight.
/// </remarks>
[RegisterTypeInIl2Cpp(false)]
public class ProjectileItem : MonoBehaviour
{
    /// <summary>The layer of an animal's own object, which has the <see cref="BaseAi"/>.</summary>
    private const int AiLayer = 16;

    /// <summary>The layer of a collider on part of an animal's body, whose parent has the <see cref="BaseAi"/>.</summary>
    private const int AiBodyPartLayer = 27;

    /// <summary>
    /// The layers the bullet looks for ahead of it each physics step, besides animals, worked out the first time it's
    /// needed: see <see cref="HitLayerMask"/>.
    /// </summary>
    private static int? s_HitLayerMask;

    /// <summary>
    /// The layer of animals' own objects, which the bullet only looks for when <see cref="HitLayerMask"/> finds
    /// nothing. Their colliders are thought to surround the body parts, so looking for both at once would hit these
    /// first and miss the body part's damage scaling.
    /// </summary>
    private const int AiLayerMask = 1 << AiLayer;

    /// <summary>How often, in seconds, the bullet's position is added to the trail.</summary>
    private const float TrailUpdateInterval = 0.05f;

    /// <summary>
    /// How long, in seconds, the bullet stays after it hits something: its trail fades out over this time, and its
    /// impact sound, which plays from it, has this long to finish.
    /// </summary>
    private const float ImpactLingerDuration = 2f;

    /// <summary>
    /// How long, in seconds, a bullet can fly without hitting anything before it's removed, like one fired into the
    /// sky. The slowest bullet, at 150 m/s, covers about 1.5 km in this time.
    /// </summary>
    private const float MaxFlightTime = 10f;

    /// <summary>The linear drag of a bullet from a gun whose ammo has no ballistic coefficient, as the mod's bullets had before.</summary>
    private const float DefaultDrag = 0.1f;

    /// <summary>How far off a surface, in metres, a bullet that glanced off it starts its next step.</summary>
    private const float RicochetClearance = 0.01f;

    /// <summary>How many trail points to make room for up front.</summary>
    private const int InitialTrailCapacity = 100;

    /// <summary>The arrow's trail material, which every bullet's trail uses, looked up the first time it's needed.</summary>
    private static Material? s_TrailMaterial;

    /// <summary>The rifle or revolver that fired the bullet, or <see langword="null"/> for one another mod fired.</summary>
    private GunType? m_GunType;

    /// <summary>
    /// The damage the bullet does at the muzzle: the gun's <see cref="GunItem.m_DamageHP"/>, or
    /// <see cref="ProjectileShot.Damage"/>.
    /// </summary>
    private float m_MuzzleDamage;

    /// <summary>The weapon animals take the hit from.</summary>
    private WeaponSource m_WeaponSource;

    /// <summary>
    /// The shot the bullet is part of, for one another mod fired with <see cref="ProjectileShot.OnShotFinished"/>: it
    /// collects what the bullet hits instead of the bullet damaging it. <see langword="null"/> otherwise.
    /// </summary>
    private ProjectileGroup<ProjectileHit>? m_Group;

    /// <summary>Whether the bullet plays the game's impact effects where it hits.</summary>
    private bool m_PlayImpactEffects = true;

    /// <summary>Whether the bullet has told its <see cref="m_Group"/> it hit or missed, so it only does once.</summary>
    private bool m_Finished;

    /// <summary>
    /// The bullet's velocity, in metres per second, which <see cref="FixedUpdate"/> moves it by. After a hit, it's the
    /// velocity where the bullet hit.
    /// </summary>
    private Vector3 m_Velocity;

    /// <summary>How the air slows the bullet.</summary>
    private DragModel m_Drag;

    /// <summary>The air the bullet flies through, read from the weather when it's fired.</summary>
    private Atmosphere m_Atmosphere;

    /// <summary>Where the bullet was fired from, to work out how far it has travelled when it hits.</summary>
    private Vector3 m_FirePosition;

    /// <summary>How fast the bullet left the muzzle, in metres per second, to compare its speed against when it hits.</summary>
    private float m_MuzzleSpeed;

    /// <summary>
    /// The slowest the bullet has gone, in metres per second, which sets its damage, so speed gravity adds late in a long,
    /// high arc doesn't raise it again.
    /// </summary>
    private float m_LowestSpeed;

    /// <summary>When the bullet was fired, to remove it after <see cref="MaxFlightTime"/>.</summary>
    private float m_FireTime;

    /// <summary>Whether the bullet has hit something, so it only hits once.</summary>
    private bool m_HasHit;

    /// <summary>How many times the bullet has glanced off something, up to <see cref="Ricochet.MaxRicochets"/>.</summary>
    private int m_Ricochets;

    /// <summary>How long, in seconds, since the bullet hit something.</summary>
    private float m_TimeSinceHit;

    /// <summary>Whether the bullet logs every physics step, from <see cref="ProjectileDebug.LogSteps"/> when it's fired.</summary>
    private bool m_LogSteps;

    /// <summary>
    /// The bullet's drawn path, if <see cref="ProjectileDebug.DrawPaths"/> was on when it was fired, or
    /// <see langword="null"/>.
    /// </summary>
    private ProjectilePath? m_Path;

    private LineRenderer? m_Trail;
    private List<Vector3>? m_TrailPoints;
    private float m_LastTrailUpdateTime;

    /// <summary>
    /// Works out how accurate a shot is: higher is more accurate. At 100 or more, a shot lands exactly where it was
    /// aimed.
    /// </summary>
    /// <param name="gunItem">The gun being fired.</param>
    /// <param name="isHipFire">Whether the gun is fired from the hip, rather than aimed.</param>
    /// <param name="isStanding">Whether the player is standing, rather than crouching.</param>
    /// <param name="isMoving">Whether the player is moving.</param>
    /// <returns>
    /// The rifle's skill-based range, or the revolver's skill-based accuracy plus its own, multiplied by
    /// <see cref="Ballistics.AccuracyMultiplier"/>.
    /// </returns>
    public static float CalculateAccuracy(GunItem gunItem, bool isHipFire, bool isStanding, bool isMoving)
    {
        var baseAccuracy = gunItem.m_GunType switch
        {
            GunType.Rifle => GameManager.GetSkillRifle().GetEffectiveRange(),
            GunType.Revolver => Ballistics.RevolverSkillAccuracy(GetRevolverSkillTier()) + gunItem.m_AccuracyRange,
            _ => gunItem.m_AccuracyRange
        };

        return baseAccuracy * Ballistics.AccuracyMultiplier(isHipFire, isStanding, isMoving);
    }

    /// <summary>Makes a copy of a bullet template and fires it from a rifle or revolver.</summary>
    /// <param name="template">An inactive template from <see cref="ProjectileTemplates"/>.</param>
    /// <param name="gunItem">The gun firing it. Il2Cpp doesn't copy this component's fields, so it's passed here.</param>
    /// <param name="position">Where to fire it from, in world space.</param>
    /// <param name="rotation">The direction to fire it in, in world space.</param>
    /// <param name="weather">The weather the shot is fired in, from <see cref="ProjectileUtilities.ReadWeather"/>.</param>
    internal static void SpawnAndFire(GameObject template, GunItem gunItem, Vector3 position, Quaternion rotation, ShotWeather weather) =>
        Spawn(template, position, rotation).Fire(gunItem, weather);

    /// <summary>Makes a copy of a bullet template and fires it as another mod asked, through <see cref="Projectiles.Fire"/>.</summary>
    /// <param name="template">An inactive template from <see cref="ProjectileTemplates"/>.</param>
    /// <param name="shot">The shot to fire.</param>
    /// <param name="rotation">The direction to fire it in, in world space: the shot's, with this projectile's spread.</param>
    /// <param name="group">The shot it's part of, to tell what it hits, or <see langword="null"/> to damage it itself.</param>
    /// <param name="weather">The weather the whole shot is fired in, read once for all its projectiles.</param>
    internal static void SpawnAndFire(GameObject template, ProjectileShot shot, Quaternion rotation,
        ProjectileGroup<ProjectileHit>? group, ShotWeather weather) =>
        Spawn(template, shot.Position, rotation).Fire(shot, group, weather);

    /// <summary>Makes an active copy of a bullet template, ready to fire.</summary>
    /// <param name="template">An inactive template from <see cref="ProjectileTemplates"/>.</param>
    /// <param name="position">Where to put it, in world space.</param>
    /// <param name="rotation">The direction to face it, in world space.</param>
    private static ProjectileItem Spawn(GameObject template, Vector3 position, Quaternion rotation)
    {
        var bullet = Instantiate(template, position, rotation);
        bullet.name = template.name;

        // The template is inactive, so the copy only wakes up here.
        bullet.SetActive(true);
        return bullet.GetComponent<ProjectileItem>();
    }

    private void Awake()
    {
        m_TrailPoints = new List<Vector3>(InitialTrailCapacity);

        ConfigureComponents();
        enabled = false;
    }

    private void Update()
    {
        // Lets the shot report at its time limit, even if this bullet is still flying.
        m_Group?.Update(Time.time);

        if (m_HasHit)
        {
            Linger();
        }
        else if (Time.time - m_FireTime > MaxFlightTime)
        {
            if (ProjectileDebug.Enabled)
            {
                ProjectileDebug.Log(
                    $"{name} flew for {MaxFlightTime} s without hitting anything, so it's removed at {transform.position}, " +
                    $"{Vector3.Distance(m_FirePosition, transform.position):F1} m from where it was fired, at {m_Velocity.magnitude:F0} m/s.");
            }

            m_Path?.End(transform.position, hit: false);
            Miss();
            Destroy(gameObject);
        }
        else
        {
            ExtendTrail();
        }
    }

    /// <summary>
    /// The layers the bullet looks for ahead of it each physics step, besides animals: the ground, buildings, props
    /// and gear the game's own shots hit (<see cref="Utils.m_WeaponProjectileCollisionLayerMask"/>), the colliders on
    /// animals' body parts, and every layer the gear layer collides with.
    /// </summary>
    /// <remarks>
    /// Bullets used to have a collider on the gear layer, which stopped them on anything that layer collides with,
    /// even where the raycast's layers didn't include it. Looking for those layers too keeps them hitting the same
    /// things. The animals' own layer stays out, so body parts are found first.
    /// </remarks>
    private static int HitLayerMask
    {
        get
        {
            if (s_HitLayerMask is { } mask)
                return mask;

            mask = Utils.m_WeaponProjectileCollisionLayerMask | 1 << AiBodyPartLayer;
            for (var layer = 0; layer < 32; layer++)
            {
                if (layer != AiLayer && !Physics.GetIgnoreLayerCollision(ProjectileTemplates.GearLayer, layer))
                    mask |= 1 << layer;
            }

            s_HitLayerMask = mask;
            return mask;
        }
    }

    /// <summary>
    /// Works out where the bullet will be after this physics step, looks along the straight line to there and hits the
    /// first thing on it. If there's nothing, it moves the bullet there, facing the way it's now going through the air.
    /// </summary>
    /// <remarks>
    /// The game's arrows find what they hit with a raycast each physics step too. The step's path is really a slight
    /// curve, but at a few metres to a few tens of metres a step, the straight line is close enough.
    /// </remarks>
    private void FixedUpdate()
    {
        if (m_HasHit)
            return;

        var origin = transform.position;
        var next = Flight.Step(new(origin.ToNumerics(), m_Velocity.ToNumerics()), Physics.gravity.ToNumerics(), m_Drag, m_Atmosphere, Time.fixedDeltaTime);
        var destination = next.Position.ToUnity();

        var path = destination - origin;
        var stepDistance = path.magnitude;
        if (stepDistance > 0f)
        {
            var direction = path / stepDistance;
            if (Physics.Raycast(origin, direction, out var hit, stepDistance, HitLayerMask, QueryTriggerInteraction.Ignore)
                || Physics.Raycast(origin, direction, out hit, stepDistance, AiLayerMask, QueryTriggerInteraction.Ignore))
            {
                // Its velocity where it hit, part way through the step, for the damage or the ricochet.
                var hitVelocity = Flight.VelocityPartWay(m_Velocity.ToNumerics(), next.Velocity, hit.distance / stepDistance).ToUnity();
                if (TryRicochet(hit, hitVelocity))
                    return;

                m_Velocity = hitVelocity;
                m_LowestSpeed = Mathf.Min(m_LowestSpeed, m_Velocity.magnitude);
                Hit(hit.collider, hit.point, hit.normal);
                return;
            }
        }

        m_Velocity = next.Velocity.ToUnity();
        m_LowestSpeed = Mathf.Min(m_LowestSpeed, m_Velocity.magnitude);
        // A spinning bullet points into the air flowing past it, which in a crosswind is a little off its path.
        var airVelocity = m_Velocity - m_Atmosphere.Wind.ToUnity();
        var rotation = airVelocity.sqrMagnitude > 0f ? Quaternion.LookRotation(airVelocity) : transform.rotation;
        transform.SetPositionAndRotation(destination, rotation);

        m_Path?.Add(destination);
        if (m_LogSteps)
            LogStep(airVelocity);
    }

    /// <summary>Logs where the bullet is after a physics step and how fast it's going, for <see cref="m_LogSteps"/>.</summary>
    /// <param name="airVelocity">Its velocity through the air, which sets its Mach number.</param>
    private void LogStep(Vector3 airVelocity)
    {
        var position = transform.position;
        var speed = m_Velocity.magnitude;
        ProjectileDebug.LogStep(
            $"{name} at {position}, {Vector3.Distance(m_FirePosition, position):F1} m out after {Time.time - m_FireTime:F2} s: " +
            $"{speed:F0} m/s ({speed / Mathf.Max(1f, m_MuzzleSpeed):P0} of the muzzle's), Mach {m_Atmosphere.Mach(airVelocity.magnitude):F2}, " +
            $"climbing at {Flight.ClimbAngle(m_Velocity.ToNumerics()):F2}°.");
    }

    /// <summary>
    /// Stops the bullet where it hit, then damages the animal it hit, or tells its <see cref="m_Group"/> what it hit
    /// instead, pushes the gear it hit and plays the impact effects.
    /// </summary>
    /// <remarks>
    /// It stops first because a raycast finds the hit before the bullet gets there, and the damage is worked out from
    /// where the bullet is.
    /// </remarks>
    /// <param name="surface">The collider the bullet hit.</param>
    /// <param name="point">Where the bullet hit, in world space.</param>
    /// <param name="normal">The surface's normal where the bullet hit.</param>
    private void Hit(Collider surface, Vector3 point, Vector3 normal)
    {
        m_HasHit = true;

        var hitObject = surface.gameObject;
        if (ProjectileDebug.Enabled)
        {
            ProjectileDebug.Log(
                $"{name} hit {hitObject.name} (collider {surface.name}, layer {hitObject.layer}) at {point}, " +
                $"{Vector3.Distance(m_FirePosition, point):F1} m from where it was fired, after {Time.time - m_FireTime:F2} s, ricochets {m_Ricochets}: " +
                $"{m_Velocity.magnitude:F0} m/s (slowest {m_LowestSpeed:F0}) of {m_MuzzleSpeed:F0} at the muzzle, " +
                $"{Ricochet.AngleToSurface(m_Velocity.ToNumerics(), normal.ToNumerics()):F1}° to the surface, " +
                $"damage {DamageOnHit(point):F1} of {m_MuzzleDamage:F1} before any body part scales it.");
        }

        m_Path?.End(point, hit: true);
        StopAt(point);

        if (m_Group is null)
            TryInflictDamage(hitObject, hitObject.name);
        else
            ReportHit(surface, point, normal);

        TryPushGear(surface);

        if (m_PlayImpactEffects)
            SpawnImpactEffects(surface, point);
    }

    // A bullet still in the air when its scene unloads is destroyed without hitting anything.
    private void OnDestroy()
    {
        m_Path?.Abandon();
        Miss();
    }

    /// <summary>
    /// If <see cref="Settings.m_TrailsEnabled"/> is on, creates the trail using the arrow's trail material.
    /// </summary>
    private void ConfigureComponents()
    {
        if (!Settings.Instance.m_TrailsEnabled)
            return;

        // Unity objects need == null, not ??=, to tell when they've been destroyed.
        if (s_TrailMaterial == null)
            s_TrailMaterial = PrefabMaterials.GetLineRendererMaterial("GEAR_Arrow", "LineRenderer");

        if (s_TrailMaterial == null)
        {
            Mod.Logger.Log("Couldn't find the arrow's trail material, so bullets have no trail.", FlaggedLoggingLevel.Warning);
            return;
        }

        GameObject trailObject = new("LineRenderer") { transform = { parent = transform } };

        m_Trail = trailObject.AddComponent<LineRenderer>();
        m_Trail.material = s_TrailMaterial;
        m_Trail.startWidth = 0.4f;
        m_Trail.endWidth = 0.1f;

        Gradient gradient = new();
        gradient.SetKeys(
            new GradientColorKey[] { new(Color.white, 0.0f), new(Color.white, 1.0f) },
            new GradientAlphaKey[] { new(1.0f, 0.0f), new(0.0f, 1.0f) });
        m_Trail.colorGradient = gradient;
        m_Trail.useWorldSpace = true;
        m_Trail.positionCount = 0;
    }

    /// <summary>
    /// Counts the shot in the player's statistics, then launches the bullet at the muzzle velocity of the weapon in
    /// the player's hands, with its ammo's drag and the gun's sights zeroed.
    /// </summary>
    /// <param name="gunItem">The gun firing it.</param>
    /// <param name="weather">The weather the shot is fired in, or <see cref="ShotWeather.None"/>.</param>
    private void Fire(GunItem gunItem, ShotWeather weather)
    {
        m_GunType = gunItem.m_GunType;
        m_MuzzleDamage = gunItem.m_DamageHP;
        m_WeaponSource = gunItem.m_GunType.ToWeaponSource();

        StatsManager.IncrementValue(m_GunType == GunType.Rifle ? StatID.RifleShot : StatID.RevolverShot);

        var weaponName = GameManager.GetPlayerManagerComponent().m_ItemInHands.name;
        var muzzleVelocity = Ballistics.GetMuzzleVelocity(weaponName);
        if (muzzleVelocity == 0)
            Mod.Logger.Log($"{weaponName} has no muzzle velocity, so its bullet drops at the muzzle.", FlaggedLoggingLevel.Warning);

        var ammoGearName = ProjectileTemplates.GetAmmoGearName(gunItem.m_GunType);
        var ballisticCoefficient = ammoGearName == null ? 0f : Ballistics.GetBallisticCoefficient(ammoGearName);
        var drag = ballisticCoefficient > 0f ? DragModel.G1(ballisticCoefficient) : DragModel.Linear(DefaultDrag);

        var zeroDistance = gunItem.m_GunType switch
        {
            GunType.Rifle => Ballistics.RifleZeroDistance,
            GunType.Revolver => Ballistics.RevolverZeroDistance,
            _ => 0f
        };

        Launch(muzzleVelocity, drag, zeroDistance, weather);
    }

    /// <summary>Launches the bullet as another mod asked, through <see cref="Projectiles.Fire"/>.</summary>
    /// <param name="shot">The shot to fire.</param>
    /// <param name="group">The shot it's part of, to tell what it hits, or <see langword="null"/> to damage it itself.</param>
    /// <param name="weather">The weather the shot is fired in, or <see cref="ShotWeather.None"/>.</param>
    private void Fire(ProjectileShot shot, ProjectileGroup<ProjectileHit>? group, ShotWeather weather)
    {
        m_MuzzleDamage = shot.Damage;
        m_WeaponSource = shot.WeaponSource;
        m_Group = group;
        m_PlayImpactEffects = shot.PlayImpactEffects;

        var drag = shot.BallisticCoefficient > 0f ? DragModel.G1(shot.BallisticCoefficient) : DragModel.Linear(shot.Drag);
        Launch(shot.MuzzleVelocity * Ballistics.MuzzleVelocityMultiplier, drag, shot.ZeroDistance, weather);
    }

    /// <summary>
    /// Releases the bullet and launches it forwards, raised by the angle that zeroes it at
    /// <paramref name="zeroDistance"/>, then starts its trail.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The zero angle is worked out for a level shot and added to whatever way the bullet is aimed, as a real gun's
    /// sights are set once and stay at that angle to the barrel, uphill or down.
    /// </para>
    /// <para>
    /// The weather, read once for the whole shot, sets the air it flies through and the powder's temperature, which
    /// lowers the muzzle velocity in the cold. The zero is still for 15 °C and still air, so the cold and wind move the
    /// bullet off the point of aim, and damage is still measured against the muzzle velocity at 15 °C, so a cold shot
    /// does a little less.
    /// </para>
    /// </remarks>
    /// <param name="muzzleVelocity">How fast it leaves the muzzle at 15 °C, in metres per second.</param>
    /// <param name="drag">How the air slows it.</param>
    /// <param name="zeroDistance">Where it should cross the line of sight, in metres, or 0 to fire it straight along it.</param>
    /// <param name="weather">The weather the shot is fired in, or <see cref="ShotWeather.None"/>.</param>
    private void Launch(float muzzleVelocity, DragModel drag, float zeroDistance, ShotWeather weather)
    {
        m_FireTime = Time.time;
        enabled = true;
        transform.parent = null;
        m_Drag = drag;
        var aimDirection = transform.forward;

        var zeroAngle = Flight.ZeroAngle(muzzleVelocity, drag, Physics.gravity.magnitude, zeroDistance, Time.fixedDeltaTime);
        if (zeroAngle > 0f)
            transform.rotation *= Quaternion.Euler(-zeroAngle, 0f, 0f);

        m_Atmosphere = weather.Atmosphere;
        var launchSpeed = weather.MuzzleVelocity(muzzleVelocity);

        var velocity = transform.forward * launchSpeed;
        m_Velocity = velocity;
        m_MuzzleSpeed = muzzleVelocity;
        m_LowestSpeed = launchSpeed;

        m_FirePosition = transform.position;

        if (ProjectileDebug.Enabled)
        {
            ProjectileDebug.Log(
                $"{name} fired by {(m_GunType is { } gunType ? gunType.ToString() : "another mod")} from {m_FirePosition} towards {aimDirection}: " +
                $"{launchSpeed:F0} m/s ({muzzleVelocity:F0} at 15 °C), {drag}, zeroed at {zeroDistance:F0} m ({zeroAngle:F3}° up), " +
                $"damage {m_MuzzleDamage:F1}, in {weather}.");
        }

        m_LogSteps = ProjectileDebug.LogSteps;
        if (ProjectileDebug.DrawPaths)
            m_Path = new ProjectilePath(m_FirePosition, aimDirection, m_Atmosphere.Wind.ToUnity());

        // Lets a cougar react to a shot that only just misses it, as it does to the game's own shots.
        GameManager.GetCougarManager().MaybeAimNearMiss(velocity.normalized);

        StartTrail();
    }

    /// <summary>Starts the trail at the fire position, if the bullet has one.</summary>
    private void StartTrail()
    {
        if (m_Trail is null || m_TrailPoints is null)
            return;

        m_Trail.startColor = new Color(1f, 1f, 1f, 0f);
        m_Trail.endColor = Color.white * 0.7f;

        m_TrailPoints.Add(m_FirePosition);
        m_Trail.positionCount = 1;
        m_Trail.SetPosition(0, m_FirePosition);
    }

    /// <summary>
    /// Damages the animal the bullet hit, if it hit one, the way the game's own shots do. The damage falls with the
    /// speed the bullet has lost, see <see cref="Ballistics.DamageOnHit"/>, and the hit counts towards the player's
    /// skill and statistics.
    /// </summary>
    /// <remarks>
    /// The collider hit usually has a <see cref="LocalizedDamage"/> saying how much that body part scales the damage,
    /// how fast it bleeds out and its chance of a critical hit. The game's arrows check for one rather than assume it,
    /// so without one the hit does unscaled damage, leaves the bleed-out time to the game and can't be critical.
    /// </remarks>
    /// <param name="victim">The object the bullet hit.</param>
    /// <param name="colliderName">The name of the collider that was hit, which the game uses to tell body parts apart.</param>
    private void TryInflictDamage(GameObject victim, string colliderName)
    {
        var baseAi = victim.layer switch
        {
            AiLayer => victim.GetComponent<BaseAi>(),
            AiBodyPartLayer => victim.transform.GetComponentInParent<BaseAi>(),
            _ => null
        };

        if (baseAi == null)
        {
            // An animal on another layer would be missed here, so say if the object belongs to one.
            if (victim.transform.GetComponentInParent<BaseAi>() != null)
                Mod.Logger.Log($"{victim.name} belongs to an animal but is on layer {victim.layer}, so the hit does no damage.", FlaggedLoggingLevel.Debug);

            return;
        }

        var weaponSource = m_WeaponSource;
        baseAi.MaybeFleeOrAttackFromProjectileHit(weaponSource);

        var damage = DamageOnHit(transform.position);
        var healthBefore = baseAi.m_CurrentHP;

        // Only the game's own rifle and revolver have a skill and statistics for hits.
        if (m_GunType is GunType.Rifle or GunType.Revolver && baseAi.GetAiMode() != AiMode.Dead)
        {
            var hitStat = m_GunType == GunType.Rifle ? StatID.SuccessfulHits_Rifle : StatID.SuccessfulHits_Revolver;
            var skill = m_GunType == GunType.Rifle ? SkillType.Rifle : SkillType.Revolver;
            StatsManager.IncrementValue(hitStat);
            GameManager.GetSkillsManager().IncrementPointsAndNotify(skill, 1, SkillsManager.PointAssignmentMode.AssignOnlyInSandbox);
        }

        var localizedDamage = victim.GetComponent<LocalizedDamage>();
        if (localizedDamage == null)
        {
            Mod.Logger.Log($"{colliderName} has no LocalizedDamage, so the hit does unscaled damage.", FlaggedLoggingLevel.Debug);
            baseAi.ApplyDamage(damage, DamageSource.Player, colliderName);
            LogDamage(baseAi, colliderName, damage, healthBefore);
            return;
        }

        var bleedOutMinutes = localizedDamage.GetBleedOutMinutes(weaponSource);
        damage *= localizedDamage.GetDamageScale(weaponSource);

        // A critical hit kills outright, with the body part's chance for the gun that fired.
        if (!baseAi.m_IgnoreCriticalHits && localizedDamage.RollChanceToKill(weaponSource))
            damage = float.PositiveInfinity;

        baseAi.SetupDamageForAnim(transform.position, GameManager.GetPlayerTransform().position, localizedDamage);
        baseAi.ApplyDamage(damage, bleedOutMinutes, DamageSource.Player, colliderName);
        LogDamage(baseAi, colliderName, damage, healthBefore);
    }

    /// <summary>The damage the bullet does where it hit, before any body part scales it: see <see cref="Ballistics.DamageOnHit"/>.</summary>
    /// <param name="point">Where it hit, in world space, to work out how far it travelled.</param>
    private float DamageOnHit(Vector3 point) =>
        Ballistics.DamageOnHit(m_Drag, m_LowestSpeed, m_MuzzleSpeed, Vector3.Distance(m_FirePosition, point), m_MuzzleDamage);

    /// <summary>Logs the damage a hit did to an animal, and its health before and after.</summary>
    /// <param name="baseAi">The animal hit.</param>
    /// <param name="colliderName">The name of the collider that was hit.</param>
    /// <param name="damage">The damage passed to the game.</param>
    /// <param name="healthBefore">The animal's health before the hit.</param>
    private void LogDamage(BaseAi baseAi, string colliderName, float damage, float healthBefore)
    {
        if (!ProjectileDebug.Enabled)
            return;

        ProjectileDebug.Log(
            $"{name} did {damage:F1} damage to {baseAi.gameObject.name} at {colliderName} " +
            $"({Vector3.Distance(m_FirePosition, transform.position):F1} m, {m_Velocity.magnitude:F0} of {m_MuzzleSpeed:F0} m/s, " +
            $"{m_MuzzleDamage:F1} at the muzzle): " +
            $"health {healthBefore:F1} -> {baseAi.m_CurrentHP:F1} of {baseAi.m_MaxHP:F1}, now {baseAi.GetAiMode()}.");
    }

    /// <summary>
    /// Pushes the loose gear the bullet hit, if it hit some, as hard as the game's own bullet for the gun would: along
    /// the bullet's path and upwards, with a spin in a random direction.
    /// </summary>
    /// <remarks>
    /// The forces come from the game's bullet, see <see cref="ProjectileTemplates.RememberGearImpactForce"/>. How the game
    /// combines them isn't known, so this is a guess at it.
    /// </remarks>
    /// <param name="surface">The collider the bullet hit.</param>
    private void TryPushGear(Collider surface)
    {
        if (surface == null || m_GunType is not { } gunType || !ProjectileTemplates.TryGetGearImpactForce(gunType, out var impact))
            return;

        var gearItem = surface.GetComponentInParent<GearItem>();
        if (gearItem == null)
            return;

        var force = transform.forward * impact.Force + Vector3.up * impact.UpwardForce;
        gearItem.ApplyForce(force, UnityEngine.Random.insideUnitSphere * impact.TorqueForce);
    }

    /// <summary>
    /// Stops the bullet where it hit and hides it, so it can stay a little longer for its trail and impact sound
    /// without hitting anything else.
    /// </summary>
    /// <param name="point">Where the bullet hit, in world space.</param>
    private void StopAt(Vector3 point)
    {
        transform.position = point;

        foreach (var meshRenderer in GetComponentsInChildren<MeshRenderer>())
            meshRenderer.enabled = false;

        // The trail ends where the bullet hit.
        if (m_Trail is not null && m_TrailPoints is not null)
        {
            m_TrailPoints.Add(point);
            m_Trail.positionCount = m_TrailPoints.Count;
            m_Trail.SetPositions(m_TrailPoints.ToArray());
        }
    }

    /// <summary>
    /// Plays the game's bullet impact particles, decal and sound for the surface the bullet hit, at the point it hit.
    /// </summary>
    /// <remarks>
    /// After a hit, the sound plays from the bullet, which <see cref="StopAt"/> has moved to that point. After a
    /// ricochet the bullet flies on, so the sound plays from an empty object left at the point for
    /// <see cref="ImpactLingerDuration"/>. Whether the game's audio plays from an object it didn't set up hasn't been
    /// checked in game.
    /// </remarks>
    /// <param name="surface">The collider the bullet hit.</param>
    /// <param name="point">Where the bullet hit, in world space.</param>
    /// <param name="known">
    /// The surface's impact type and material tag, if they've already been looked up, as a ricochet does, or
    /// <see langword="null"/> to look them up.
    /// </param>
    /// <param name="flyingOn">Whether the bullet glanced off and flies on, so the sound can't play from it.</param>
    private void SpawnImpactEffects(Collider surface, Vector3 point, (BulletImpactEffectType Type, string MaterialTag)? known = null,
        bool flyingOn = false)
    {
        if (surface == null)
            return;

        string materialTag;
        BulletImpactEffectType impactEffectType;
        if (known is { } surfaceType)
            (impactEffectType, materialTag) = surfaceType;
        else
            impactEffectType = GetImpactEffectType(surface, point, out materialTag);

        var impactEffectPool = GameManager.GetEffectPoolManager().GetBulletImpactEffectPool();

        // Surfaces without a material tag get the stone effect.
        impactEffectPool.SpawnUntilParticlesDone(
            impactEffectType == BulletImpactEffectType.BulletImpactEffect_Untagged
                ? BulletImpactEffectType.BulletImpactEffect_Stone
                : impactEffectType,
            point,
            transform.rotation);

        var materialEffectType = ImpactDecals.MapBulletImpactEffectTypeToMaterialEffectType(impactEffectType);
        GameManager.GetDynamicDecalsManager().AddImpactDecal(ProjectileType.Bullet, materialEffectType, point, transform.forward);

        var soundSource = gameObject;
        if (flyingOn)
        {
            soundSource = new GameObject("AdaptiveArsenal_RicochetSound");
            soundSource.transform.position = point;
            Destroy(soundSource, ImpactLingerDuration);
        }

        GameAudioManager.SetMaterialSwitch(materialTag, soundSource);
        GameAudioManager.Play3DSound("Play_BulletImpacts", soundSource);
    }

    /// <summary>The game's kind of bullet impact for a surface, which sets its effects and whether bullets glance off it.</summary>
    /// <param name="surface">The collider the bullet hit.</param>
    /// <param name="point">Where it hit, in world space.</param>
    /// <param name="materialTag">The surface's material tag, which the impact sound switches on.</param>
    private static BulletImpactEffectType GetImpactEffectType(Collider surface, Vector3 point, out string materialTag)
    {
        materialTag = Utils.GetMaterialTagForObjectAtPosition(surface.gameObject, point);
        return vp_Bullet.GetImpactEffectTypeBasedOnMaterial(materialTag);
    }

    /// <summary>
    /// Glances the bullet off what it hit, if it's hard and the bullet hit it at a shallow angle: see
    /// <see cref="Ricochet"/>. It plays the impact effects there, then flies on from the point, slower and in a new
    /// direction, and can still hit something.
    /// </summary>
    /// <remarks>
    /// <see cref="Ricochet.Glances"/> decides it: rock, metal and ice glance, at a shallow enough angle. Animals never
    /// do, whatever their tag. The angle is checked first, since it's cheap and most hits are too steep, and the surface
    /// is looked up only then, once, for both the decision and the effects. Loose gear it glances off is pushed, as a
    /// hit would. The rest of the step after the ricochet is lost, which at a few hundredths of a second doesn't show.
    /// </remarks>
    /// <param name="hit">What the step's raycast hit.</param>
    /// <param name="velocity">The bullet's velocity where it hit.</param>
    /// <returns>Whether it glanced off, so it didn't hit.</returns>
    private bool TryRicochet(RaycastHit hit, Vector3 velocity)
    {
        var surface = hit.collider;
        var incoming = velocity.ToNumerics();
        var normal = hit.normal.ToNumerics();
        if (surface.gameObject.layer is AiLayer or AiBodyPartLayer || !Ricochet.IsShallow(incoming, normal, m_Ricochets))
            return false;

        var impactEffectType = GetImpactEffectType(surface, hit.point, out var materialTag);
        if (!Ricochet.Glances(incoming, normal, ToSurfaceKind(impactEffectType), m_Ricochets))
            return false;

        m_Ricochets++;

        // The effects and the push face the way the bullet came in, so they come before it turns.
        transform.position = hit.point;
        TryPushGear(surface);
        if (m_PlayImpactEffects)
            SpawnImpactEffects(surface, hit.point, (impactEffectType, materialTag), flyingOn: true);

        m_Velocity = Ricochet.Deflect(incoming, normal, UnityEngine.Random.value, UnityEngine.Random.value).ToUnity();
        m_LowestSpeed = Mathf.Min(m_LowestSpeed, m_Velocity.magnitude);

        if (ProjectileDebug.Enabled)
        {
            ProjectileDebug.Log(
                $"{name} glanced off {surface.gameObject.name} ({impactEffectType}, tag {materialTag}) at {hit.point}, " +
                $"{Vector3.Distance(m_FirePosition, hit.point):F1} m from where it was fired, ricochet {m_Ricochets} of {Ricochet.MaxRicochets}: " +
                $"in at {Ricochet.AngleToSurface(incoming, normal):F1}° and {velocity.magnitude:F0} m/s, " +
                $"out at {Ricochet.AngleToSurface(m_Velocity.ToNumerics(), normal):F1}° and {m_Velocity.magnitude:F0} m/s.");
        }

        m_Path?.Ricochet(hit.point, hit.normal);

        // Just off the surface, so the next step's raycast doesn't start inside it, and facing into the air as every
        // step does.
        transform.SetPositionAndRotation(hit.point + hit.normal * RicochetClearance, Quaternion.LookRotation(m_Velocity - m_Atmosphere.Wind.ToUnity()));
        m_TrailPoints?.Add(hit.point);
        return true;
    }

    /// <summary>The <see cref="SurfaceKind"/> the game's impact type is, one to one.</summary>
    /// <param name="impactEffectType">The game's impact type for the surface.</param>
    private static SurfaceKind ToSurfaceKind(BulletImpactEffectType impactEffectType) => impactEffectType switch
    {
        BulletImpactEffectType.BulletImpactEffect_Snow => SurfaceKind.Snow,
        BulletImpactEffectType.BulletImpactEffect_Wood => SurfaceKind.Wood,
        BulletImpactEffectType.BulletImpactEffect_Fabric => SurfaceKind.Fabric,
        BulletImpactEffectType.BulletImpactEffect_Ice => SurfaceKind.Ice,
        BulletImpactEffectType.BulletImpactEffect_Stone => SurfaceKind.Stone,
        BulletImpactEffectType.BulletImpactEffect_Metal => SurfaceKind.Metal,
        BulletImpactEffectType.BulletImpactEffect_Flesh => SurfaceKind.Flesh,
        BulletImpactEffectType.BulletImpactEffect_Glass => SurfaceKind.Glass,
        _ => SurfaceKind.Untagged
    };

    /// <summary>
    /// Every <see cref="TrailUpdateInterval"/>, adds the bullet's position to the trail, then drops the oldest points
    /// past <see cref="Settings.m_TrailLength"/>.
    /// </summary>
    private void ExtendTrail()
    {
        if (m_Trail is null || Time.time - m_LastTrailUpdateTime < TrailUpdateInterval)
            return;

        m_LastTrailUpdateTime = Time.time;

        if (m_TrailPoints is null)
            return;

        m_TrailPoints.Add(transform.position);

        var pointsToDrop = Curves.PointsToDrop(m_TrailPoints, Vector3.Distance, Settings.Instance.m_TrailLength);
        if (pointsToDrop > 0)
            m_TrailPoints.RemoveRange(0, pointsToDrop);

        m_Trail.positionCount = m_TrailPoints.Count;
        m_Trail.SetPositions(m_TrailPoints.ToArray());
    }

    /// <summary>
    /// After a hit, fades the trail out over <see cref="ImpactLingerDuration"/>, then destroys the bullet.
    /// </summary>
    private void Linger()
    {
        m_TimeSinceHit += Time.deltaTime;

        if (m_TimeSinceHit >= ImpactLingerDuration)
        {
            Destroy(gameObject);
            return;
        }

        if (m_Trail is null)
            return;

        // Scales the colours set in StartTrail, rather than the last frame's, so the fade is steady.
        var alpha = 1f - m_TimeSinceHit / ImpactLingerDuration;
        m_Trail.startColor = new Color(1f, 1f, 1f, 0f);
        m_Trail.endColor = new Color(0.7f, 0.7f, 0.7f, 0.7f * alpha);
    }

    /// <summary>Tells the bullet's <see cref="m_Group"/> what it hit, instead of damaging it.</summary>
    /// <param name="surface">The collider the bullet hit.</param>
    /// <param name="point">Where it hit, in world space.</param>
    /// <param name="normal">The surface's normal where it hit.</param>
    private void ReportHit(Collider surface, Vector3 point, Vector3 normal)
    {
        if (m_Group is null || m_Finished)
            return;

        m_Finished = true;
        var distance = Vector3.Distance(m_FirePosition, point);
        ProjectileHit hit = new(surface, point, normal, m_Velocity.normalized, m_Velocity.magnitude, distance,
            DamageOnHit(point));
        m_Group.ProjectileHit(hit, Time.time);
    }

    /// <summary>Tells the bullet's <see cref="m_Group"/> it missed, if it hasn't told it anything yet.</summary>
    private void Miss()
    {
        if (m_Group is null || m_Finished)
            return;

        m_Finished = true;
        m_Group.ProjectileMissed(Time.time);
    }

    /// <summary>Calls another mod's code, logging what it throws instead of letting it break the bullet.</summary>
    /// <param name="call">The call.</param>
    /// <param name="callbackName">The name of the callback, for the log.</param>
    internal static void CallOtherMod(Action call, string callbackName)
    {
        try
        {
            call();
        }
        catch (Exception exception)
        {
            // Complex Logger formats the message with string.Format, so it can't hold braces.
            var message = exception.ToString().Replace('{', '(').Replace('}', ')');
            Mod.Logger.Log($"Another mod's {callbackName} threw: {message}", FlaggedLoggingLevel.Error);
        }
    }

    /// <summary>The player's current Revolver skill tier.</summary>
    private static int GetRevolverSkillTier() =>
        GameManager.GetSkillsManager().GetSkill(SkillType.Revolver).GetCurrentTierNumber();
}
