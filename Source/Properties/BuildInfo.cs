namespace AdaptiveArsenal.Properties;

internal static class BuildInfo
{
    #region Mandatory
    
    /// <summary>The machine-readable name of the mod (no special characters or spaces)</summary>
    public const string Name                            = 
        "AdaptiveArsenal";
    
    /// <summary>Who made the mod</summary>
    public const string Author                          = 
        "Deadman";
    
    /// <summary>Current version (e.g. 1.0.0, 1.0.0-Alpha, 1.0.0-Beta, 1.0.0-RC, etc.) </summary>
    public const string Version                         = 
        "1.0.4-DeveloperBuild";
    
    /// <summary>Version shown in DnSpy</summary>
    public const string AssemblyVersion                 = 
        //$"{Version}.0"
        "1.0.4.0";
    
    /// <summary>Name used on GUI's, like ModSettings</summary>
    public const string GUIName                         = 
        "Adaptive Arsenal";
    
    /// <summary>The minimum Melon Loader version that your mod requires.</summary>
    public const string MelonLoaderVersion              = 
        "0.6.1";
    
    #endregion

    #region Optional
    /// <summary>What the mod does</summary>
    public const string Description                     = 
        "Adaptive Arsenal is a modification that improves upon existing weapon mechanics within The Long Dark.";
    
    /// <summary>Company that made it</summary>
    public const string Company                         = 
        null!;
    
    /// <summary>A valid download link</summary>
    public const string DownloadLink                    = 
        null!;
    
    /// <summary>Copyright info</summary>
    /// <remarks>When updating the year, use the StartYear-CurrentYear format</remarks>
    public const string Copyright                       = 
        "Copyright © 2024";
    
    /// <summary>Trademark info</summary>
    public const string Trademark                       = 
        null!;
    
    /// <summary>Product Name (Generally use the Name)</summary>
    public const string Product                         = 
        Name;
    
    /// <summary>Culture info</summary>
    public const string Culture                         = 
        null!;
    
    /// <summary>Priority of your mod. Most of the time you should not need to change this</summary>
    public const int Priority                           = 
        0;
    
    #endregion
}