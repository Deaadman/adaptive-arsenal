# API for other mods

> **Draft.** This API is new and hasn't been tried by another mod yet, so it may still change before a release. `Projectiles.ApiVersion` goes up with any change that could break a mod using it.

Adaptive Arsenal replaces rifle and revolver shots with bullets that fly, drop and slow down. Other mods can fire the same kind of projectile for guns of their own through `AdaptiveArsenal.Api`, and decide what happens when it hits.

## What's in it

- `Projectiles.Fire(ProjectileShot)` fires one projectile. A gun that fires pellets fires one for each. It returns `false`, firing nothing, when the player has turned real bullets off or the projectile couldn't be made, so your mod can fire its own shot instead.
- `Projectiles.Enabled` says whether real bullets are on.
- `Projectiles.TryGetAim(vp_FPSShooter, out position, out rotation)` finds where a gun's shots start and where it's aimed, from its sights, the way Adaptive Arsenal does for rifles and revolvers. The first-person weapon is drawn by its own camera, so its sights aren't where they appear to be in the world.
- `Projectiles.ApiVersion` is the version of the API.

`ProjectileShot` describes the projectile:

| Property | What it does |
| --- | --- |
| `Position`, `Rotation` | Where it's fired from and the direction, with any spread already applied. |
| `MuzzleVelocity` | Its speed at the muzzle in metres per second. The player's bullet speed setting scales it, as it does the mod's own bullets (150 to 550). |
| `Drag` | How quickly the air slows it, as the Unity rigidbody's drag. Higher slows it sooner, so it drops more at range. The mod's own bullets use 0.1. |
| `Damage`, `WeaponSource` | For the mod to damage animals when `OnHit` is empty: the damage at the muzzle, which falls with distance, and the weapon body parts scale it for. |
| `AmmoGearName` | The ammo whose single round it looks like in flight, like `GEAR_RifleAmmoSingle`. Empty is invisible. |
| `PlayImpactEffects` | Whether it plays the game's impact particles, decal and sound. Turn it off to play your own. |
| `OnHit` | Called with a `ProjectileHit` when it hits something, instead of the mod damaging the animal. |
| `OnFinished` | Called once when it's done: `true` right after a hit, `false` when it's removed without one. |

`ProjectileHit` has the `Collider` hit, the `Point` and `Normal`, the `Direction` and `Speed` it was flying at, the `Distance` from where it was fired, and `DamageAtDistance`: `Damage` after the mod's falloff, for using it with your own damage.

Without `OnHit`, the mod damages the animal hit the way it does for its own bullets, with body-part scaling, bleeding and critical hits for `WeaponSource`. Only the game's own rifle and revolver count towards their skills and statistics. Either way, the projectile stops where it hits and its trail fades out. It doesn't push loose gear for other mods' guns, since it doesn't know how hard they should; do it in `OnHit` if you want it.

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
        if (AdaptiveArsenal.Api.Projectiles.ApiVersion != 1 || !AdaptiveArsenal.Api.Projectiles.Enabled)
            return false;

        var shot = new PelletShot(pellets);
        for (var i = 0; i < pellets; i++)
        {
            var roll = Random.Range(0f, 360f);
            var yaw = Random.Range(-spread, spread);
            var rotation = bullet.transform.rotation * Quaternion.Euler(0f, 0f, roll) * Quaternion.Euler(0f, yaw, 0f);

            AdaptiveArsenal.Api.Projectiles.Fire(new AdaptiveArsenal.Api.ProjectileShot
            {
                Position = bullet.transform.position,
                Rotation = rotation,
                MuzzleVelocity = muzzleVelocity,
                Drag = drag,
                Damage = bullet.Damage,
                PlayImpactEffects = false,
                OnHit = shot.AddHit,
                OnFinished = shot.PelletFinished
            });
        }

        return true;
    }
}
```

Here `PelletShot` is your own class: `AddHit` adds up each pellet's damage on the animal it hit, and `PelletFinished` applies it all once the last pellet is done, so thresholds like "this many pellets for a bleed" work across a whole shot.

A good place to call it is the prefix where your mod already handles the game's `vp_Bullet` for its gun. Return `false` from the prefix and destroy the game's bullet when `TryFire` succeeds, and carry on with your own hitscan when it doesn't.

## Changes

- **1**: `Projectiles.Fire`, `Enabled`, `TryGetAim`, `ProjectileShot` and `ProjectileHit`.
