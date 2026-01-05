using BuildInfo = AdaptiveArsenal.Properties.BuildInfo;

[assembly: MelonInfo(typeof(AdaptiveArsenal.Mod), BuildInfo.Name, BuildInfo.Version, BuildInfo.Author, BuildInfo.DownloadLink)]
[assembly: MelonGame("Hinterland", "TheLongDark")]
[assembly: MelonPriority(BuildInfo.Priority)]
[assembly: MelonIncompatibleAssemblies("TargetPracticeAndMasterHunter")]
[assembly: VerifyLoaderVersion(BuildInfo.MelonLoaderVersion, true)]