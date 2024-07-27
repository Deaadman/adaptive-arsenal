using AdaptiveArsenal.Utilities;
using Il2CppTLD.Stats;

namespace AdaptiveArsenal.Components;

[RegisterTypeInIl2Cpp(false)]
public class ProjectileItem : MonoBehaviour
{
    private AmmoItem m_AmmoItem;
    private GunType m_GunType;
    private LineRenderer m_LineRenderer;
    private Rigidbody m_Rigidbody;
    
    private bool m_LineRendererStartFadeOut;
    private const float ScaleMultiplier = 0.5f;
    private const float Damage = 100f;
    private const float MinDamage = 10f;
    private const float LineRendererMaxLength = 200f;
    private const float LineRendererFadeDuration = 2f;
    private const float MaxRange = 500f;
    private float m_LineRendererFadeTimer;

    private readonly List<Vector3> m_TrajectoryPoints = [];
    private Vector3 m_InitialPosition;

    private static readonly Dictionary<string, int> GunMuzzleVelocities = new()
    {
        {"GEAR_Rifle_Barbs", 800},
        {"GEAR_Rifle_Curators", 1000},
        {"GEAR_Rifle_Vaughns", 700},
        {"GEAR_Rifle", 800},
        {"GEAR_RevolverFancy", 600},
        {"GEAR_RevolverGreen", 400},
        {"GEAR_RevolverStubNosed", 300},
        {"GEAR_Revolver", 400}
    };
    
    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_AmmoItem = GetComponent<AmmoItem>();

        ConfigureComponents();
        enabled = false;
    }

    private static float CalculateDamageByDistance(float distance) => Mathf.Lerp(Damage, MinDamage, Mathf.Clamp01(distance / MaxRange));

    private void ConfigureComponents()
    {
        m_Rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        m_GunType = m_AmmoItem.m_AmmoForGunType;

        var fxArrowTrailMaterial = MaterialSwapper.GetLineRendererMaterialFromGearItemPrefab("GEAR_Arrow", "LineRenderer");
        if (fxArrowTrailMaterial == null) return;
        GameObject lineRendererObject = new("LineRenderer") { transform = { parent = transform } };

        m_LineRenderer = lineRendererObject.AddComponent<LineRenderer>();
        m_LineRenderer.material = fxArrowTrailMaterial;
        m_LineRenderer.startWidth = 0.4f;
        m_LineRenderer.endWidth = 0.1f;

        Gradient gradient = new();
        gradient.SetKeys(new GradientColorKey[] { new(Color.white, 0.0f), new(Color.white, 1.0f) }, new GradientAlphaKey[] { new(1.0f, 0.0f), new(0.0f, 1.0f) });
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
        
        m_Rigidbody.AddForce(transform.forward * (GetMuzzleVelocity(GameManager.GetPlayerManagerComponent().m_ItemInHands.name) * ScaleMultiplier), ForceMode.VelocityChange);
        m_InitialPosition = transform.position;
    }

    private static int GetMuzzleVelocity(string gearItemName) => GunMuzzleVelocities.Keys.Where(gearItemName.Contains).Select(key => GunMuzzleVelocities[key]).FirstOrDefault();

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

        if (!baseAi.m_IgnoreCriticalHits && localizedDamage.RollChanceToKill(WeaponSource.Rifle)) damage = float.PositiveInfinity;

        if (baseAi.GetAiMode() != AiMode.Dead)
        {
            var statId = m_GunType == GunType.Rifle ? StatID.SuccessfulHits_Rifle : StatID.SuccessfulHits_Revolver;
            var skillType = m_GunType == GunType.Rifle ? SkillType.Rifle : SkillType.Revolver;
            StatsManager.IncrementValue(statId);
            GameManager.GetSkillsManager().IncrementPointsAndNotify(skillType, 1, SkillsManager.PointAssignmentMode.AssignOnlyInSandbox);
        }

        baseAi.SetupDamageForAnim(transform.position, GameManager.GetPlayerTransform().position, localizedDamage);
        baseAi.ApplyDamage(damage, bleedOutMinutes, DamageSource.Player, collider);
        
        Logging.Log($"Projectile hit: Distance={distance:F2}m, Base Damage={distanceBasedDamage:F2}, " + $"Body Part={collider}, Damage Scale={damageScaleFactor:F2}, Final Damage={damage:F2}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryInflictDamage(collision.gameObject, collision.gameObject.name);
        SpawnImpactEffects(collision, transform);

        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.isKinematic = true;

        m_LineRendererStartFadeOut = true;
        m_LineRendererFadeTimer = 0f;
        
        Destroy(gameObject);
    }

    internal static void SpawnAndFire(GameObject prefab, Vector3 startPos, Quaternion startRot)
    {
        var gameObject = Instantiate(prefab, startPos, startRot);
        gameObject.name = prefab.name;
        gameObject.transform.parent = null;
        gameObject.GetComponent<ProjectileItem>().Fire();
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
                if (!(totalLength > LineRendererMaxLength)) continue;
                m_TrajectoryPoints.RemoveAt(0);
                break;
            }

            m_LineRenderer.positionCount = m_TrajectoryPoints.Count;
            m_LineRenderer.SetPositions(m_TrajectoryPoints.ToArray());
        }
        else
        {
            m_LineRendererFadeTimer += Time.deltaTime;
            var alpha = Mathf.Clamp01(1.0f - (m_LineRendererFadeTimer / LineRendererFadeDuration));
            var startColor = m_LineRenderer.startColor;
            var endColor = m_LineRenderer.endColor;
            m_LineRenderer.startColor = new Color(startColor.r, startColor.g, startColor.b, alpha * startColor.a);
            m_LineRenderer.endColor = new Color(endColor.r, endColor.g, endColor.b, alpha * endColor.a);

            if (m_LineRendererFadeTimer >= LineRendererFadeDuration)
            {
                Destroy(m_LineRenderer);
            }
        }
    }
}