namespace AdaptiveArsenal.Utilities;

/// <summary>
/// Calls game methods whose signature changed between the game version the NuGet package has and the one players
/// run, so one build works with both and CI can still build against the package.
/// </summary>
/// <remarks>
/// For now that's <see cref="PlayerAnimation.IsAllowedToFire"/>, which game version 2.58 gave a
/// <see cref="GunType"/> parameter. The newest <c>STBlade.Modding.TLD.Il2CppAssemblies.Windows</c> is from 2.55, so
/// the method is found once by reflection and called through a delegate. Once a 2.58 package is out and referenced,
/// call the method directly and remove this.
/// </remarks>
internal static class GameCompatibility
{
    /// <summary>Game version 2.58 and later: <c>IsAllowedToFire(bool allowHipFire, GunType gunType)</c>.</summary>
    private static readonly Func<PlayerAnimation, bool, GunType, bool>? s_IsAllowedToFireWithGunType =
        CreateDelegate<Func<PlayerAnimation, bool, GunType, bool>>(typeof(bool), typeof(GunType));

    /// <summary>Before game version 2.58: <c>IsAllowedToFire(bool allowHipFire)</c>.</summary>
    private static readonly Func<PlayerAnimation, bool, bool>? s_IsAllowedToFire =
        s_IsAllowedToFireWithGunType == null ? CreateDelegate<Func<PlayerAnimation, bool, bool>>(typeof(bool)) : null;

    /// <summary>Whether the warning that neither method was found has been logged.</summary>
    private static bool s_LoggedMissing;

    /// <summary>
    /// Whether the player can fire now, as the game decides. Returns <see langword="false"/> if the game has neither
    /// version of the method, so the mod leaves the shot to the game.
    /// </summary>
    /// <param name="playerAnimation">The player's animation component.</param>
    /// <param name="allowHipFire">Whether the gun can be fired without aiming.</param>
    /// <param name="gunType">The gun, which only game version 2.58 and later look at.</param>
    internal static bool IsAllowedToFire(PlayerAnimation playerAnimation, bool allowHipFire, GunType gunType)
    {
        if (s_IsAllowedToFireWithGunType != null)
            return s_IsAllowedToFireWithGunType(playerAnimation, allowHipFire, gunType);

        if (s_IsAllowedToFire != null)
            return s_IsAllowedToFire(playerAnimation, allowHipFire);

        if (!s_LoggedMissing)
        {
            s_LoggedMissing = true;
            Mod.Logger.Log("This game version has no PlayerAnimation.IsAllowedToFire the mod knows, so it leaves shots to the game.", FlaggedLoggingLevel.Error);
        }

        return false;
    }

    /// <summary>
    /// A delegate for the <see cref="PlayerAnimation.IsAllowedToFire"/> with these parameters, or
    /// <see langword="null"/> if this game version doesn't have it.
    /// </summary>
    /// <typeparam name="T">The delegate type, taking the <see cref="PlayerAnimation"/> first.</typeparam>
    /// <param name="parameterTypes">The method's parameter types.</param>
    private static T? CreateDelegate<T>(params Type[] parameterTypes) where T : Delegate
    {
        var method = typeof(PlayerAnimation).GetMethod(nameof(PlayerAnimation.IsAllowedToFire),
            BindingFlags.Public | BindingFlags.Instance, null, parameterTypes, null);
        return method != null && method.ReturnType == typeof(bool) ? (T)Delegate.CreateDelegate(typeof(T), method) : null;
    }
}
