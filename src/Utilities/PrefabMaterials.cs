using UnityEngine.AddressableAssets;

namespace AdaptiveArsenal.Utilities;

/// <summary>
/// Borrows materials from the game's own prefabs, so the mod's effects match the game's look.
/// </summary>
internal static class PrefabMaterials
{
    /// <summary>
    /// Gets the material of the <see cref="LineRenderer"/> on a child of a gear item's prefab.
    /// </summary>
    /// <param name="gearItemName">The gear item whose prefab to load, like <c>GEAR_Arrow</c>.</param>
    /// <param name="childName">The name of the child object with the <see cref="LineRenderer"/>.</param>
    /// <returns>The material, or <see langword="null"/> if there's no such child or it has no <see cref="LineRenderer"/>.</returns>
    internal static Material? GetLineRendererMaterial(string gearItemName, string childName)
    {
        // Reading the prefab doesn't run anything on it. The game's GearItem.LoadGearItemPrefab went in 2.58.
        var prefab = Addressables.LoadAssetAsync<GameObject>(gearItemName).WaitForCompletion();
        if (prefab == null)
            return null;

        var child = prefab.transform.Find(childName);
        if (child == null)
            return null;

        // Unity objects need == null, not ?., to tell when they've been destroyed.
        var lineRenderer = child.GetComponent<LineRenderer>();
        return lineRenderer != null ? lineRenderer.material : null;
    }
}
