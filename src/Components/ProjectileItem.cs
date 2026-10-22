using AdaptiveArsenal.Core;
using AdaptiveArsenal.Utilities;

using Il2CppTLD.Stats;

namespace AdaptiveArsenal.Components;

/// <summary>
/// A bullet fired from a rifle or revolver. It flies through the world as a physics object, instead of hitting
/// instantly like the game's own shots, and draws a trail behind it.
/// </summary>
/// <remarks>
/// <see cref="Patches.vp_FPSShooterPatches"/> adds this component to the single-round ammo prefabs, and fires a copy
/// of one with <see cref="SpawnAndFire"/> for each shot. It stays disabled on the prefab, and only runs once fired.
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

    /// <summary>How long, in seconds, the trail takes to fade out after the bullet hits something.</summary>
    private const float TrailFadeDuration = 2f;

    /// <summary>How many trail points to make room for up front.</summary>
    private const int InitialTrailCapacity = 100;

    private AmmoItem? m_AmmoItem;
    private GunType m_GunType;
    private Rigidbody? m_Rigidbody;

    /// <summary>Where the bullet was fired from, to work out how far it has travelled when it hits.</summary>
    private Vector3 m_FirePosition;

    private LineRenderer? m_Trail;
    private List<Vector3>? m_TrailPoints;
    private float m_LastTrailUpdateTime;
    private bool m_IsTrailFading;
    private float m_TrailFadeTimer;

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

    /// <summary>Makes a copy of a bullet prefab and fires it.</summary>
    /// <param name="prefab">A prefab with a <see cref="ProjectileItem"/>.</param>
    /// <param name="position">Where to fire it from, in world space.</param>
    /// <param name="rotation">The direction to fire it in, in world space.</param>
    internal static void SpawnAndFire(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var bullet = Instantiate(prefab, position, rotation);
        bullet.name = prefab.name;
        bullet.transform.parent = null;
        bullet.GetComponent<ProjectileItem>().Fire();
    }

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_AmmoItem = GetComponent<AmmoItem>();

        m_TrailPoints = new List<Vector3>(InitialTrailCapacity);

        ConfigureComponents();
        enabled = false;
    }

    private void Update()
    {
        if (m_Trail is null)
            return;

        if (m_IsTrailFading)
            FadeTrail();
        else
            ExtendTrail();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryInflictDamage(collision.gameObject, collision.gameObject.name);
        SpawnImpactEffects(collision, transform);

        if (m_Rigidbody is null)
            return;

        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.isKinematic = true;

        m_IsTrailFading = true;
        m_TrailFadeTimer = 0f;

        // The trail is a child of the bullet, so it's destroyed here too, before FadeTrail gets to run.
        Destroy(gameObject);
    }

    /// <summary>
    /// Sets up the rigidbody for fast movement, reads which gun the ammo is for, and creates the trail using the
    /// arrow's trail material.
    /// </summary>
    private void ConfigureComponents()
    {
        if (m_AmmoItem is null || m_Rigidbody is null)
            return;

        m_Rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        m_GunType = m_AmmoItem.m_AmmoForGunType;

        var arrowTrailMaterial = PrefabMaterials.GetLineRendererMaterial("GEAR_Arrow", "LineRenderer");
        if (arrowTrailMaterial == null)
            return;

        GameObject trailObject = new("LineRenderer") { transform = { parent = transform } };

        m_Trail = trailObject.AddComponent<LineRenderer>();
        m_Trail.material = arrowTrailMaterial;
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
    /// Counts the shot in the player's statistics, releases the bullet from the gun and launches it at the muzzle
    /// velocity of the weapon in the player's hands.
    /// </summary>
    private void Fire()
    {
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

        if (m_Trail is null)
            return;

        m_Trail.startColor = new Color(1f, 1f, 1f, 0f);
        m_Trail.endColor = Color.white * 0.7f;

        var weaponName = GameManager.GetPlayerManagerComponent().m_ItemInHands.name;
        var velocity = transform.forward * (Ballistics.GetMuzzleVelocity(weaponName) * Ballistics.MuzzleVelocityScale);
        m_Rigidbody.AddForce(velocity, ForceMode.VelocityChange);

        m_FirePosition = transform.position;

        if (m_TrailPoints is null)
            return;

        m_TrailPoints.Add(m_FirePosition);
        m_Trail.positionCount = 1;
        m_Trail.SetPosition(0, m_FirePosition);

        // Lets a cougar react to a shot that only just misses it, as it does to the game's own shots.
        GameManager.GetCougarManager().MaybeAimNearMiss(velocity.normalized);
    }

    /// <summary>
    /// Damages the animal the bullet hit, if it hit one, the way the game's own shots do. The damage falls with the
    /// distance travelled, see <see cref="Ballistics.DamageAtDistance"/>, and the hit counts towards the player's skill
    /// and statistics.
    /// </summary>
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

        var localizedDamage = victim.GetComponent<LocalizedDamage>();
        var weaponSource = m_GunType.ToWeaponSource();
        baseAi.MaybeFleeOrAttackFromProjectileHit(weaponSource);
        var bleedOutMinutes = localizedDamage.GetBleedOutMinutes(weaponSource);
        var bodyPartDamageScale = localizedDamage.GetDamageScale(weaponSource);

        var distance = Vector3.Distance(m_FirePosition, transform.position);
        var damage = Ballistics.DamageAtDistance(distance) * bodyPartDamageScale;

        // A critical hit kills outright. The chance is rolled as a rifle shot for revolvers too.
        if (!baseAi.m_IgnoreCriticalHits && localizedDamage.RollChanceToKill(WeaponSource.Rifle))
            damage = float.PositiveInfinity;

        if (baseAi.GetAiMode() != AiMode.Dead)
        {
            var hitStat = m_GunType == GunType.Rifle ? StatID.SuccessfulHits_Rifle : StatID.SuccessfulHits_Revolver;
            var skill = m_GunType == GunType.Rifle ? SkillType.Rifle : SkillType.Revolver;
            StatsManager.IncrementValue(hitStat);
            GameManager.GetSkillsManager().IncrementPointsAndNotify(skill, 1, SkillsManager.PointAssignmentMode.AssignOnlyInSandbox);
        }

        baseAi.SetupDamageForAnim(transform.position, GameManager.GetPlayerTransform().position, localizedDamage);
        baseAi.ApplyDamage(damage, bleedOutMinutes, DamageSource.Player, colliderName);
    }

    /// <summary>
    /// Plays the game's bullet impact particles, decal and sound for the surface the bullet hit.
    /// </summary>
    /// <param name="collision">The collision with the surface.</param>
    /// <param name="bulletTransform">The bullet's transform, where the effects are placed.</param>
    private static void SpawnImpactEffects(Collision collision, Transform bulletTransform)
    {
        var materialTag = Utils.GetMaterialTagForObjectAtPosition(collision.collider.gameObject, collision.gameObject.transform.position);
        var impactEffectType = vp_Bullet.GetImpactEffectTypeBasedOnMaterial(materialTag);
        var impactEffectPool = GameManager.GetEffectPoolManager().GetBulletImpactEffectPool();

        // Surfaces without a material tag get the stone effect.
        impactEffectPool.SpawnUntilParticlesDone(
            impactEffectType == BulletImpactEffectType.BulletImpactEffect_Untagged
                ? BulletImpactEffectType.BulletImpactEffect_Stone
                : impactEffectType,
            bulletTransform.position,
            bulletTransform.rotation);

        var materialEffectType = ImpactDecals.MapBulletImpactEffectTypeToMaterialEffectType(impactEffectType);
        GameManager.GetDynamicDecalsManager().AddImpactDecal(ProjectileType.Bullet, materialEffectType, collision.gameObject.transform.position, bulletTransform.forward);

        if (!collision.collider || !collision.collider.gameObject)
            return;

        GameAudioManager.SetMaterialSwitch(materialTag, collision.collider.gameObject);

        var soundEmitter = GameAudioManager.GetSoundEmitterFromGameObject(collision.collider.gameObject);
        AkSoundEngine.PostEvent("Play_BulletImpacts", soundEmitter);
        GameAudioManager.SetAudioSourceTransform(collision.collider.gameObject, collision.collider.gameObject.transform);
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
    /// Fades the trail out over <see cref="TrailFadeDuration"/>, then destroys it.
    /// </summary>
    private void FadeTrail()
    {
        if (m_Trail is null)
            return;

        m_TrailFadeTimer += Time.deltaTime;
        var alpha = Mathf.Clamp01(1.0f - (m_TrailFadeTimer / TrailFadeDuration));
        var startColor = m_Trail.startColor;
        var endColor = m_Trail.endColor;
        m_Trail.startColor = new Color(startColor.r, startColor.g, startColor.b, alpha * startColor.a);
        m_Trail.endColor = new Color(endColor.r, endColor.g, endColor.b, alpha * endColor.a);

        if (m_TrailFadeTimer >= TrailFadeDuration)
            Destroy(m_Trail);
    }

    /// <summary>The player's current Revolver skill tier.</summary>
    private static int GetRevolverSkillTier() =>
        GameManager.GetSkillsManager().GetSkill(SkillType.Revolver).GetCurrentTierNumber();
}
