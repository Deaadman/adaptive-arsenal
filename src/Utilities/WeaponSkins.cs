using AdaptiveArsenal.Components;
using AdaptiveArsenal.Core;

using MelonLoader.Utils;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// The weapon skins in <see cref="SkinsDirectory"/>, loaded when the game starts, and which one each weapon wears.
/// </summary>
/// <remarks>
/// <para>
/// Each weapon's skin is its <see cref="WeaponData.m_Skin"/>, so it's saved with the game. A skin that's no longer on
/// disk is skipped, and the weapon keeps its own look until the skin is back.
/// </para>
/// <para>
/// A skin shows in three places, each set from a patch: on the weapon's own model, on the ground and when inspected
/// (<see cref="ApplyToModel"/>), on the first-person model when it's equipped (<see cref="ApplyToFirstPerson"/>), and
/// as its icon (<see cref="TryGetIcon"/>).
/// </para>
/// </remarks>
internal static class WeaponSkins
{
    /// <summary>The folder skins are loaded from, laid out as <see cref="SkinCatalog"/> describes.</summary>
    internal static readonly string SkinsDirectory = Path.Combine(MelonEnvironment.ModsDirectory, "AdaptiveArsenal", "Skins");

    /// <summary>Every loaded skin, by <see cref="SkinDefinition.Id"/>.</summary>
    private static readonly Dictionary<string, LoadedSkin> s_Skins = [];

    /// <summary>Finds the skins on disk and loads their images. Called once, when the game starts.</summary>
    internal static void Initialize()
    {
        var definitions = SkinCatalog.Discover(SkinsDirectory, out var problems);
        foreach (var problem in problems)
            Mod.Logger.Log(problem, FlaggedLoggingLevel.Warning);

        foreach (var definition in definitions)
        {
            var texture = LoadTexture(definition.TexturePath, $"{definition.Id}/texture");
            var icon = LoadTexture(definition.IconPath, $"{definition.Id}/icon");

            if (texture == null || icon == null)
            {
                Mod.Logger.Log($"Skin {definition.Id}: its images couldn't be loaded.", FlaggedLoggingLevel.Warning);
                continue;
            }

            s_Skins[definition.Id] = new LoadedSkin(definition, texture, icon);
        }

        Mod.Logger.Log($"Loaded {s_Skins.Count} weapon skins from {SkinsDirectory}.", FlaggedLoggingLevel.Debug);
    }

    /// <summary>The skins there are for a kind of weapon, sorted by ID.</summary>
    /// <param name="gearItem">A weapon of that kind.</param>
    internal static List<SkinDefinition> GetSkinsFor(GearItem gearItem)
    {
        var gearName = Utils.SanitizePrefabName(gearItem.name);
        return [.. s_Skins.Values.Select(skin => skin.Definition).Where(skin => skin.GearName == gearName).OrderBy(skin => skin.Id, StringComparer.Ordinal)];
    }

    /// <summary>
    /// The <see cref="SkinDefinition.Id"/> of the loaded skin a weapon wears, or <see langword="null"/> for its own look.
    /// </summary>
    /// <param name="gearItem">The weapon.</param>
    internal static string? GetSkinId(GearItem gearItem) =>
        TryGetSkin(gearItem, out var skin) ? skin.Definition.Id : null;

    /// <summary>
    /// Puts a skin on a weapon, or takes it off, and shows the change on its model, and on the first-person model if
    /// it's in the player's hands. Returns <see langword="false"/> if there's no such skin for that weapon.
    /// </summary>
    /// <param name="gearItem">The weapon.</param>
    /// <param name="skinId">The <see cref="SkinDefinition.Id"/>, or <see langword="null"/> for the weapon's own look.</param>
    internal static bool SetSkin(GearItem gearItem, string? skinId)
    {
        if (skinId != null && (!s_Skins.TryGetValue(skinId, out var skin)
                || skin.Definition.GearName != Utils.SanitizePrefabName(gearItem.name)))
        {
            return false;
        }

        if (skinId != null)
            WeaponDataManager.Get(gearItem).m_Skin = skinId;
        else if (WeaponDataManager.TryGet(gearItem, out var data))
            data.m_Skin = null;

        ApplyToModel(gearItem);

        var itemInHands = GameManager.GetPlayerManagerComponent().m_ItemInHands;
        var camera = GameManager.GetVpFPSCamera();
        if (itemInHands == gearItem && camera != null && camera.CurrentWeapon != null)
            ApplyToFirstPerson(camera.CurrentWeapon, gearItem);

        return true;
    }

    /// <summary>Shows a weapon's skin, or its own look, on its model.</summary>
    /// <param name="gearItem">The weapon.</param>
    internal static void ApplyToModel(GearItem gearItem)
    {
        var texture = TryGetSkin(gearItem, out var skin) ? skin.Texture : null;
        var weaponSkin = gearItem.GetComponent<WeaponSkin>();

        // Most weapons never have a skin, so they don't need the component.
        if (texture == null && weaponSkin == null)
            return;

        if (weaponSkin == null)
            weaponSkin = gearItem.gameObject.AddComponent<WeaponSkin>();

        weaponSkin.Apply(ModelRenderers(gearItem), texture);
    }

    /// <summary>
    /// Shows the skin of the weapon just equipped, or its own look, on the first-person models, which every weapon of
    /// the same kind shares.
    /// </summary>
    /// <param name="weapon">The first-person weapon.</param>
    /// <param name="gearItem">The weapon equipped.</param>
    internal static void ApplyToFirstPerson(vp_FPSWeapon weapon, GearItem gearItem)
    {
        var texture = TryGetSkin(gearItem, out var skin) ? skin.Texture : null;

        foreach (var firstPersonWeapon in (FirstPersonWeapon?[])[weapon.m_FirstPersonWeaponRightHand, weapon.m_FirstPersonWeaponLeftHand, weapon.m_FirstPersonWeaponShoulder])
        {
            if (firstPersonWeapon == null || firstPersonWeapon.m_Renderable == null)
                continue;

            var weaponSkin = firstPersonWeapon.GetComponent<WeaponSkin>();
            if (texture == null && weaponSkin == null)
                continue;

            if (weaponSkin == null)
                weaponSkin = firstPersonWeapon.gameObject.AddComponent<WeaponSkin>();

            weaponSkin.Apply(FirstPersonRenderers(firstPersonWeapon), texture);
        }
    }

    /// <summary>Gets the icon of a weapon's skin, if it has one.</summary>
    /// <param name="gearItem">The weapon.</param>
    /// <param name="icon">The skin's icon, or <see langword="null"/> when the weapon has its own look.</param>
    internal static bool TryGetIcon(GearItem gearItem, [NotNullWhen(true)] out Texture2D? icon)
    {
        icon = TryGetSkin(gearItem, out var skin) ? skin.Icon : null;
        return icon != null;
    }

    /// <summary>Gets the loaded skin a weapon wears, if it wears one that's loaded.</summary>
    /// <param name="gearItem">The weapon.</param>
    /// <param name="skin">The skin, or <see langword="null"/>.</param>
    private static bool TryGetSkin(GearItem gearItem, [NotNullWhen(true)] out LoadedSkin? skin)
    {
        skin = null;
        return s_Skins.Count > 0
            && WeaponDataManager.TryGet(gearItem, out var data)
            && data.m_Skin != null
            && s_Skins.TryGetValue(data.m_Skin, out skin);
    }

    /// <summary>The renderers of a weapon's own model, which the game lists on its <see cref="GearItem"/>.</summary>
    /// <param name="gearItem">The weapon.</param>
    private static IEnumerable<Renderer> ModelRenderers(GearItem gearItem)
    {
        if (gearItem.m_MeshRenderers != null)
        {
            foreach (var renderer in gearItem.m_MeshRenderers)
                yield return renderer;
        }

        if (gearItem.m_SkinnedMeshRenderers != null)
        {
            foreach (var renderer in gearItem.m_SkinnedMeshRenderers)
                yield return renderer;
        }
    }

    /// <summary>
    /// The renderers of a first-person model that the skin is drawn on: everything in it but the rounds shown while
    /// reloading.
    /// </summary>
    /// <param name="firstPersonWeapon">The first-person model.</param>
    private static IEnumerable<Renderer> FirstPersonRenderers(FirstPersonWeapon firstPersonWeapon)
    {
        var reloadBullets = new HashSet<IntPtr>();
        if (firstPersonWeapon.m_BulletsToReload != null)
        {
            foreach (var bullet in firstPersonWeapon.m_BulletsToReload)
            {
                if (bullet != null)
                    reloadBullets.Add(bullet.Pointer);
            }
        }

        return firstPersonWeapon.m_Renderable.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => !reloadBullets.Contains(renderer.Pointer));
    }

    /// <summary>Loads a PNG into a texture that stays loaded, or returns <see langword="null"/> if it can't.</summary>
    /// <param name="path">The PNG's path.</param>
    /// <param name="name">The texture's name, for debugging.</param>
    private static Texture2D? LoadTexture(string path, string name)
    {
        try
        {
            var texture = new Texture2D(2, 2) { name = name, hideFlags = HideFlags.DontUnloadUnusedAsset };
            if (ImageConversion.LoadImage(texture, File.ReadAllBytes(path), true))
                return texture;

            UnityEngine.Object.Destroy(texture);
        }
        catch (IOException exception)
        {
            Mod.Logger.Log($"Couldn't read {path}: {exception.Message}", FlaggedLoggingLevel.Warning);
        }

        return null;
    }

    /// <summary>A skin with its images loaded.</summary>
    /// <param name="Definition">Where it was found and what it's called.</param>
    /// <param name="Texture">The texture drawn on the weapon's model.</param>
    /// <param name="Icon">The weapon's icon in the inventory.</param>
    private sealed record LoadedSkin(SkinDefinition Definition, Texture2D Texture, Texture2D Icon);
}
