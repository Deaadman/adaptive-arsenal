namespace AdaptiveArsenal.Utilities;

/// <summary>
/// The <c>aa_skin</c> command in the game's developer console, to try skins on the weapon in the player's hands
/// without going through the <see cref="Components.SkinPicker"/> in the inventory.
/// </summary>
/// <remarks>
/// <c>aa_skin</c> lists the skins for the weapon in hands, <c>aa_skin &lt;folder&gt;</c> puts one on, and
/// <c>aa_skin default</c> takes it off. The console needs a mod that opens it, like Developer Console.
/// </remarks>
internal static class SkinConsoleCommand
{
    private const string Command = "aa_skin";

    /// <summary>Adds the command to the console.</summary>
    internal static void Register()
    {
        try
        {
            uConsole.RegisterCommand(Command, "Lists or sets the skin of the weapon in your hands: aa_skin [folder|default]", new Action(Run));
        }
        catch (Exception exception)
        {
            Mod.Logger.Log($"Couldn't add the {Command} console command: {exception.Message}", FlaggedLoggingLevel.Warning);
        }
    }

    private static void Run()
    {
        var gearItem = GameManager.GetPlayerManagerComponent().m_ItemInHands;
        if (gearItem == null)
        {
            uConsole.Log("Hold the weapon to change its skin.");
            return;
        }

        var skins = WeaponSkins.GetSkinsFor(gearItem);

        if (uConsole.GetNumParameters() == 0)
        {
            if (skins.Count == 0)
                uConsole.Log($"There are no skins for {gearItem.DisplayName} in {WeaponSkins.SkinsDirectory}.");

            foreach (var skin in skins)
                uConsole.Log($"{skin.FolderName}: {skin.Name}");

            return;
        }

        var folder = uConsole.GetString();
        if (string.Equals(folder, "default", StringComparison.OrdinalIgnoreCase))
        {
            WeaponSkins.SetSkin(gearItem, null);
            uConsole.Log($"{gearItem.DisplayName} has its own look again.");
            return;
        }

        var match = skins.FirstOrDefault(skin => string.Equals(skin.FolderName, folder, StringComparison.OrdinalIgnoreCase));
        if (match == null || !WeaponSkins.SetSkin(gearItem, match.Id))
        {
            uConsole.Log($"There's no skin {folder} for {gearItem.DisplayName}. Type {Command} to list them.");
            return;
        }

        uConsole.Log($"{gearItem.DisplayName} now wears {match.Name}.");
    }
}
