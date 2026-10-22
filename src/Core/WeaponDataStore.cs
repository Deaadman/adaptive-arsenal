using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace AdaptiveArsenal.Core;

/// <summary>
/// The <see cref="WeaponData"/> for every weapon the mod remembers something about, by the instance ID the game saves
/// with each item, and its conversion to and from the text that's saved with the game.
/// </summary>
/// <remarks>
/// Each entry also keeps the weapon's gear name. Looking a weapon up with a different gear name than it was stored with
/// treats it as a different weapon: the old entry is ignored, or replaced when asked for with <see cref="GetOrAdd"/>.
/// </remarks>
internal sealed class WeaponDataStore
{
    /// <summary>The version of the saved format, written with it so a later version of the mod can convert it.</summary>
    internal const int FormatVersion = 1;

    /// <summary>Saves public fields, which <see cref="WeaponData"/> uses, and leaves out the defaults to keep saves small.</summary>
    private static readonly JsonSerializerOptions s_JsonOptions = new()
    {
        IncludeFields = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
    };

    private readonly Dictionary<int, WeaponData> m_Weapons = [];

    /// <summary>How many weapons there's data for.</summary>
    internal int Count => m_Weapons.Count;

    /// <summary>Gets the data for a weapon, adding empty data if there's none yet.</summary>
    /// <param name="instanceId">The instance ID the game saves with the weapon.</param>
    /// <param name="gearName">The weapon's gear name, like <c>GEAR_Rifle</c>.</param>
    internal WeaponData GetOrAdd(int instanceId, string gearName)
    {
        if (!TryGet(instanceId, gearName, out var data))
        {
            data = new WeaponData { m_GearName = gearName };
            m_Weapons[instanceId] = data;
        }

        return data;
    }

    /// <summary>Gets the data for a weapon, if there is any, without adding it.</summary>
    /// <param name="instanceId">The instance ID the game saves with the weapon.</param>
    /// <param name="gearName">The weapon's gear name, like <c>GEAR_Rifle</c>.</param>
    /// <param name="data">The weapon's data, or <see langword="null"/> when there's none.</param>
    internal bool TryGet(int instanceId, string gearName, [NotNullWhen(true)] out WeaponData? data)
    {
        if (m_Weapons.TryGetValue(instanceId, out data) && data.m_GearName == gearName)
            return true;

        data = null;
        return false;
    }

    /// <summary>Forgets a weapon, like one that's been destroyed. Returns whether there was data for it.</summary>
    /// <param name="instanceId">The instance ID the game saves with the weapon.</param>
    internal bool Remove(int instanceId) => m_Weapons.Remove(instanceId);

    /// <summary>Forgets every weapon, like when the player leaves a game.</summary>
    internal void Clear() => m_Weapons.Clear();

    /// <summary>Converts every weapon's data to text, to save with the game.</summary>
    internal string Serialize() =>
        JsonSerializer.Serialize(new SaveData { m_Version = FormatVersion, m_Weapons = m_Weapons }, s_JsonOptions);

    /// <summary>
    /// Replaces every weapon's data with what <see cref="Serialize"/> wrote. Returns <see langword="false"/>, leaving
    /// the store empty, when the text isn't data the store can read.
    /// </summary>
    /// <param name="json">The saved text, or <see langword="null"/> or empty for a game with nothing saved.</param>
    internal bool Deserialize(string? json)
    {
        m_Weapons.Clear();
        if (string.IsNullOrEmpty(json))
            return true;

        SaveData? saveData;
        try
        {
            saveData = JsonSerializer.Deserialize<SaveData>(json, s_JsonOptions);
        }
        catch (JsonException)
        {
            return false;
        }

        if (saveData?.m_Weapons is null)
            return false;

        foreach (var (instanceId, data) in saveData.m_Weapons)
        {
            if (data is not null)
                m_Weapons[instanceId] = data;
        }

        return true;
    }

    /// <summary>The shape of the saved text.</summary>
    private sealed class SaveData
    {
        /// <summary>The <see cref="FormatVersion"/> it was saved with.</summary>
        public int m_Version;

        /// <summary>Every weapon's data, by instance ID.</summary>
        public Dictionary<int, WeaponData>? m_Weapons;
    }
}
