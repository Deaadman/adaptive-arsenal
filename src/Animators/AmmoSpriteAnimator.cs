using AdaptiveArsenal.Utilities;

using System.Collections;

using Il2CppInterop.Runtime.Attributes;

namespace AdaptiveArsenal.Animators;

/// <summary>
/// Plays the animation of a spent round flying off the HUD's ammo display when a rifle or revolver is fired.
/// </summary>
/// <remarks>
/// <see cref="Patches.EquipItemPopupPatches"/> adds this to the <see cref="EquipItemPopup"/>, the HUD panel that
/// shows the rounds in the weapon, and starts <see cref="AnimateCasingEjection"/> each time a round is used.
/// </remarks>
[RegisterTypeInIl2Cpp(false)]
public class AmmoSpriteAnimator : MonoBehaviour
{
    /// <summary>How long the animation lasts, in seconds.</summary>
    private const float EjectionDuration = 1f;

    /// <summary>How fast the round spins, in degrees per second.</summary>
    private const float EjectionSpinSpeed = 360f;

    /// <summary>How far right the round lands from where it starts, in HUD pixels.</summary>
    private const float EjectionEndOffsetX = 100f;

    /// <summary>How far up the round lands from where it starts, in HUD pixels: it lands below.</summary>
    private const float EjectionEndOffsetY = -150f;

    /// <summary>The Bézier control point of the round's arc, right of where it starts, in HUD pixels.</summary>
    private const float EjectionArcOffsetX = 50f;

    /// <summary>The Bézier control point of the round's arc, above where it starts, in HUD pixels.</summary>
    private const float EjectionArcOffsetY = 100f;

    /// <summary>Draws the round above the rest of the ammo display.</summary>
    private const int RoundSpriteDepth = 30;

    private GameObject? m_RoundSpriteTemplate;
    private readonly List<GameObject> m_EjectingRounds = [];

    /// <summary>
    /// Animates one spent round arcing up and away from the ammo display while it spins, then removes it.
    /// </summary>
    /// <param name="startPosition">Where the round starts, local to <paramref name="parent"/>: the slot of the round that was used.</param>
    /// <param name="parent">The transform the ammo display's round sprites belong to.</param>
    /// <param name="gunType">The gun that was fired, which picks the round's icon.</param>
    /// <returns>A coroutine for <see cref="MelonCoroutines.Start"/>.</returns>
    [HideFromIl2Cpp]
    internal IEnumerator AnimateCasingEjection(Vector3 startPosition, Transform parent, GunType gunType)
    {
        // Unity objects need == null, not is null, to tell when they've been destroyed.
        if (m_RoundSpriteTemplate == null)
            yield break;

        var round = Instantiate(m_RoundSpriteTemplate, parent);

        m_EjectingRounds.Add(round);
        round.transform.localPosition = startPosition;
        round.SetActive(true);

        var sprite = round.GetComponent<UISprite>();
        sprite.depth = RoundSpriteDepth;
        sprite.spriteName = gunType switch
        {
            GunType.Rifle => "ico_ammo_rifle",
            GunType.Revolver => "ico_ammo_revolver",
            _ => ""
        };

        var end = new Vector3(startPosition.x + EjectionEndOffsetX, startPosition.y + EjectionEndOffsetY, startPosition.z);
        var control = new Vector3(startPosition.x + EjectionArcOffsetX, startPosition.y + EjectionArcOffsetY, startPosition.z);

        var elapsed = 0f;
        while (elapsed < EjectionDuration)
        {
            round.transform.localPosition = MathUtils.QuadraticBezier(startPosition, control, end, elapsed / EjectionDuration);
            round.transform.Rotate(0, 0, EjectionSpinSpeed * Time.deltaTime);

            elapsed += Time.deltaTime;
            yield return null;
        }

        m_EjectingRounds.Remove(round);
        Destroy(round);
    }

    /// <summary>
    /// Copies one of the ammo display's own round sprites, so the ejected round looks the same: the second, or the
    /// first if there's only one. With none, rounds aren't animated.
    /// </summary>
    private void Start()
    {
        var ammoSprites = GetComponent<EquipItemPopup>().m_ListAmmoSprites;
        if (ammoSprites.Length == 0)
        {
            Mod.Logger.Log("The HUD's ammo display has no round sprites, so spent rounds aren't animated.", FlaggedLoggingLevel.Warning);
            return;
        }

        m_RoundSpriteTemplate = ammoSprites[Math.Min(1, ammoSprites.Length - 1)].gameObject;
    }

    /// <summary>Removes any rounds still mid-animation when the HUD panel is hidden.</summary>
    private void OnDisable()
    {
        foreach (var round in m_EjectingRounds)
            Destroy(round);

        m_EjectingRounds.Clear();
    }
}
