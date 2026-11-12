using Il2CppInterop.Runtime.InteropTypes.Arrays;

using Il2CppTLD.Gear;

using UnityEngine.AddressableAssets;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// Tracer rifle cartridges: a copy of the game's rifle cartridge, made in memory, that rifles can load alongside
/// standard ones. They fly exactly like standard rounds, but glow red in flight.
/// </summary>
/// <remarks>
/// A proof of concept until the cartridge comes from an asset bundle. The copy is tinted red but has the standard
/// cartridge's icon. <see cref="RuntimeAddressables"/> makes it loadable by <see cref="GearName"/>, so it can be added
/// to the inventory (with the developer console's <c>add GEAR_RifleAmmoSingleTracer</c>), saved and loaded. Each rifle
/// gets it as a second ammo type, so the game's own ammo switching chooses between them.
/// </remarks>
internal static class TracerAmmo
{
    /// <summary>The tracer cartridge's gear name, which is also the key its prefab loads by.</summary>
    internal const string GearName = "GEAR_RifleAmmoSingleTracer";

    /// <summary>The key the tracer cartridge's <see cref="GearItemData"/> loads by, which rifles list as an ammo type.</summary>
    private const string DataKey = "AdaptiveArsenal_GearItemData_RifleAmmoSingleTracer";

    /// <summary>The localization key of the cartridge's name.</summary>
    private const string NameLocalizationId = "AdaptiveArsenal_GEAR_RifleAmmoSingleTracer";

    /// <summary>The localization key of the cartridge's description.</summary>
    private const string DescriptionLocalizationId = "AdaptiveArsenal_GEAR_RifleAmmoSingleTracer_Desc";

    /// <summary>The game's rifle cartridge, which the tracer is copied from.</summary>
    private const string SourceGearName = "GEAR_RifleAmmoSingle";

    /// <summary>The colour the cartridge is tinted towards, and how far.</summary>
    private static readonly Color s_Tint = new(0.8f, 0.1f, 0.05f);

    /// <summary>How far the cartridge's colour is moved towards <see cref="s_Tint"/>, from 0 to 1.</summary>
    private const float TintAmount = 0.5f;

    /// <summary>The text for each of the cartridge's localization keys.</summary>
    private static readonly Dictionary<string, string> s_Text = new()
    {
        [NameLocalizationId] = "Rifle Tracer Cartridge",
        [DescriptionLocalizationId] = "A rifle cartridge whose bullet burns bright red in flight, so you can see where it went."
    };

    /// <summary>Whether the tracer cartridge has been made.</summary>
    internal static bool IsReady { get; private set; }

    /// <summary>Whether <see cref="Build"/> has run, so it only tries once.</summary>
    private static bool s_Built;

    /// <summary>
    /// Makes the tracer cartridge, the first time it's called: a copy of the game's rifle cartridge, kept inactive and
    /// across scenes, with its own <see cref="GearItemData"/>, and registers both with <see cref="RuntimeAddressables"/>.
    /// If that fails, it isn't tried again, so there are no tracer cartridges until the game restarts.
    /// </summary>
    /// <remarks>Addressables must have started, so call it once a scene has loaded.</remarks>
    internal static void Build()
    {
        if (s_Built)
            return;

        s_Built = true;

        var sourcePrefab = Addressables.LoadAssetAsync<GameObject>(SourceGearName).WaitForCompletion();
        if (sourcePrefab == null)
        {
            Mod.Logger.Log($"Couldn't load {SourceGearName}, so there are no tracer cartridges.", FlaggedLoggingLevel.Warning);
            return;
        }

        // The copy sits under an inactive object, so its GearItem never wakes up and registers with GearManager.
        // The copies the game makes of it are active, and wake up like any other item.
        GameObject holder = new("AdaptiveArsenal_ItemTemplates");
        holder.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(holder);

        var template = UnityEngine.Object.Instantiate(sourcePrefab, holder.transform);
        template.name = GearName;

        var gearItem = template.GetComponent<GearItem>();
        if (gearItem == null || gearItem.m_GearItemData == null)
        {
            Mod.Logger.Log($"{SourceGearName} has no GearItem or data, so there are no tracer cartridges.", FlaggedLoggingLevel.Warning);
            UnityEngine.Object.Destroy(holder);
            return;
        }

        var data = UnityEngine.Object.Instantiate(gearItem.m_GearItemData);
        data.name = GearName;
        data.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        data.m_PrefabReference = new AssetReferenceGearItem(GearName);
        data.m_LocalizedName = new LocalizedString { m_LocalizationID = NameLocalizationId };
        data.m_LocalizedDescription = new LocalizedString { m_LocalizationID = DescriptionLocalizationId };
        gearItem.m_GearItemData = data;

        KeepOwnStack(template, gearItem);
        Tint(template);

        RuntimeAddressables.Register(GearName, template);
        RuntimeAddressables.Register(DataKey, data);
        IsReady = true;

        Mod.Logger.Log($"Made {GearName} from {SourceGearName}.", FlaggedLoggingLevel.Debug);
    }

    /// <summary>
    /// Adds the tracer cartridge to a rifle's ammo types, if it isn't there yet. Call it before the rifle loads them.
    /// </summary>
    /// <param name="gunItem">The gun, which is left alone unless it's a rifle.</param>
    internal static void AddToRifle(GunItem gunItem)
    {
        if (!IsReady || gunItem.m_GunType != GunType.Rifle)
            return;

        var ammoReferences = gunItem.m_AmmoReferences;
        if (ammoReferences == null)
        {
            Mod.Logger.Log($"{gunItem.name} has no ammo list, so it can't load tracer cartridges.", FlaggedLoggingLevel.Warning);
            return;
        }

        foreach (var reference in ammoReferences)
        {
            if (reference != null && reference.RuntimeKey?.ToString() == DataKey)
                return;
        }

        ammoReferences.Add(new AssetReferenceT<GearItemData>(DataKey));
        Mod.Logger.Log($"{gunItem.name} takes tracer cartridges, as ammo type {ammoReferences.Count - 1}.", FlaggedLoggingLevel.Debug);
    }

    /// <summary>Whether the rounds in a gun's clip are tracer cartridges.</summary>
    /// <remarks>
    /// While the player switches ammo, the selected type is the new one but the clip still holds the old one until the
    /// game unloads it, and <see cref="GunItem.m_PreviousAmmoIndex"/> says which that is. That's inferred from how the
    /// game and the Shotgun mod use it.
    /// </remarks>
    /// <param name="gunItem">The gun.</param>
    internal static bool IsLoaded(GunItem gunItem)
    {
        if (!IsReady)
            return false;

        var index = gunItem.IsSwappingAmmo ? gunItem.m_PreviousAmmoIndex : gunItem.m_SelectedAmmoIndex;
        var validAmmo = gunItem.ValidAmmo;
        if (validAmmo == null || index < 0 || index >= validAmmo.Cast<Il2CppSystem.Collections.Generic.ICollection<GearItemData>>().Count)
            return false;

        var ammo = validAmmo[index];
        return ammo != null && ammo.name == GearName;
    }

    /// <summary>The text for one of the tracer cartridge's localization keys.</summary>
    /// <param name="key">The localization key.</param>
    /// <param name="text">The text, if it's one of the cartridge's.</param>
    /// <returns>Whether it's one of the cartridge's.</returns>
    internal static bool TryGetText(string? key, [NotNullWhen(true)] out string? text)
    {
        text = null;
        return key != null && s_Text.TryGetValue(key, out text);
    }

    /// <summary>
    /// Stops the copy stacking with the game's cartridges. Its <see cref="StackableItem"/> is the standard cartridge's,
    /// so it could list other cartridges to share a stack with, or make standard ones when a stack is split.
    /// </summary>
    /// <param name="template">The copy.</param>
    /// <param name="gearItem">The copy's <see cref="GearItem"/>.</param>
    private static void KeepOwnStack(GameObject template, GearItem gearItem)
    {
        var stackable = template.GetComponent<StackableItem>();
        if (stackable == null)
            return;

        stackable.m_ShareStackWithGear = new Il2CppReferenceArray<StackableItem>(0);

        // Unity points a copy's references to its own objects at the copy, but not references to other prefabs.
        if (stackable.m_InstantiateStackItem != null && stackable.m_InstantiateStackItem != gearItem)
        {
            Mod.Logger.Log($"{GearName} split stacks into {stackable.m_InstantiateStackItem.name}, so it now splits into itself.", FlaggedLoggingLevel.Debug);
            stackable.m_InstantiateStackItem = gearItem;
        }
    }

    /// <summary>Tints copies of the cartridge's materials towards red, so it can be told apart from a standard one.</summary>
    /// <param name="template">The cartridge.</param>
    private static void Tint(GameObject template)
    {
        foreach (var renderer in template.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.materials;
            foreach (var material in materials)
            {
                if (material != null && material.HasProperty("_Color"))
                    material.color = Color.Lerp(material.color, s_Tint, TintAmount);
            }

            renderer.materials = materials;
        }
    }
}
