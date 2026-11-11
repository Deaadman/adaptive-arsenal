# Weapon mechanics

Research notes on how real rifles and revolvers handle, compared with how The Long Dark handles them, for planning what the mod could change. These notes are about the guns themselves, such as loading, cycling, misfires and the cold. Bullets in flight are covered in [How it works](../features.md).

Nothing here is in the mod yet. Each page ends with ideas, and changing gameplay is a decision for each one.

| Page | What it covers |
| --- | --- |
| [Rifles](rifles.md) | Working the bolt, the chamber and magazine, loading and unloading, misfires and jams, the safety, sights |
| [Revolvers](revolvers.md) | Single and double action, misfires, loading designs, carrying safely |
| [All guns](general.md) | The cold, what wears a gun out, recoil, the trigger, timings, sights |

## Where the game's side comes from

The game's side is read from `refs/game`, the game decompiled with Cpp2IL. Its method bodies are mostly stubs that list the calls they make, so the notes go by type, field and method names, and mark guesses as *(guess)*. Check them in game before building on them.

The main types are:

- `GunItem`: magazine, condition, misfires, sway, recoil, timings and ammo types
- `vp_FPSWeapon`: aiming, sway from breathing and the cold, reloading and unjamming
- `vp_FPSShooter`: firing, dry firing and loading rounds
- `MisfireTable`: the chance of a misfire at each condition
- `Cleanable`: cleaning a gun to restore its condition

## Most worth doing

These would change how guns feel the most, and fit a survival game:

1. **Manual bolt cycling** on rifles, with a separate round in the chamber.
2. **The cold affecting the gun**, not just the shooter.
3. **Revolver misfires** that just move on to the next chamber.
