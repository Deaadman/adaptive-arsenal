# API for other mods

> **Draft.** This API is new and hasn't been tried by another mod yet, so it may still change before a release. `Projectiles.ApiVersion` goes up with any change that could break a mod using it.

Adaptive Arsenal replaces rifle and revolver shots with bullets that fly, drop and slow down. Other mods can fire the same kind of projectile for guns of their own through `AdaptiveArsenal.Api`, and decide what happens when it hits.

## What's in it

- `Projectiles.Fire(ProjectileShot)` fires a shot: one projectile, or all of a shell's pellets at once. Call it once per shot. It returns `false`, firing nothing, when the player has turned real bullets off or the projectiles couldn't be made, so your mod can fire its own shot instead.
- `Projectiles.Enabled` says whether real bullets are on.
- `Projectiles.TryGetAim(vp_FPSShooter, out position, out rotation)` finds where a gun's shots start and where it's aimed, from its sights, the way Adaptive Arsenal does for rifles and revolvers. The first-person weapon is drawn by its own camera, so its sights aren't where they appear to be in the world.
- `Projectiles.ApiVersion` is the version of the API.

`ProjectileShot` describes the shot:

| Property | What it does |
| --- | --- |
| `Position`, `Rotation` | Where it's fired from and where the gun is aimed. |
| `Count` | How many projectiles to fire, like a shell's pellets. The default is 1. |
| `Spread` | The furthest each projectile can stray from the aim, in degrees. Each gets its own random direction within it, spread evenly. The default is 0. |
| `MuzzleVelocity` | Its speed at the muzzle in metres per second. The player's bullet speed setting scales it, as it does the mod's own bullets (150 to 550). |
| `Drag` | How quickly the air slows it, as the Unity rigidbody's drag. Higher slows it sooner, so it drops more at range. The mod's own bullets use 0.1. |
| `Damage`, `WeaponSource` | For the mod to damage animals when `OnShotFinished` is empty: the damage at the muzzle, which falls with distance, and the weapon body parts scale it for. |
| `AmmoGearName` | The ammo whose single round it looks like in flight, like `GEAR_RifleAmmoSingle`. Empty is invisible. |
| `PlayImpactEffects` | Whether it plays the game's impact particles, decal and sound. Turn it off to play your own. |
| `OnShotFinished` | Called once for the whole shot with a `ProjectileShotResult`, instead of the mod damaging the animals hit. |
| `ResultDelay` | The most seconds to wait for the projectiles before calling `OnShotFinished`. Any still flying then count as misses, so a pellet into the sky doesn't hold up the rest. The default is 0.5; 0 waits for every one. |

`ProjectileShotResult` has how many projectiles were `Fired`, and the `Hits`: one `ProjectileHit` for each projectile that hit something in time. Dividing the hits on an animal by `Fired` gives how much of the shot it took, for scaling bleeding and critical hits.

`ProjectileHit` has the `Collider` hit, the `Point` and `Normal`, the `Direction` and `Speed` it was flying at, the `Distance` from where it was fired, and `DamageAtDistance`: `Damage` after the mod's falloff, for using it with your own damage.

Projectiles fired through the API never count as shots, hits or skill, however many there are. Without `OnShotFinished`, each one damages the animal it hits the way the mod's own bullets do, with body-part scaling, bleeding and critical hits for `WeaponSource`. Either way, each projectile stops where it hits and its trail fades out. It doesn't push loose gear for other mods' guns, since it doesn't know how hard they should; do it in `OnShotFinished` if you want it.

Call everything from the game's main thread. Exceptions your callbacks throw are logged, and don't stop the projectile.

## Using it from your mod

Until there's a NuGet package, reference `AdaptiveArsenal.dll` from a release, and don't copy it into your mod's output.

```xml
<Reference Include="AdaptiveArsenal">
    <HintPath>path\to\AdaptiveArsenal.dll</HintPath>
    <Private>false</Private>
</Reference>
```

If your mod should still work without Adaptive Arsenal, say it's optional, so MelonLoader loads it first when it's there:

```csharp
[assembly: MelonOptionalDependencies("AdaptiveArsenal")]
```

Then keep every use of its types in one class, and only call that class once you know Adaptive Arsenal is loaded. .NET loads a type's references when a method using it first runs, so a mod that only touches them inside that class doesn't fail when the DLL is missing.

```csharp
internal static class AdaptiveArsenalSupport
{
    internal static bool IsLoaded { get; } =
        MelonBase.RegisteredMelons.Any(melon => melon.Info.Name == "Adaptive Arsenal");
}

// Only called when AdaptiveArsenalSupport.IsLoaded is true.
internal static class AdaptiveArsenalPellets
{
    internal static bool TryFire(vp_Bullet bullet, int pellets, float spread, float muzzleVelocity, float drag)
    {
        if (AdaptiveArsenal.Api.Projectiles.ApiVersion != 1)
            return false;

        // One call fires every pellet.
        return AdaptiveArsenal.Api.Projectiles.Fire(new AdaptiveArsenal.Api.ProjectileShot
        {
            Position = bullet.transform.position,
            Rotation = bullet.transform.rotation,
            Count = pellets,
            Spread = spread,
            MuzzleVelocity = muzzleVelocity,
            Drag = drag,
            Damage = bullet.Damage,
            ResultDelay = 0.25f,
            OnShotFinished = ApplyShot
        });
    }

    private static void ApplyShot(AdaptiveArsenal.Api.ProjectileShotResult result)
    {
        foreach (var animal in result.Hits.GroupBy(hit => AiUtils.GetBaseAiFromObject(hit.Collider.gameObject)))
        {
            if (animal.Key == null)
                continue;

            var coverage = animal.Count() / (float)result.Fired;
            var damage = animal.Sum(hit => hit.DamageAtDistance);
            // Scale bleeding and the chance of a critical hit by coverage, then apply it all once.
        }
    }
}
```

`ApplyShot` gets every pellet's hit at once, so it can add up the damage on each animal, work out how many of the pellets hit it, and apply the damage, bleeding and critical hits once for the whole shot.

A good place to call it is the prefix where your mod already handles the game's `vp_Bullet` for its gun. Return `false` from the prefix and destroy the game's bullet when `TryFire` succeeds, and carry on with your own hitscan when it doesn't.

## Changes

- **1**: `Projectiles.Fire`, `Enabled`, `TryGetAim`, `ProjectileShot`, `ProjectileShotResult` and `ProjectileHit`.
