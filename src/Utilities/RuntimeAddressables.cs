using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;

using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

using Il2CppLocationList = Il2CppSystem.Collections.Generic.List<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// Lets the game load objects the mod makes in memory by an Addressables key, as if they were in its own content
/// catalog. The game finds every gear item, its data and a gun's ammo by key, so this is how a copy of an item becomes
/// a real one: it can be added to the inventory, saved, loaded and loaded into a gun.
/// </summary>
/// <remarks>
/// <see cref="RuntimeResourceLocator"/> tells Addressables that the keys exist, and
/// <see cref="Patches.ResourceManagerPatches"/> hands back the object when the game loads one. It's the same approach
/// the Shotgun mod's helper library (MehToolBox) uses, with its own keys, so both can run at once.
/// </remarks>
internal static class RuntimeAddressables
{
    /// <summary>Each registered key's object type, and how to make a finished load operation for its object.</summary>
    private static readonly Dictionary<string, Entry> s_Entries = [];

    /// <summary>The locator, once it's been added to Addressables.</summary>
    private static RuntimeResourceLocator? s_Locator;

    /// <summary>Registers <see cref="RuntimeResourceLocator"/> with Il2Cpp. Call it once, when the mod loads.</summary>
    internal static void RegisterTypes() =>
        ClassInjector.RegisterTypeInIl2Cpp<RuntimeResourceLocator>(new RegisterTypeOptions
        {
            Interfaces = new Il2CppInterfaceCollection([typeof(IResourceLocator)])
        });

    /// <summary>
    /// Adds the locator to Addressables, if it hasn't been yet. Addressables must have started, so call it once a
    /// scene has loaded.
    /// </summary>
    internal static void AddLocator()
    {
        if (s_Locator != null)
            return;

        s_Locator = new RuntimeResourceLocator();
        Addressables.AddResourceLocator(s_Locator.Cast<IResourceLocator>());
        Mod.Logger.Log("Added the resource locator for the mod's own items.", FlaggedLoggingLevel.Debug);
    }

    /// <summary>Makes an object loadable by a key.</summary>
    /// <typeparam name="T">The object's type, which the game asks for when it loads the key.</typeparam>
    /// <param name="key">The key, which must not be one the game already uses.</param>
    /// <param name="asset">The object, which must stay loaded, like a template kept with <c>DontDestroyOnLoad</c>.</param>
    internal static void Register<T>(string key, T asset) where T : UnityEngine.Object =>
        s_Entries[key] = new(Il2CppType.Of<T>(), resourceManager =>
            resourceManager.CreateCompletedOperationInternal(asset, true, null!, false));

    /// <summary>Whether a key is one of the mod's.</summary>
    /// <param name="key">The key.</param>
    internal static bool Contains(string? key) => key != null && s_Entries.ContainsKey(key);

    /// <summary>Makes a finished load operation for one of the mod's keys, if its object is of the type asked for.</summary>
    /// <param name="resourceManager">The resource manager loading it.</param>
    /// <param name="key">The key.</param>
    /// <param name="type">The type the game asked for, or <see langword="null"/> for any.</param>
    /// <param name="handle">The operation, whose result is the key's object.</param>
    /// <returns>Whether the key is one of the mod's, with an object of that type.</returns>
    internal static bool TryProvide(ResourceManager resourceManager, string key, Il2CppSystem.Type? type,
        [NotNullWhen(true)] out AsyncOperationHandle? handle)
    {
        handle = TryGet(key, type, out var entry) ? entry.Provide(resourceManager) : null;
        return handle != null;
    }

    /// <summary>Tells Addressables where to find the mod's keys: nowhere real, since the patch provides them.</summary>
    /// <param name="key">The key.</param>
    /// <param name="type">The type the game asked for, or <see langword="null"/> for any.</param>
    /// <param name="locations">One location for the key, or none.</param>
    /// <returns>Whether the key is one of the mod's, with an object of that type.</returns>
    internal static bool TryLocate(string key, Il2CppSystem.Type? type, out Il2CppLocationList locations)
    {
        locations = new Il2CppLocationList();
        if (!TryGet(key, type, out var entry))
            return false;

        // The provider is never used, since the patch answers first, but a location needs one.
        locations.Add(new ResourceLocationBase(key, key, typeof(BundledAssetProvider).FullName, entry.Type).Cast<IResourceLocation>());
        return true;
    }

    /// <summary>
    /// Gets a key's entry, if it's one of the mod's and its object can be used as the type asked for. Otherwise the
    /// game is left to look elsewhere, and finds nothing, as it would for any unknown key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="type">The type the game asked for, or <see langword="null"/> for any.</param>
    /// <param name="entry">The entry.</param>
    private static bool TryGet(string key, Il2CppSystem.Type? type, out Entry entry) =>
        s_Entries.TryGetValue(key, out entry) && (type == null || type.IsAssignableFrom(entry.Type));

    /// <summary>A registered object.</summary>
    /// <param name="Type">The object's Il2Cpp type.</param>
    /// <param name="Provide">Makes a finished load operation whose result is the object.</param>
    private readonly record struct Entry(Il2CppSystem.Type Type, Func<ResourceManager, AsyncOperationHandle> Provide);
}

/// <summary>
/// An Addressables locator for the keys in <see cref="RuntimeAddressables"/>, injected into Il2Cpp so Addressables can
/// call it like its own.
/// </summary>
public sealed class RuntimeResourceLocator : Il2CppSystem.Object
{
    /// <summary>Wraps an existing Il2Cpp object. Il2CppInterop needs this constructor.</summary>
    /// <param name="pointer">The object's pointer.</param>
    public RuntimeResourceLocator(IntPtr pointer) : base(pointer)
    {
    }

    /// <summary>Makes a new locator.</summary>
    public RuntimeResourceLocator() : base(ClassInjector.DerivedConstructorPointer<RuntimeResourceLocator>()) =>
        ClassInjector.DerivedConstructorBody(this);

    /// <summary>The locator's name, for Addressables.</summary>
    public string LocatorId => "AdaptiveArsenal";

    /// <summary>The keys it knows. Addressables only lists them, so none are given.</summary>
    public Il2CppSystem.Collections.Generic.IEnumerable<Il2CppSystem.Object> Keys =>
        new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object>().Cast<Il2CppSystem.Collections.Generic.IEnumerable<Il2CppSystem.Object>>();

    /// <summary>Finds the location of one of the mod's keys.</summary>
    /// <param name="key">The key the game is loading.</param>
    /// <param name="type">The type it asked for.</param>
    /// <param name="locations">The key's location, if it's one of the mod's.</param>
    /// <returns>Whether it's one of the mod's.</returns>
    public bool Locate(Il2CppSystem.Object key, Il2CppSystem.Type type, out Il2CppSystem.Collections.Generic.IList<IResourceLocation> locations)
    {
        var found = RuntimeAddressables.TryLocate(key?.ToString() ?? string.Empty, type, out var list);
        locations = list.Cast<Il2CppSystem.Collections.Generic.IList<IResourceLocation>>();
        return found;
    }
}
