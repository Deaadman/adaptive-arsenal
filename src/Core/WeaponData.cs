namespace AdaptiveArsenal.Core;

/// <summary>
/// What the mod remembers about one weapon, kept in a <see cref="WeaponDataStore"/> and saved with the game. A feature
/// that needs to remember something for each weapon, like its skin or how cold it is, adds a public field here.
/// </summary>
/// <remarks>
/// Public fields are saved under their names, so renaming one loses what players have saved in it. A field added
/// later starts at its default for weapons saved before it existed, so pick defaults that mean "nothing has happened".
/// </remarks>
internal sealed class WeaponData
{
    /// <summary>
    /// The weapon's prefab name, like <c>GEAR_Rifle</c>, so data saved for one item isn't handed to a different kind of
    /// item that ends up with the same instance ID.
    /// </summary>
    public string m_GearName = string.Empty;

    /// <summary>
    /// The <see cref="SkinDefinition.Id"/> of the skin the weapon wears, or <see langword="null"/> for its own look.
    /// </summary>
    public string? m_Skin = null;
}
