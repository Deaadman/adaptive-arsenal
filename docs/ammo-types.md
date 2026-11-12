# Ammo types

A plan for the ammo types the rifle and revolver could take besides their standard cartridges, and why. It's a
reference for development, not a description of the mod as it plays: [How it works](features.md) covers that.

Each type is based on a real one, and is only here if it gives players a real choice in the game.

## Planned types

The numbers are starting points, compared with standard ammo, to tune in game.

| Ammo | Guns | Real life | In game | Trade-off | Needs | Order |
| --- | --- | --- | --- | --- | --- | --- |
| Standard | Rifle, revolver | The game's own cartridges | Unchanged | None | Nothing | Already there |
| Tracer | Rifle | A burning compound in the base shows the bullet's path | A red glow and light in flight, otherwise identical | None: it only looks different | Done, as a proof of concept | 1 |
| Soft-point | Rifle | An exposed lead tip that mushrooms when it hits | About ×1.25 damage, faster bleed-out | Damage falls off sooner with distance | Damage, bleed-out and drop-off for each ammo type | 2 |
| Hollow point | Revolver | A hollow tip that opens up when it hits | About ×1.3 damage, faster bleed-out | Much weaker at range | The same as soft-point | 2 |
| Shot shell | Revolver | A capsule of small pellets, sold as "snake shot" | About 8 pellets spread about 4°, for rabbits and birds close up | Useless beyond about 5 to 10 m | A pellet count and spread for each ammo type | 3 |

## Notes on each type

### Tracer

Tracers let a shooter see where shots land at long range, which is why they're made for rifles and machine guns.
The glow is on the bullet, not its trail: a burning tracer looks like a short red streak, so the mod draws one over
the distance the bullet covered since the last frame, with a small red light. The trail stays as it is for every
bullet. Tracers only glow with **Real bullets** on, since with it off there's no bullet to glow.

The current tracer cartridge is a copy of the game's rifle cartridge, made in memory and tinted red (see
`src/Utilities/TracerAmmo.cs`). It's a proof of concept until it comes from an asset bundle with its own model and icon.

### Soft-point and hollow point

Both are expanding bullets, and would share the same rules in the mod. They have different names because each is
what that gun's hunting round really is:

- Rifle bullets hit fast enough that a soft lead tip mushrooms on its own, and a hollow cavity can break the bullet
  apart. Soft-point is the classic hunting round for rifles like the .303.
- Revolver bullets are much slower, around 250 m/s, and a soft tip barely expands at that speed, so they need the
  hollow cavity.

They give a choice between range and stopping power: standard ammo for long shots, expanding ammo for a cleaner kill
up close. The mod already works out each hit's damage and bleed-out time in `ProjectileItem.TryInflictDamage`, so
the ammo type can scale both there.

### Shot shell

A niche round for small game at a few metres. It's the only type that fires more than one projectile from the
player's guns, which `ProjectileGroup` already supports for other mods' shotguns.

## Left out

| Ammo | Why |
| --- | --- |
| Revolver tracer | Rare in real life, since revolvers are short-range and there's little need to see where shots land. Easy to add later by copying `GEAR_RevolverAmmoSingle` the same way, if both guns should match. |
| Subsonic | The mod's rifles already fire close to the speed of sound, and its revolvers below it. A subsonic rifle round is only worth having if quieter shots scare animals less, and that hasn't been looked into. |
| High-velocity (+P) | Overlaps with what makes the Trader's and Curator's rifles special: their faster bullets. |
| Full metal jacket | Its point is that it goes through what it hits, and the mod's bullets stop at the first thing they hit. |

## How they'd be built

1. **Stats for each ammo type.** A profile in `src/Core` with multipliers for damage, bleed-out, damage drop-off,
   muzzle velocity and spread, and a pellet count, looked up by the ammo's gear name and covered by tests.
   `ProjectileItem` applies it when it fires, from the ammo that's loaded (`TracerAmmo.IsLoaded` shows how). Ammo
   without a profile works as it does today. The API could let other mods register profiles for their own ammo.
2. **The items.** Each type is a cartridge the game's own ammo switching chooses between, as the tracer is. The proof
   of concept copies the game's cartridges in memory (`src/Utilities/RuntimeAddressables.cs`). The real items will
   come from an asset bundle, with their own models and icons.
3. **Getting them.** Not decided yet: crafted at the ammo workbench, found as loot, or both. For now, tracers can be
   added with the developer console's `add GEAR_RifleAmmoSingleTracer`.

## Still to check in game

- Whether a gun's selected ammo and the rounds in its clip can differ while switching. `TracerAmmo.IsLoaded` assumes
  the clip holds the previous type until the game unloads it.
- Whether rifles accept tracers as a second ammo type. The log says which ammo type number each rifle gives them.
- Whether the copied cartridge saves, loads and stacks on its own, without mixing with standard rounds.
