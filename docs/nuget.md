# Adaptive Arsenal

[Adaptive Arsenal](https://github.com/Deaadman/adaptive-arsenal) is a MelonLoader mod for The Long Dark that replaces rifle and revolver hitscan with projectiles that fly, drop and slow down. This package lets your mod fire the same projectiles from guns of its own, and decide what happens when they hit.

```csharp
using AdaptiveArsenal.Api;

Projectiles.Fire(new ProjectileShot
{
    Position = bullet.transform.position,
    Rotation = bullet.transform.rotation,
    Count = 12,
    Spread = 4f,
    MuzzleVelocity = 380f,
    ResultDelay = 0.25f,
    OnShotFinished = result =>
    {
        // result.Fired projectiles, and result.Hits for every one that hit something in time.
    }
});
```

Players need [Adaptive Arsenal](https://github.com/Deaadman/adaptive-arsenal/releases/latest) installed for the projectiles. If your mod should work without it, mark it as an optional dependency and fall back to your own shots: `Projectiles.Fire` also returns `false` when the player has turned projectiles off.

[API for other mods](https://github.com/Deaadman/adaptive-arsenal/blob/main/docs/api.md) has the details and examples for a rifle, a lever action and a shotgun, and the [changelog](https://github.com/Deaadman/adaptive-arsenal/blob/main/CHANGELOG.md) what's changed in each version.
