using Il2CppInterop.Runtime.Attributes;

namespace AdaptiveArsenal.Components;

/// <summary>
/// Swaps the texture on a weapon's model for a skin's, and back. It goes on a weapon's <see cref="GearItem"/>, and on
/// each first-person model of it, which every weapon of the same kind shares.
/// </summary>
/// <remarks>
/// <para>
/// The skin replaces the main texture of every material on the renderers it's given, or of the ones picked out by
/// the filter <see cref="Apply"/> is given. The first time it does, each
/// renderer gets its own copies of its materials, so other weapons using the same materials keep their look, and the
/// original textures are kept to put back.
/// </para>
/// <para>
/// <see cref="Utilities.WeaponSkins"/> decides which skin each weapon wears and calls <see cref="Apply"/>.
/// </para>
/// </remarks>
[RegisterTypeInIl2Cpp(false)]
public class WeaponSkin : MonoBehaviour
{
    /// <summary>The materials the skin is drawn on, each with its own texture to put back.</summary>
    private readonly List<(Material Material, Texture? Original)> m_Materials = [];

    /// <summary>Whether <see cref="m_Materials"/> has been filled in.</summary>
    private bool m_HasMaterials;

    /// <summary>How many materials the skin is drawn on, once <see cref="Apply"/> has found them.</summary>
    internal int MaterialCount => m_Materials.Count;

    /// <summary>The textures the skin replaces, once <see cref="Apply"/> has found them.</summary>
    [HideFromIl2Cpp]
    internal IEnumerable<Texture> OriginalTextures =>
        m_Materials.Select(entry => entry.Original).Where(original => original != null)!;

    /// <summary>Draws a skin's texture on the model, or puts the model's own textures back.</summary>
    /// <param name="renderers">The model's renderers, used the first time it's called.</param>
    /// <param name="texture">The skin's texture, or <see langword="null"/> for the model's own look.</param>
    /// <param name="includeMaterial">
    /// Which of the renderers' materials the skin is drawn on, used the first time it's called. All of them if
    /// <see langword="null"/>.
    /// </param>
    [HideFromIl2Cpp]
    internal void Apply(IEnumerable<Renderer> renderers, Texture2D? texture, Func<Material, bool>? includeMaterial = null)
    {
        // Nothing to undo, and nothing to copy the materials for.
        if (!m_HasMaterials && texture == null)
            return;

        if (!m_HasMaterials)
        {
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                // Reading Renderer.materials gives the renderer its own copies of its materials.
                foreach (var material in renderer.materials)
                {
                    if (material != null && (includeMaterial == null || includeMaterial(material)))
                        m_Materials.Add((material, material.mainTexture));
                }
            }

            m_HasMaterials = true;
        }

        // Set even when the texture hasn't changed, in case the game has put a material's own texture back since.
        foreach (var (material, original) in m_Materials)
        {
            if (material != null)
                material.mainTexture = texture == null ? original : texture;
        }
    }
}
