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
    private readonly float m_ScaleMultiplier = 0.75f;

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
    private bool m_LineRendererStartFadeOut = false;

    /// <summary>
    /// The duration of how long it'll take for the LineRenderer to fade out in seconds.
    /// </summary>
    private readonly float m_LineRendererFadeDuration = 2f;

    private float m_LineRendererFadeTimer = 0f;
    #endregion

    #region Other
    private readonly List<Vector3> m_TrajectoryPoints = [];
    private Vector3 m_InitialPosition;
    #endregion

    void Awake()
    {
        InitializeComponents();
        enabled = false;
    }

    float CalculateDamageByDistance(float distance)
    {
        float effectiveRange = m_GunExtension.m_GunStats.m_EffectiveRange;
        float maxRange = m_GunExtension.m_GunStats.m_MaxRange;

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
            float normalizedDistance = (distance - effectiveRange) / (maxRange - effectiveRange);
            return Mathf.Lerp(m_Damage, m_MinDamage, normalizedDistance);
        }
    }

    void ConfigureComponents()
    {
        m_Rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        m_GunType = m_AmmoItem.m_AmmoForGunType;

        Material? fxArrowTrailMaterial = MaterialSwapper.GetLineRendererMaterialFromGearItemPrefab("GEAR_Arrow", "LineRenderer");
        if (fxArrowTrailMaterial != null)
        {
            GameObject lineRendererObject = new("LineRenderer");
            lineRendererObject.transform.parent = transform;

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
    }

    void Fire()
    {
        enabled = true;

        StatsManager.IncrementValue(m_GunType == GunType.Rifle ? StatID.SuccessfulHits_Rifle : StatID.SuccessfulHits_Revolver, 1f);

        Utils.SetIsKinematic(m_Rigidbody, false);
        transform.parent = null;

        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.mass = 0.02f;
        m_Rigidbody.drag = 0.01f;
        m_Rigidbody.angularDrag = 0.05f;

        m_LineRenderer.startColor = new Color(1f, 1f, 1f, 0f);
        m_LineRenderer.endColor = Color.white * 0.7f;

        Vector3 initialForce = transform.forward * (m_GunExtension.m_GunStats.m_MuzzleVelocity * m_ScaleMultiplier);
        m_Rigidbody.AddForce(initialForce, ForceMode.VelocityChange);

        m_InitialPosition = transform.position;
    }

    void InitializeComponents()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_AmmoItem = GetComponent<AmmoItem>();
        if (GameManager.GetPlayerManagerComponent().m_ItemInHands != null)
        {
            m_GunExtension = GameManager.GetPlayerManagerComponent().m_ItemInHands.GetComponent<GunExtension>();
        }

        ConfigureComponents();
    }

    BaseAi? TryInflictDamage(GameObject victim, string collider)
    {
        BaseAi? baseAi = victim.layer == 16 ? victim.GetComponent<BaseAi>() : victim.layer == 27 ? victim.transform.GetComponentInParent<BaseAi>() : null;
        if (baseAi == null) return null;

        LocalizedDamage localizedDamage = victim.GetComponent<LocalizedDamage>();
        WeaponSource weaponSource = m_GunType.ToWeaponSource();
        baseAi.MaybeFleeOrAttackFromProjectileHit(weaponSource);
        float bleedOutMinutes = localizedDamage.GetBleedOutMinutes(weaponSource);
        float damageScaleFactor = localizedDamage.GetDamageScale(weaponSource);

        float distance = Vector3.Distance(m_InitialPosition, transform.position);
        float distanceBasedDamage = CalculateDamageByDistance(distance);

        float damage = distanceBasedDamage * damageScaleFactor;

        if (!baseAi.m_IgnoreCriticalHits && localizedDamage.RollChanceToKill(WeaponSource.Rifle))
        {
            damage = float.PositiveInfinity;
        }

        if (baseAi.GetAiMode() != AiMode.Dead)
        {
            StatID statId = m_GunType == GunType.Rifle ? StatID.SuccessfulHits_Rifle : StatID.SuccessfulHits_Revolver;
            SkillType skillType = m_GunType == GunType.Rifle ? SkillType.Rifle : SkillType.Revolver;
            StatsManager.IncrementValue(statId, 1f);
            GameManager.GetSkillsManager().IncrementPointsAndNotify(skillType, 1, SkillsManager.PointAssignmentMode.AssignOnlyInSandbox);
        }

        baseAi.SetupDamageForAnim(transform.position, GameManager.GetPlayerTransform().position, localizedDamage);
        baseAi.ApplyDamage(damage, bleedOutMinutes, DamageSource.Player, collider);

        return baseAi;
    }

    void OnCollisionEnter(Collision collision)
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
        GameObject gameObject = Instantiate(prefab, startPos, startRot);
        gameObject.name = prefab.name;
        gameObject.transform.parent = null;
        gameObject.GetComponent<AmmoProjectile>().Fire();
        return gameObject;
    }

    static void SpawnImpactEffects(Collision collision, Transform transform)
    {
        string materialTagForObjectAtPosition = Utils.GetMaterialTagForObjectAtPosition(collision.collider.gameObject, collision.gameObject.transform.position);
        BulletImpactEffectType impactEffectTypeBasedOnMaterial = vp_Bullet.GetImpactEffectTypeBasedOnMaterial(materialTagForObjectAtPosition);
        BulletImpactEffectPool bulletImpactEffectPool = GameManager.GetEffectPoolManager().GetBulletImpactEffectPool();

        if (impactEffectTypeBasedOnMaterial == BulletImpactEffectType.BulletImpactEffect_Untagged)
        {
            bulletImpactEffectPool.SpawnUntilParticlesDone(BulletImpactEffectType.BulletImpactEffect_Stone, transform.position, transform.rotation);
        }
        else
        {
            bulletImpactEffectPool.SpawnUntilParticlesDone(impactEffectTypeBasedOnMaterial, transform.position, transform.rotation);
        }

        MaterialEffectType materialEffectType = ImpactDecals.MapBulletImpactEffectTypeToMaterialEffectType(impactEffectTypeBasedOnMaterial);
        GameManager.GetDynamicDecalsManager().AddImpactDecal(ProjectileType.Bullet, materialEffectType, collision.gameObject.transform.position, transform.forward);

        if (collision.collider && collision.collider.gameObject)
        {
            GameAudioManager.SetMaterialSwitch(materialTagForObjectAtPosition, collision.collider.gameObject);
            GameObject soundEmitterFromGameObject = GameAudioManager.GetSoundEmitterFromGameObject(collision.collider.gameObject);
            AkSoundEngine.PostEvent("Play_BulletImpacts", soundEmitterFromGameObject);
            GameAudioManager.SetAudioSourceTransform(collision.collider.gameObject, collision.collider.gameObject.transform);
        }
    }

    void Update()
    {
        if (m_LineRenderer != null)
        {
            if (!m_LineRendererStartFadeOut)
            {
                m_TrajectoryPoints.Add(transform.position);

                float totalLength = 0f;
                for (int i = 0; i < m_TrajectoryPoints.Count - 1; i++)
                {
                    totalLength += Vector3.Distance(m_TrajectoryPoints[i], m_TrajectoryPoints[i + 1]);
                    if (totalLength > m_LineRendererMaxLength)
                    {
                        m_TrajectoryPoints.RemoveAt(0);
                        break;
                    }
                }

                m_LineRenderer.positionCount = m_TrajectoryPoints.Count;
                m_LineRenderer.SetPositions(m_TrajectoryPoints.ToArray());
            }
            else
            {
                m_LineRendererFadeTimer += Time.deltaTime;
                float alpha = Mathf.Clamp01(1.0f - (m_LineRendererFadeTimer / m_LineRendererFadeDuration));
                Color startColor = m_LineRenderer.startColor;
                Color endColor = m_LineRenderer.endColor;
                m_LineRenderer.startColor = new Color(startColor.r, startColor.g, startColor.b, alpha * startColor.a);
                m_LineRenderer.endColor = new Color(endColor.r, endColor.g, endColor.b, alpha * endColor.a);

                if (m_LineRendererFadeTimer >= m_LineRendererFadeDuration)
                {
                    Destroy(m_LineRenderer);
                }
            }
        }
    }
}