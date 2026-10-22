using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdaptiveArsenal.Core;

/// <summary>A weapon skin found on disk by <see cref="SkinCatalog"/>, before its images are loaded.</summary>
/// <param name="Id">
/// The skin's ID, saved with each weapon wearing it: its weapon's gear name and its folder's name, like
/// <c>GEAR_Rifle/Arctic</c>. Renaming the folder changes it, so weapons saved with the old ID go back to their default
/// look.
/// </param>
/// <param name="GearName">The gear name of the weapon it's for, like <c>GEAR_Rifle</c>.</param>
/// <param name="Name">The name players see.</param>
/// <param name="TexturePath">The full path of the PNG drawn on the weapon's model.</param>
/// <param name="IconPath">The full path of the PNG shown as the weapon's icon in the inventory.</param>
internal sealed record SkinDefinition(string Id, string GearName, string Name, string TexturePath, string IconPath)
{
    /// <summary>The name of the skin's folder: its <see cref="Id"/> without the gear name, like <c>Arctic</c>.</summary>
    internal string FolderName => Id[(GearName.Length + 1)..];
}

/// <summary>
/// Finds the weapon skins in the skins folder, which has a folder for each weapon's gear name, and in it a folder for
/// each skin:
/// <code>
/// Skins/
///   GEAR_Rifle/
///     Arctic/
///       skin.json     {"name": "Arctic"}
///       texture.png
///       icon.png
/// </code>
/// </summary>
internal static class SkinCatalog
{
    /// <summary>The file in each skin's folder with its name.</summary>
    internal const string ManifestFileName = "skin.json";

    /// <summary>The image in each skin's folder drawn on the weapon's model.</summary>
    internal const string TextureFileName = "texture.png";

    /// <summary>The image in each skin's folder shown as the weapon's icon in the inventory.</summary>
    internal const string IconFileName = "icon.png";

    private static readonly JsonSerializerOptions s_JsonOptions = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Finds every skin in <paramref name="skinsDirectory"/>, sorted by ID. A skin folder with something missing or
    /// wrong is left out, and why is added to <paramref name="problems"/>, so players can be told.
    /// </summary>
    /// <param name="skinsDirectory">The skins folder. If it doesn't exist, there are no skins.</param>
    /// <param name="problems">Why each left-out skin folder was left out.</param>
    internal static List<SkinDefinition> Discover(string skinsDirectory, out List<string> problems)
    {
        var skins = new List<SkinDefinition>();
        problems = [];

        if (!Directory.Exists(skinsDirectory))
            return skins;

        foreach (var gearDirectory in Directory.GetDirectories(skinsDirectory))
        {
            var gearName = Path.GetFileName(gearDirectory);

            foreach (var skinDirectory in Directory.GetDirectories(gearDirectory))
            {
                var id = $"{gearName}/{Path.GetFileName(skinDirectory)}";
                if (TryReadSkin(skinDirectory, id, gearName, out var skin, out var problem))
                    skins.Add(skin);
                else
                    problems.Add($"Skin {id}: {problem}");
            }
        }

        skins.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        return skins;
    }

    /// <summary>Reads one skin's folder.</summary>
    /// <param name="skinDirectory">The skin's folder.</param>
    /// <param name="id">The skin's ID.</param>
    /// <param name="gearName">The gear name of the weapon it's for.</param>
    /// <param name="skin">The skin, or <see langword="null"/> when it can't be read.</param>
    /// <param name="problem">Why it can't be read, or <see langword="null"/>.</param>
    private static bool TryReadSkin(string skinDirectory, string id, string gearName,
        [NotNullWhen(true)] out SkinDefinition? skin, [NotNullWhen(false)] out string? problem)
    {
        skin = null;

        var manifestPath = Path.Combine(skinDirectory, ManifestFileName);
        var texturePath = Path.Combine(skinDirectory, TextureFileName);
        var iconPath = Path.Combine(skinDirectory, IconFileName);

        foreach (var path in (string[])[manifestPath, texturePath, iconPath])
        {
            if (!File.Exists(path))
            {
                problem = $"{Path.GetFileName(path)} is missing.";
                return false;
            }
        }

        Manifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestPath), s_JsonOptions);
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            problem = $"{ManifestFileName} couldn't be read: {exception.Message}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(manifest?.m_Name))
        {
            problem = $"{ManifestFileName} has no name.";
            return false;
        }

        skin = new SkinDefinition(id, gearName, manifest.m_Name.Trim(), texturePath, iconPath);
        problem = null;
        return true;
    }

    /// <summary>The shape of <see cref="ManifestFileName"/>.</summary>
    private sealed class Manifest
    {
        /// <summary>The name players see.</summary>
        [JsonPropertyName("name")]
        public string? m_Name = null;
    }
}
