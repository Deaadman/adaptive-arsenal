using AdaptiveArsenal.Core;

using ModData;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// The <see cref="WeaponData"/> for each weapon, saved with the player's game through
/// <see href="https://github.com/dommrogers/ModData">ModData</see>. Features that remember something about each weapon,
/// like its skin or how cold it is, read and change it here with <see cref="Get"/> and <see cref="TryGet"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Patches.SaveGameSystemPatches"/> calls <see cref="Save"/> whenever the game saves, and
/// <see cref="Patches.GameManagerPatches"/> calls <see cref="Load"/> when a save is loaded and <see cref="Clear"/>
/// when the player leaves it. Changes to a weapon's data are kept in memory until then, so set the fields and the next
/// save keeps them.
/// </para>
/// <para>
/// Weapons are told apart by <see cref="GearItem.m_InstanceID"/>, which the game saves with each item, and their gear
/// name. Any <see cref="GearItem"/> works, so bows and knives can have data too.
/// </para>
/// </remarks>
internal static class WeaponDataManager
{
    /// <summary>The name of the mod's entry in ModData's file for each save.</summary>
    private const string ModDataName = "AdaptiveArsenal";

    private static readonly ModDataManager s_ModData = new(ModDataName, false);

    private static readonly WeaponDataStore s_Store = new();

    /// <summary>Gets a weapon's data, adding empty data if there's none yet.</summary>
    /// <param name="gearItem">The weapon.</param>
    internal static WeaponData Get(GearItem gearItem) => s_Store.GetOrAdd(gearItem.m_InstanceID, GearName(gearItem));

    /// <summary>Gets a weapon's data, if there is any, without adding it.</summary>
    /// <param name="gearItem">The weapon.</param>
    /// <param name="data">The weapon's data, or <see langword="null"/> when there's none.</param>
    internal static bool TryGet(GearItem gearItem, [NotNullWhen(true)] out WeaponData? data) =>
        s_Store.TryGet(gearItem.m_InstanceID, GearName(gearItem), out data);

    /// <summary>Forgets a weapon's data, like when it's destroyed.</summary>
    /// <param name="gearItem">The weapon.</param>
    internal static void Remove(GearItem gearItem) => s_Store.Remove(gearItem.m_InstanceID);

    /// <summary>Saves every weapon's data with the game being saved.</summary>
    internal static void Save()
    {
        if (!s_ModData.Save(s_Store.Serialize()))
            Mod.Logger.Log("Couldn't save weapon data with the game.", FlaggedLoggingLevel.Error);
    }

    /// <summary>Replaces every weapon's data with what was saved with the game being loaded.</summary>
    internal static void Load()
    {
        if (s_Store.Deserialize(s_ModData.Load()))
            Mod.Logger.Log($"Loaded data for {s_Store.Count} weapons.", FlaggedLoggingLevel.Debug);
        else
            Mod.Logger.Log("The saved weapon data couldn't be read, so it's been reset.", FlaggedLoggingLevel.Error);
    }

    /// <summary>Forgets every weapon's data, when the player leaves their game.</summary>
    internal static void Clear() => s_Store.Clear();

    /// <summary>The weapon's prefab name, like <c>GEAR_Rifle</c>, without the <c>(Clone)</c> Unity adds to copies.</summary>
    /// <param name="gearItem">The weapon.</param>
    private static string GearName(GearItem gearItem) => Utils.SanitizePrefabName(gearItem.name);
}
