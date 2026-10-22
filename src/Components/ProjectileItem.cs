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
    /// <summary>The damage a bullet does at the muzzle, before the hit location is taken into account.</summary>
    private const float MaxDamage = 100f;

    /// <summary>The damage a bullet does at <see cref="DamageFalloffRange"/> and beyond.</summary>
    private const float MinDamage = 10f;

    /// <summary>The distance, in metres, over which damage falls from <see cref="MaxDamage"/> to <see cref="MinDamage"/>.</summary>
    private const float DamageFalloffRange = 500f;

    /// <summary>Converts a value in <see cref="MuzzleVelocities"/> to a speed in metres per second.</summary>
    private const float MuzzleVelocityScale = 0.5f;

    /// <summary>Every shot's accuracy is multiplied by this.</summary>
    private const float BaseAccuracyMultiplier = 1.2f;

    /// <summary>Accuracy is multiplied by this when firing from the hip.</summary>
    private const float HipFireAccuracyMultiplier = 0.7f;

    /// <summary>Accuracy is multiplied by this when standing rather than crouching.</summary>
    private const float StandingAccuracyMultiplier = 0.8f;

    /// <summary>Accuracy is multiplied by this when moving.</summary>
    private const float MovingAccuracyMultiplier = 0.9f;

    /// <summary>The base accuracy of a revolver at each Revolver skill tier.</summary>
    private static readonly int[] RevolverAccuracyBySkillTier = [40, 50, 60, 80, 100];

    /// <summary>
    /// The muzzle velocity of each weapon, keyed by part of its gear name. Scaled by <see cref="MuzzleVelocityScale"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="GetMuzzleVelocity"/> uses the first key the gear name contains, so the more specific names come
    /// before <c>GEAR_Rifle</c> and <c>GEAR_Revolver</c>, which every rifle and revolver name contains.
    /// </remarks>
    private static readonly Dictionary<string, int> MuzzleVelocities = new()
    {
        { "GEAR_Rifle_Barbs", 800 },
        { "GEAR_Rifle_Curators", 1000 },
        { "GEAR_Rifle_Traders", 1100 },
        { "GEAR_Rifle_Vaughns", 700 },
        { "GEAR_Rifle", 800 },
        { "GEAR_RevolverFancy", 600 },
        { "GEAR_RevolverGreen", 400 },
        { "GEAR_RevolverStubNosed", 300 },
        { "GEAR_Revolver", 400 }
    };

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
    /// <see cref="BaseAccuracyMultiplier"/> and the multiplier for each of the conditions that apply.
    /// </returns>
    public static float CalculateAccuracy(GunItem gunItem, bool isHipFire, bool isStanding, bool isMoving)
    {
        var baseAccuracy = gunItem.m_GunType switch
        {
            GunType.Rifle => GameManager.GetSkillRifle().GetEffectiveRange(),
            GunType.Revolver => GetRevolverSkillAccuracy() + gunItem.m_AccuracyRange,
            _ => gunItem.m_AccuracyRange
        };

        var accuracyMultiplier = BaseAccuracyMultiplier;

        if (isHipFire) accuracyMultiplier *= HipFireAccuracyMultiplier;
        if (isStanding) accuracyMultiplier *= StandingAccuracyMultiplier;
        if (isMoving) accuracyMultiplier *= MovingAccuracyMultiplier;

        return baseAccuracy * accuracyMultiplier;
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
        var velocity = transform.forward * (GetMuzzleVelocity(weaponName) * MuzzleVelocityScale);
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
    /// distance travelled, see <see cref="CalculateDamageByDistance"/>, and the hit counts towards the player's skill
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
        var damage = CalculateDamageByDistance(distance) * bodyPartDamageScale;

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

        // Walk back from the newest point; once the trail is too long, drop every point before the one reached.
        var length = 0f;
        var pointsToDrop = 0;
        for (var i = m_TrailPoints.Count - 1; i > 0; i--)
        {
            length += Vector3.Distance(m_TrailPoints[i], m_TrailPoints[i - 1]);
            if (!(length > TrailMaxLength))
                continue;

            pointsToDrop = i;
            break;
        }

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

    /// <summary>
    /// The damage a bullet does after travelling <paramref name="distance"/> metres: <see cref="MaxDamage"/> at the
    /// muzzle, falling steadily to <see cref="MinDamage"/> at <see cref="DamageFalloffRange"/>.
    /// </summary>
    private static float CalculateDamageByDistance(float distance) =>
        Mathf.Lerp(MaxDamage, MinDamage, Mathf.Clamp01(distance / DamageFalloffRange));

    /// <summary>The revolver's base accuracy at the player's current Revolver skill tier.</summary>
    private static float GetRevolverSkillAccuracy() =>
        RevolverAccuracyBySkillTier[GameManager.GetSkillsManager().GetSkill(SkillType.Revolver).GetCurrentTierNumber()];

    /// <summary>
    /// The muzzle velocity for a weapon, from the first key in <see cref="MuzzleVelocities"/> its gear name contains.
    /// </summary>
    /// <param name="gearItemName">The weapon's gear name, which may have a suffix like <c>(Clone)</c>.</param>
    /// <returns>The muzzle velocity, or 0 for a weapon that isn't listed.</returns>
    private static int GetMuzzleVelocity(string gearItemName) =>
        MuzzleVelocities.Keys.Where(gearItemName.Contains).Select(key => MuzzleVelocities[key]).FirstOrDefault();
}
