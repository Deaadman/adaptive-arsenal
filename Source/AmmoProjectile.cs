using AdaptiveArsenal.Utilities;
using Il2CppTLD.Stats;

namespace AdaptiveArsenal;

[RegisterTypeInIl2Cpp(false)]
public class AmmoProjectile : MonoBehaviour
{
    #region References
#nullable disable
    private AmmoItem m_AmmoItem;
    private GunExtension m_GunExtension;
    private GunType m_GunType;
    private LineRenderer m_LineRenderer;
    private Rigidbody m_Rigidbody;
#nullable enable
    #endregion

    #region Properties
    /// <summary>
    /// Multiplier for the gun's muzzle velocity, affecting projectile speed.
    /// </summary>
    private readonly float m_ScaleMultiplier = 0.5f;

    /// <summary>
    /// Base damage dealt by the weapon, defaulting to 100 for all weapons.
    /// </summary>
    private readonly float m_Damage = 100f;

    /// <summary>
    /// Minimum damage dealt by the weapon at or beyond its maximum range.
    /// </summary>
    private readonly float m_MinDamage = 20f;
    #endregion

    #region LineRenderer Properties
    /// <summary>
    /// The maximum length at which the LineRenderer will render at.
    /// </summary>
    private readonly float m_LineRendererMaxLength = 200f;

    /// <summary>
    /// Will the LineRenderer start to fade out?
    /// </summary>
    private bool m_LineRendererStartFadeOut;

    /// <summary>
    /// The duration of how long it'll take for the LineRenderer to fade out in seconds.
    /// </summary>
    private const float m_LineRendererFadeDuration = 2f;

    private float m_LineRendererFadeTimer;
    #endregion

    #region Other
    private readonly List<Vector3> m_TrajectoryPoints = [];
    private Vector3 m_InitialPosition;
    #endregion

    private void Awake()
    {
        InitializeComponents();
        enabled = false;
    }

    private float CalculateDamageByDistance(float distance)
    {
        float effectiveRange = m_GunExtension.GunStats.EffectiveRange;
        float maxRange = m_GunExtension.GunStats.MaxRange;

        if (distance <= effectiveRange)
        {
            return m_Damage;
        }
        else if (distance > maxRange)
        {
            return m_MinDamage;
        }
        else
        {
            var normalizedDistance = (distance - effectiveRange) / (maxRange - effectiveRange);
            return Mathf.Lerp(m_Damage, m_MinDamage, normalizedDistance);
        }
    }

    private void ConfigureComponents()
    {
        m_Rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        m_GunType = m_AmmoItem.m_AmmoForGunType;

        var fxArrowTrailMaterial = MaterialSwapper.GetLineRendererMaterialFromGearItemPrefab("GEAR_Arrow", "LineRenderer");
        if (fxArrowTrailMaterial == null) return;
        GameObject lineRendererObject = new("LineRenderer")
        {
            transform =
            {
                parent = transform
            }
        };

        m_LineRenderer = lineRendererObject.AddComponent<LineRenderer>();
        m_LineRenderer.material = fxArrowTrailMaterial;
        m_LineRenderer.startWidth = 0.4f;
        m_LineRenderer.endWidth = 0.1f;

        Gradient gradient = new();
        gradient.SetKeys(
            new GradientColorKey[] { new(Color.white, 0.0f), new(Color.white, 1.0f) },
            new GradientAlphaKey[] { new(1.0f, 0.0f), new(0.0f, 1.0f) }
        );
        m_LineRenderer.colorGradient = gradient;
    }

    private void Fire()
    {
        enabled = true;

        StatsManager.IncrementValue(m_GunType == GunType.Rifle ? StatID.SuccessfulHits_Rifle : StatID.SuccessfulHits_Revolver);

        Utils.SetIsKinematic(m_Rigidbody, false);
        transform.parent = null;

        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.mass = 0.02f;
        m_Rigidbody.drag = 0.1f;
        m_Rigidbody.angularDrag = 0.1f;

        m_LineRenderer.startColor = new Color(1f, 1f, 1f, 0f);
        m_LineRenderer.endColor = Color.white * 0.7f;

        var initialForce = transform.forward * (m_GunExtension.GunStats.MuzzleVelocity * m_ScaleMultiplier);
        m_Rigidbody.AddForce(initialForce, ForceMode.VelocityChange);

        m_InitialPosition = transform.position;
    }

    private void InitializeComponents()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_AmmoItem = GetComponent<AmmoItem>();
        if (GameManager.GetPlayerManagerComponent().m_ItemInHands != null)
        {
            m_GunExtension = GameManager.GetPlayerManagerComponent().m_ItemInHands.GetComponent<GunExtension>();
        }

        ConfigureComponents();
    }

    private void TryInflictDamage(GameObject victim, string collider)
    {
        var baseAi = victim.layer switch
        {
            16 => victim.GetComponent<BaseAi>(),
            27 => victim.transform.GetComponentInParent<BaseAi>(),
            _ => null
        };
        
        if (baseAi == null) return;

        var localizedDamage = victim.GetComponent<LocalizedDamage>();
        var weaponSource = m_GunType.ToWeaponSource();
        baseAi.MaybeFleeOrAttackFromProjectileHit(weaponSource);
        var bleedOutMinutes = localizedDamage.GetBleedOutMinutes(weaponSource);
        var damageScaleFactor = localizedDamage.GetDamageScale(weaponSource);

        var distance = Vector3.Distance(m_InitialPosition, transform.position);
        var distanceBasedDamage = CalculateDamageByDistance(distance);

        var damage = distanceBasedDamage * damageScaleFactor;

        if (!baseAi.m_IgnoreCriticalHits && localizedDamage.RollChanceToKill(WeaponSource.Rifle))
        {
            damage = float.PositiveInfinity;
        }

        if (baseAi.GetAiMode() != AiMode.Dead)
        {
            var statId = m_GunType == GunType.Rifle ? StatID.SuccessfulHits_Rifle : StatID.SuccessfulHits_Revolver;
            var skillType = m_GunType == GunType.Rifle ? SkillType.Rifle : SkillType.Revolver;
            StatsManager.IncrementValue(statId);
            GameManager.GetSkillsManager().IncrementPointsAndNotify(skillType, 1, SkillsManager.PointAssignmentMode.AssignOnlyInSandbox);
        }

        baseAi.SetupDamageForAnim(transform.position, GameManager.GetPlayerTransform().position, localizedDamage);
        baseAi.ApplyDamage(damage, bleedOutMinutes, DamageSource.Player, collider);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryInflictDamage(collision.gameObject, collision.gameObject.name);
        SpawnImpactEffects(collision, transform);

        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.isKinematic = true;

        m_LineRendererStartFadeOut = true;
        m_LineRendererFadeTimer = 0f;

        Destroy(gameObject, m_LineRendererFadeDuration);
    }

    internal static GameObject SpawnAndFire(GameObject prefab, Vector3 startPos, Quaternion startRot)
    {
        var gameObject = Instantiate(prefab, startPos, startRot);
        gameObject.name = prefab.name;
        gameObject.transform.parent = null;
        gameObject.GetComponent<AmmoProjectile>().Fire();
        return gameObject;
    }

    private static void SpawnImpactEffects(Collision collision, Transform transform)
    {
        var materialTagForObjectAtPosition = Utils.GetMaterialTagForObjectAtPosition(collision.collider.gameObject, collision.gameObject.transform.position);
        var impactEffectTypeBasedOnMaterial = vp_Bullet.GetImpactEffectTypeBasedOnMaterial(materialTagForObjectAtPosition);
        var bulletImpactEffectPool = GameManager.GetEffectPoolManager().GetBulletImpactEffectPool();

        bulletImpactEffectPool.SpawnUntilParticlesDone(
            impactEffectTypeBasedOnMaterial == BulletImpactEffectType.BulletImpactEffect_Untagged
                ? BulletImpactEffectType.BulletImpactEffect_Stone
                : impactEffectTypeBasedOnMaterial, transform.position, transform.rotation);

        var materialEffectType = ImpactDecals.MapBulletImpactEffectTypeToMaterialEffectType(impactEffectTypeBasedOnMaterial);
        GameManager.GetDynamicDecalsManager().AddImpactDecal(ProjectileType.Bullet, materialEffectType, collision.gameObject.transform.position, transform.forward);

        if (!collision.collider || !collision.collider.gameObject) return;
        GameAudioManager.SetMaterialSwitch(materialTagForObjectAtPosition, collision.collider.gameObject);
        var soundEmitterFromGameObject = GameAudioManager.GetSoundEmitterFromGameObject(collision.collider.gameObject);
        AkSoundEngine.PostEvent("Play_BulletImpacts", soundEmitterFromGameObject);
        GameAudioManager.SetAudioSourceTransform(collision.collider.gameObject, collision.collider.gameObject.transform);
    }

    private void Update()
    {
        if (!m_LineRenderer) return;
        if (!m_LineRendererStartFadeOut)
        {
            m_TrajectoryPoints.Add(transform.position);

            var totalLength = 0f;
            for (var i = 0; i < m_TrajectoryPoints.Count - 1; i++)
            {
                totalLength += Vector3.Distance(m_TrajectoryPoints[i], m_TrajectoryPoints[i + 1]);
                if (!(totalLength > m_LineRendererMaxLength)) continue;
                m_TrajectoryPoints.RemoveAt(0);
                break;
            }

            m_LineRenderer.positionCount = m_TrajectoryPoints.Count;
            m_LineRenderer.SetPositions(m_TrajectoryPoints.ToArray());
        }
        else
        {
            m_LineRendererFadeTimer += Time.deltaTime;
            var alpha = Mathf.Clamp01(1.0f - (m_LineRendererFadeTimer / m_LineRendererFadeDuration));
            var startColor = m_LineRenderer.startColor;
            var endColor = m_LineRenderer.endColor;
            m_LineRenderer.startColor = new Color(startColor.r, startColor.g, startColor.b, alpha * startColor.a);
            m_LineRenderer.endColor = new Color(endColor.r, endColor.g, endColor.b, alpha * endColor.a);

            if (m_LineRendererFadeTimer >= m_LineRendererFadeDuration)
            {
                Destroy(m_LineRenderer);
            }
        }
    }
}