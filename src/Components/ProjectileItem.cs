using AdaptiveArsenal.Core;
using AdaptiveArsenal.Utilities;

using Il2CppTLD.Stats;

namespace AdaptiveArsenal.Components;

/// <summary>
/// A bullet fired from a rifle or revolver. It flies through the world as a physics object, instead of hitting
/// instantly like the game's own shots, and draws a trail behind it.
/// </summary>
/// <remarks>
/// <see cref="ProjectileTemplates"/> builds an inactive template with this component for each gun, and
/// <see cref="Patches.vp_FPSShooterPatches"/> fires a copy of it with <see cref="SpawnAndFire"/> for each shot. When it
/// hits something, it stops and hides, and lingers for <see cref="ImpactLingerDuration"/> while its trail fades and
/// its impact sound plays.
/// </remarks>
[RegisterTypeInIl2Cpp(false)]
public class ProjectileItem : MonoBehaviour
{
    /// <summary>The layer of an animal's own object, which has the <see cref="BaseAi"/>.</summary>
    private const int AiLayer = 16;

    /// <summary>The layer of a collider on part of an animal's body, whose parent has the <see cref="BaseAi"/>.</summary>
    private const int AiBodyPartLayer = 27;

    /// <summary>The longest the trail gets, in metres. Older points are dropped past this.</summary>
    private const float TrailMaxLength = 200f;

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

    /// <summary>How many trail points to make room for up front.</summary>
    private const int InitialTrailCapacity = 100;

    /// <summary>The arrow's trail material, which every bullet's trail uses, looked up the first time it's needed.</summary>
    private static Material? s_TrailMaterial;

    private GunType m_GunType;
    private Rigidbody? m_Rigidbody;

    /// <summary>Where the bullet was fired from, to work out how far it has travelled when it hits.</summary>
    private Vector3 m_FirePosition;

    /// <summary>When the bullet was fired, to remove it after <see cref="MaxFlightTime"/>.</summary>
    private float m_FireTime;

    /// <summary>Whether the bullet has hit something, so it only hits once.</summary>
    private bool m_HasHit;

    /// <summary>How long, in seconds, since the bullet hit something.</summary>
    private float m_TimeSinceHit;

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

    /// <summary>Makes a copy of a bullet template and fires it.</summary>
    /// <param name="template">An inactive template from <see cref="ProjectileTemplates"/>.</param>
    /// <param name="gunType">The gun firing it. Il2Cpp doesn't copy this component's fields, so it's passed here.</param>
    /// <param name="position">Where to fire it from, in world space.</param>
    /// <param name="rotation">The direction to fire it in, in world space.</param>
    internal static void SpawnAndFire(GameObject template, GunType gunType, Vector3 position, Quaternion rotation)
    {
        var bullet = Instantiate(template, position, rotation);
        bullet.name = template.name;

        // The template is inactive, so the copy only wakes up here.
        bullet.SetActive(true);
        bullet.GetComponent<ProjectileItem>().Fire(gunType);
    }

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_TrailPoints = new List<Vector3>(InitialTrailCapacity);

        ConfigureComponents();
        enabled = false;
    }

    private void Update()
    {
        if (m_HasHit)
            Linger();
        else if (Time.time - m_FireTime > MaxFlightTime)
            Destroy(gameObject);
        else
            ExtendTrail();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (m_HasHit)
            return;

        m_HasHit = true;
        var contact = collision.GetContact(0);

        TryInflictDamage(collision.gameObject, collision.gameObject.name);
        StopAt(contact.point);
        SpawnImpactEffects(collision.collider, contact.point);
    }

    /// <summary>Sets up the rigidbody for fast movement and creates the trail using the arrow's trail material.</summary>
    private void ConfigureComponents()
    {
        if (m_Rigidbody is null)
            return;

        m_Rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

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
    /// Counts the shot in the player's statistics, releases the bullet and launches it at the muzzle velocity of the
    /// weapon in the player's hands, then starts its trail.
    /// </summary>
    /// <param name="gunType">The gun firing it.</param>
    private void Fire(GunType gunType)
    {
        m_GunType = gunType;
        m_FireTime = Time.time;
        enabled = true;

        StatsManager.IncrementValue(m_GunType == GunType.Rifle ? StatID.RifleShot : StatID.RevolverShot);

        Utils.SetIsKinematic(m_Rigidbody, false);
        transform.parent = null;

        if (m_Rigidbody is null)
            return;

        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.mass = 0.02f;
        m_Rigidbody.drag = 0.1f;
        m_Rigidbody.angularDrag = 0.1f;

        var weaponName = GameManager.GetPlayerManagerComponent().m_ItemInHands.name;
        var muzzleVelocity = Ballistics.GetMuzzleVelocity(weaponName);
        if (muzzleVelocity == 0)
            Mod.Logger.Log($"{weaponName} has no muzzle velocity, so its bullet drops at the muzzle.", FlaggedLoggingLevel.Warning);

        var velocity = transform.forward * (muzzleVelocity * Ballistics.MuzzleVelocityScale);
        m_Rigidbody.AddForce(velocity, ForceMode.VelocityChange);

        m_FirePosition = transform.position;

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
    /// distance travelled, see <see cref="Ballistics.DamageAtDistance"/>, and the hit counts towards the player's skill
    /// and statistics.
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
            return;

        var weaponSource = m_GunType.ToWeaponSource();
        baseAi.MaybeFleeOrAttackFromProjectileHit(weaponSource);

        var distance = Vector3.Distance(m_FirePosition, transform.position);
        var damage = Ballistics.DamageAtDistance(distance);

        if (baseAi.GetAiMode() != AiMode.Dead)
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
            return;
        }

        var bleedOutMinutes = localizedDamage.GetBleedOutMinutes(weaponSource);
        damage *= localizedDamage.GetDamageScale(weaponSource);

        // A critical hit kills outright. The chance is rolled as a rifle shot for revolvers too.
        if (!baseAi.m_IgnoreCriticalHits && localizedDamage.RollChanceToKill(WeaponSource.Rifle))
            damage = float.PositiveInfinity;

        baseAi.SetupDamageForAnim(transform.position, GameManager.GetPlayerTransform().position, localizedDamage);
        baseAi.ApplyDamage(damage, bleedOutMinutes, DamageSource.Player, colliderName);
    }

    /// <summary>
    /// Stops the bullet where it hit and hides it, so it can stay a little longer for its trail and impact sound
    /// without hitting anything else.
    /// </summary>
    /// <param name="point">Where the bullet hit, in world space.</param>
    private void StopAt(Vector3 point)
    {
        if (m_Rigidbody is not null)
        {
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.isKinematic = true;
        }

        transform.position = point;

        var bulletCollider = GetComponent<Collider>();
        if (bulletCollider != null)
            bulletCollider.enabled = false;

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
    /// <remarks>The sound plays from the bullet, which <see cref="StopAt"/> has moved to that point.</remarks>
    /// <param name="surface">The collider the bullet hit.</param>
    /// <param name="point">Where the bullet hit, in world space.</param>
    private void SpawnImpactEffects(Collider surface, Vector3 point)
    {
        if (surface == null)
            return;

        var materialTag = Utils.GetMaterialTagForObjectAtPosition(surface.gameObject, point);
        var impactEffectType = vp_Bullet.GetImpactEffectTypeBasedOnMaterial(materialTag);
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

        GameAudioManager.SetMaterialSwitch(materialTag, gameObject);
        GameAudioManager.Play3DSound("Play_BulletImpacts", gameObject);
    }

    /// <summary>
    /// Every <see cref="TrailUpdateInterval"/>, adds the bullet's position to the trail, then drops the oldest points
    /// past <see cref="TrailMaxLength"/>.
    /// </summary>
    private void ExtendTrail()
    {
        if (m_Trail is null || Time.time - m_LastTrailUpdateTime < TrailUpdateInterval)
            return;

        m_LastTrailUpdateTime = Time.time;

        if (m_TrailPoints is null)
            return;

        m_TrailPoints.Add(transform.position);

        var pointsToDrop = Curves.PointsToDrop(m_TrailPoints, Vector3.Distance, TrailMaxLength);
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

    /// <summary>The player's current Revolver skill tier.</summary>
    private static int GetRevolverSkillTier() =>
        GameManager.GetSkillsManager().GetSkill(SkillType.Revolver).GetCurrentTierNumber();
}
