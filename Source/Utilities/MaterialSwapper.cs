namespace AdaptiveArsenal.Utilities;

internal static class MaterialSwapper
{
    /// <summary>
    /// Retrieves the <see cref="Material"/> used by the <see cref="LineRenderer"/> component of a specified child
    /// object within a gear item prefab.
    /// </summary>
    /// <remarks>This method assumes that the gear item prefab can be loaded using the provided name. If the
    /// specified child object does not exist or does not have a <see cref="LineRenderer"/> component, the method
    /// returns <see langword="null"/>.</remarks>
    /// <param name="gearItemName">The name of the gear item prefab to load.</param>
    /// <param name="childName">The name of the child object to search for within the prefab.</param>
    internal static Material? GetLineRendererMaterialFromGearItemPrefab(string gearItemName, string childName)
    {
        Transform childTransform = GearItem.LoadGearItemPrefab(gearItemName).transform.Find(childName);
        if (childTransform == null) return null;

        LineRenderer lineRenderer = childTransform.GetComponent<LineRenderer>();

        return lineRenderer?.material;
    }
}