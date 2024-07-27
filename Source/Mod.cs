using LocalizationUtilities;

namespace AdaptiveArsenal;

internal sealed class Mod : MelonMod
{
    public override void OnInitializeMelon()
    {
        RegisterLocalizationKeys("AdaptiveArsenal.Resources.Localization.json");
    }
    
    private static void RegisterLocalizationKeys(string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(jsonFilePath)) throw new ArgumentNullException(nameof(jsonFilePath));

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(jsonFilePath);
        if (stream == null) throw new FileNotFoundException($"Resource not found: {jsonFilePath}");

        using var reader = new StreamReader(stream);
        var jsonText = reader.ReadToEnd();

        if (string.IsNullOrWhiteSpace(jsonText)) throw new InvalidDataException("JSON content is empty or whitespace.");

        LocalizationManager.LoadJsonLocalization(jsonText);
    }
}