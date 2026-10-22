using AdaptiveArsenal.Components;

using UnityEngine.AddressableAssets;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// Builds the bullet that <see cref="ProjectileItem.SpawnAndFire"/> copies for each shot: the mesh and materials of a
/// single round of ammo, with a rigidbody, a collider and a <see cref="ProjectileItem"/>, and nothing else.
/// </summary>
/// <remarks>
/// Copying the ammo's gear item instead would bring its <see cref="GearItem"/>, which registers with
/// <see cref="GearManager"/> and is written into the save while the bullet is in flight. Each template is built once,
/// kept across scenes, and left inactive so it never runs; only its copies do.
/// </remarks>
internal static class ProjectileTemplates
{
    /// <summary>
    /// The game's gear layer (<c>vp_Layer.Gear</c>). Bullets used to be copies of the ammo's gear item, which is on
    /// it, so this keeps them colliding with the same things.
    /// </summary>
    private const int GearLayer = 17;

    /// <summary>The radius of the bullet's collider, in metres.</summary>
    private const float ColliderRadius = 0.01f;

    /// <summary>The templates built so far, by the gun they're for.</summary>
    private static readonly Dictionary<GunType, GameObject> s_Templates = [];

    /// <summary>How hard each gun's shots push loose gear, from the game's own bullet for it.</summary>
    private static readonly Dictionary<GunType, GearImpactForce> s_GearImpactForces = [];

    /// <summary>Remembers how hard the game's own bullet for a gun pushes loose gear, for the mod's bullets to match.</summary>
    /// <param name="gunType">The gun the bullet is for.</param>
    /// <param name="gameBullet">The game's bullet, from the shooter's prefab before the mod replaces it.</param>
    internal static void RememberGearImpactForce(GunType gunType, vp_Bullet gameBullet)
    {
        GearImpactForce force = new(gameBullet.m_GearImpactForce, gameBullet.m_GearImpactUpwardForce, gameBullet.m_GearImpactTorqueForce);
        if (s_GearImpactForces.TryAdd(gunType, force))
            Mod.Logger.Log($"{gunType} bullets push gear with {force}.", FlaggedLoggingLevel.Debug);
    }

    /// <summary>Gets how hard a gun's shots push loose gear.</summary>
    /// <param name="gunType">The gun that fired.</param>
    /// <param name="force">The force, if the game's bullet for that gun was found.</param>
    /// <returns>Whether the force is known.</returns>
    internal static bool TryGetGearImpactForce(GunType gunType, out GearImpactForce force) =>
        s_GearImpactForces.TryGetValue(gunType, out force);

    /// <summary>Gets the bullet template for a gun, building it the first time.</summary>
    /// <param name="gunType">The gun the bullet is for: a rifle or revolver.</param>
    /// <returns>The inactive template, or <see langword="null"/> if the gun has no ammo or it couldn't be loaded.</returns>
    internal static GameObject? Get(GunType gunType)
    {
        // Unity objects need == null, not ?., to tell when they've been destroyed.
        if (s_Templates.TryGetValue(gunType, out var template) && template != null)
            return template;

        template = Build(gunType);
        if (template != null)
            s_Templates[gunType] = template;

        return template;
    }

    /// <summary>Builds the bullet template for a gun from its single round of ammo.</summary>
    /// <param name="gunType">The gun the bullet is for.</param>
    /// <returns>The inactive template, or <see langword="null"/> if the gun has no ammo or it couldn't be loaded.</returns>
    private static GameObject? Build(GunType gunType)
    {
        var ammoGearName = gunType switch
        {
            GunType.Rifle => "GEAR_RifleAmmoSingle",
            GunType.Revolver => "GEAR_RevolverAmmoSingle",
            _ => null
        };

        if (ammoGearName == null)
            return null;

        // Reading the prefab doesn't run anything on it, so its GearItem never registers.
        var ammoPrefab = Addressables.LoadAssetAsync<GameObject>(ammoGearName).WaitForCompletion();
        if (ammoPrefab == null)
        {
            Mod.Logger.Log($"Couldn't load {ammoGearName}, so {gunType} bullets can't be made.", FlaggedLoggingLevel.Warning);
            return null;
        }

        GameObject template = new($"AdaptiveArsenal_{gunType}Bullet") { layer = GearLayer };
        template.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(template);

        CopyMesh(ammoPrefab, template);

        template.AddComponent<Rigidbody>();
        template.AddComponent<SphereCollider>().radius = ColliderRadius;
        template.AddComponent<ProjectileItem>();

        return template;
    }

    /// <summary>
    /// Gives <paramref name="template"/> a child that draws the first mesh in <paramref name="ammoPrefab"/>, placed as
    /// it is in the prefab.
    /// </summary>
    /// <remarks>Without a mesh the bullet is invisible, but still flies and leaves a trail.</remarks>
    /// <param name="ammoPrefab">The ammo's gear item prefab, which is only read.</param>
    /// <param name="template">The bullet template to add the mesh to.</param>
    private static void CopyMesh(GameObject ammoPrefab, GameObject template)
    {
        var sourceRenderer = ammoPrefab.GetComponentInChildren<MeshRenderer>(true);
        var sourceFilter = sourceRenderer != null ? sourceRenderer.GetComponent<MeshFilter>() : null;
        if (sourceRenderer == null || sourceFilter == null)
        {
            Mod.Logger.Log($"{ammoPrefab.name} has no mesh, so its bullets are invisible.", FlaggedLoggingLevel.Warning);
            return;
        }

        GameObject mesh = new("Mesh") { layer = GearLayer };
        mesh.transform.SetParent(template.transform, false);
        mesh.transform.localPosition = ammoPrefab.transform.InverseTransformPoint(sourceRenderer.transform.position);
        mesh.transform.localRotation = Quaternion.Inverse(ammoPrefab.transform.rotation) * sourceRenderer.transform.rotation;
        mesh.transform.localScale = sourceRenderer.transform.lossyScale;

        mesh.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
        mesh.AddComponent<MeshRenderer>().sharedMaterials = sourceRenderer.sharedMaterials;
    }
}

/// <summary>How hard a shot pushes the loose gear it hits, as the game's own bullets do.</summary>
/// <param name="Force">The push along the bullet's path.</param>
/// <param name="UpwardForce">The push upwards.</param>
/// <param name="TorqueForce">The most it can spin the gear, in a random direction.</param>
internal readonly record struct GearImpactForce(float Force, float UpwardForce, float TorqueForce);
