using AdaptiveArsenal.Utilities;
using Il2CppTLD.Stats;

namespace AdaptiveArsenal.Components;

[RegisterTypeInIl2Cpp(false)]
public class ProjectileItem : MonoBehaviour
{
    private AmmoItem? m_AmmoItem;
    private GunType m_GunType;
    private LineRenderer? m_LineRenderer;
    private Rigidbody? m_Rigidbody;
    
    private const float Damage = 100f;
    private const float MaxRange = 500f;
    private const float MinDamage = 10f;
    private const float ScaleMultiplier = 0.5f;
    private static readonly int[] RevolverEffectiveRange = [40, 50, 60, 80, 100];
    
    private bool LineRendererStartFadeOut;
    private const float LineRendererFadeDuration = 2f;
    private const float LineRendererMaxLength = 200f;
    private const float TrajectoryUpdateInterval = 0.05f;
    private float LastTrajectoryUpdateTime;
    private float LineRendererFadeTimer;
    private const int InitialTrajectoryCapacity = 100;
    private Vector3 InitialPosition;
    private List<Vector3>? TrajectoryPoints;
    
    private static readonly Dictionary<string, int> GunMuzzleVelocities = new()
    {
        {"GEAR_Rifle_Barbs", 800},
        {"GEAR_Rifle_Curators", 1000},
        {"GEAR_Rifle_Traders", 1100},
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

        TrajectoryPoints = new List<Vector3>(InitialTrajectoryCapacity);
        
        ConfigureComponents();
        enabled = false;
    }

    public static float CalculateAccuracy(GunItem gunItem, bool isHipFire, bool isStanding, bool isMoving)
    {
        var baseAccuracy = gunItem.m_GunType switch
        {
            GunType.Rifle => GameManager.GetSkillRifle().GetEffectiveRange(),
            GunType.Revolver => GetEffectiveRevolverRange() + gunItem.m_AccuracyRange,
            _ => gunItem.m_AccuracyRange
        };

        var accuracyMultiplier = 1.2f;
        
        if (isHipFire) accuracyMultiplier *= 0.7f;
        if (isStanding) accuracyMultiplier *= 0.8f;
        if (isMoving) accuracyMultiplier *= 0.9f;
        
        return baseAccuracy * accuracyMultiplier;
    }
    
    private static float CalculateDamageByDistance(float distance) => Mathf.Lerp(Damage, MinDamage, Mathf.Clamp01(distance / MaxRange));

    private void ConfigureComponents()
    {
        if (m_AmmoItem is null || m_Rigidbody is null)
            return;
        
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
        m_LineRenderer.useWorldSpace = true;
        m_LineRenderer.positionCount = 0;
    }

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

        if (m_LineRenderer is null)
            return;
        
        m_LineRenderer.startColor = new Color(1f, 1f, 1f, 0f);
        m_LineRenderer.endColor = Color.white * 0.7f;
        
        var muzzleVelocity = transform.forward * (GetMuzzleVelocity(GameManager.GetPlayerManagerComponent().m_ItemInHands.name) * ScaleMultiplier);
        m_Rigidbody.AddForce(muzzleVelocity, ForceMode.VelocityChange);

        InitialPosition = transform.position;
        
        if (TrajectoryPoints is null)
            return;
        
        TrajectoryPoints.Add(InitialPosition);
        m_LineRenderer.positionCount = 1;
        m_LineRenderer.SetPosition(0, InitialPosition);
        
        GameManager.GetCougarManager().MaybeAimNearMiss(muzzleVelocity.normalized);
    }

    private static float GetEffectiveRevolverRange() => RevolverEffectiveRange[GameManager.GetSkillsManager().GetSkill(SkillType.Revolver).GetCurrentTierNumber()];
    
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

        var distance = Vector3.Distance(InitialPosition, transform.position);
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
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryInflictDamage(collision.gameObject, collision.gameObject.name);
        SpawnImpactEffects(collision, transform);

        if (m_Rigidbody is null)
            return;
        
        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.isKinematic = true;

        LineRendererStartFadeOut = true;
        LineRendererFadeTimer = 0f;
        
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
        if (m_LineRenderer is null) 
            return;

        if (!LineRendererStartFadeOut)
        {
            if (Time.time - LastTrajectoryUpdateTime < TrajectoryUpdateInterval) return;

            LastTrajectoryUpdateTime = Time.time;
            
            if (TrajectoryPoints is null)
                return;
            
            TrajectoryPoints.Add(transform.position);
            
            var totalLength = 0f;
            var removeCount = 0;
            for (var i = TrajectoryPoints.Count - 1; i > 0; i--)
            {
                totalLength += Vector3.Distance(TrajectoryPoints[i], TrajectoryPoints[i - 1]);
                if (!(totalLength > LineRendererMaxLength)) continue;
                removeCount = i;
                break;
            }

            if (removeCount > 0)
            {
                TrajectoryPoints.RemoveRange(0, removeCount);
            }

            m_LineRenderer.positionCount = TrajectoryPoints.Count;
            m_LineRenderer.SetPositions(TrajectoryPoints.ToArray());
        }
        else
        {
            LineRendererFadeTimer += Time.deltaTime;
            var alpha = Mathf.Clamp01(1.0f - (LineRendererFadeTimer / LineRendererFadeDuration));
            var startColor = m_LineRenderer.startColor;
            var endColor = m_LineRenderer.endColor;
            m_LineRenderer.startColor = new Color(startColor.r, startColor.g, startColor.b, alpha * startColor.a);
            m_LineRenderer.endColor = new Color(endColor.r, endColor.g, endColor.b, alpha * endColor.a);

            if (LineRendererFadeTimer >= LineRendererFadeDuration)
            {
                Destroy(m_LineRenderer);
            }
        }
    }
}