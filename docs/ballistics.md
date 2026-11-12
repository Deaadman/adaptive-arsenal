# Ballistics

Research notes on how real bullets fly and hit, compared with how the mod's bullets do, for planning the projectile overhaul. For how the bullets work now from a player's point of view, see [How it works](features.md).

Nothing here is in the mod yet. Changing gameplay is a decision for each idea.

## What the mod does now

- **Flight.** Each bullet is a Unity rigidbody ([`ProjectileItem`](../src/Components/ProjectileItem.cs)). Unity's gravity pulls it down, and a rigidbody drag of 0.1 slows it.
- **Muzzle velocity.** Each weapon has one muzzle velocity ([`Ballistics.MuzzleVelocities`](../src/Core/Ballistics.cs)): 350 to 550 m/s for rifles and 150 to 300 m/s for revolvers.
- **Direction.** The bullet leaves along the line from the rear sight to the front sight ([`ProjectileUtilities`](../src/Utilities/ProjectileUtilities.cs)), turned by a random spread.
- **Spread.** The spread is anywhere in a circle of up to 10°, every point in it as likely as any other, and smaller as accuracy rises.
- **Hits.** The bullet stops at the first thing it hits.
- **Damage.** Damage falls in a straight line with distance travelled, to 10% of the muzzle damage at 500 m.

## How real bullets differ

### Drag

The air slows a real bullet in proportion to the square of its speed. How much also depends on the bullet's shape and weight, which its ballistic coefficient sums up, and drag changes sharply around the speed of sound.

Unity's rigidbody drag takes the same share of the speed off every second, whatever the speed: about 10% a second at 0.1. So a real rifle bullet slows hard at first, losing roughly 40% of its speed over its first 500 m, while ours barely slow at the speeds where real ones slow most. And every bullet slows the same way, whatever its shape or weight.

### Muzzle velocity

Real muzzle velocities are about twice the mod's:

| | Mod | Real |
| --- | --- | --- |
| Rifles (like the .303 or .30-06) | 350–550 m/s | roughly 740–870 m/s |
| Revolvers (like the .38 or .357) | 150–300 m/s | roughly 270–440 m/s |

At 400 m/s, a bullet drops about 1.2 m over 200 m. A real rifle bullet drops about 0.4 m. The slower bullets may be on purpose, so players can see drop and lead, but the gap is large.

### Sight zeroing

Our bullets leave along the line of sight, so they start dropping below the point of aim straight away and always land low.

A real gun's sights are set at a slight angle to the barrel, so the bullet rises through the line of sight and comes back down to it at the zero distance, often 100 m. Out to about 150–200 m it lands close to where you aimed without holding over.

### Damage

What a real bullet does depends on its energy when it hits, which goes with the square of its speed, and on how the bullet is built. A rifle bullet still has about a third of its energy at 500 m, so the mod's fall to 10% is much steeper than reality.

Damage also ignores speed, so the **Bullet speed** setting changes drop and flight time but not damage. A slow bullet at 100 m does as much damage as a fast one.

### Spread

A real rifle's own spread is tiny, about 1–4 MOA (0.02–0.07°), and shots bunch towards the middle rather than spreading evenly. Most real misses come from the shooter: sway, breathing and jerking the trigger. The game already makes the sights sway, so the mod's spread of up to 10° partly counts the shooter's error twice.

### Wind

A crosswind pushes a bullet sideways: tens of centimetres for a rifle at 300 m, and more for slower revolver bullets. The game has its own wind, but bullets ignore it.

### Ricochets and penetration

Real bullets glance off rock, ice and water when they hit at a shallow angle. They can go through thin wood, be deflected by brush, and often pass right through an animal. The mod's bullets stop at the first collider they touch.

### The cold

Cold air is denser, so it adds drag. Cold powder burns slower, so bullets leave the barrel slower.

### Smaller differences

- **The bullet doesn't follow its arc.** Nothing turns the rigidbody, so the bullet's mesh keeps facing the way it was fired. A real bullet's spin keeps it nose first along its path.
- **Mass does nothing.** The rigidbody's mass is 0.02 kg, but the bullet is launched with a set velocity and Unity's drag ignores mass, so it has no effect.
- **Trails.** Real bullets can't be seen, unless they're tracers. The trail helps players see where a shot went, so it's worth keeping.
- **Spin drift and the Earth's rotation** push a real bullet slightly sideways, but too little to matter at the game's ranges.

## Overhaul plan

Each part below says what will change and why, and ends with what was decided. Every recommendation was approved on 12 November 2026. The numbers are starting points to tune in game, not final values. Figures marked *(estimate)* are worked out by hand, and the real ones will come from the simulation and its tests.

### 1. The mod flies the bullet itself

**Change.** Stop using a Unity rigidbody to move the bullet. Each physics step, a function in `src/Core` works out the bullet's new position and velocity from gravity, drag and wind. [`ProjectileItem`](../src/Components/ProjectileItem.cs) then raycasts along that step, as it already does, and moves the bullet there.

**Why.**
- Unity's drag can't grow with speed. Adding the right force to the rigidbody each step would work, but it would split the physics between Unity and the mod.
- Zeroing (part 4) has to work out the launch angle by simulating the same flight. If the game and the zeroing use the same code, the bullet really does land on the point of aim at the zero distance.
- In `src/Core`, flight can be tested without the game: drop at 100 m, time to target, speed left at 300 m.
- The raycast already finds every hit, so the rigidbody, its collider and `OnCollisionEnter` aren't needed. That also removes the 0.02 kg mass that does nothing.

**Also.** The bullet turns to face along its path each step, so its mesh follows the arc.

**Recommendation.** Do it. It's the base for drag, zeroing and wind.

**Decided.** The mod flies the bullet itself, without a rigidbody.

### 2. Drag that grows with speed

**Change.** Drag uses the standard G1 drag table, which gives drag at each speed relative to the speed of sound, and a ballistic coefficient (BC) for each kind of ammo. The bullet is the same whichever gun fires it, so the BC belongs to the ammo, while muzzle velocity belongs to the gun (part 3).

| Ammo | BC (G1) |
| --- | --- |
| Rifle (`GEAR_RifleAmmoSingle`) | 0.40 |
| Revolver (`GEAR_RevolverAmmoSingle`) | 0.15 |

These are typical of a round-nosed .303 or .30-06 hunting bullet and a lead .38 revolver bullet. The game doesn't say what calibres its guns are, so these are guesses at what fits.

**Recommendation.** G1 for both. G7 fits modern long, pointed rifle bullets better, but the game's guns are older designs and G1 is the usual table for round-nosed bullets.

**Decided.** G1 for both, with the BCs above.

### 3. Muzzle velocities

**Change.** Raise them to about real values. Different guns firing the same ammo still differ, mostly through barrel length, so the stub-nosed revolver stays the slowest.

| Weapon | Now (m/s) | Proposed (m/s) |
| --- | --- | --- |
| Trader's (Bunker) rifle | 550 | 820 |
| Curator's rifle | 500 | 780 |
| Hunting rifle | 400 | 740 |
| Barbara's rifle | 400 | 740 |
| Vaughn's rifle | 350 | 700 |
| Revolver (`GEAR_RevolverFancy`) | 300 | 330 |
| Revolver (`GEAR_Revolver`, `GEAR_RevolverGreen`) | 200 | 270 |
| Revolver (`GEAR_RevolverStubNosed`) | 150 | 240 |

Today the rifles range from 350 to 550 m/s. The proposal narrows that gap, because real rifles firing the same round don't differ that much. The order stays the same, so each rifle keeps its place.

**Gameplay.** Bullets drop much less and arrive sooner. With zeroing (part 4), a hunting rifle lands within about 10 cm of the point of aim out to about 150 m *(estimate)*, so lead and holdover only matter on long shots.

**Recommendation.** Use the proposed numbers, keeping the **Bullet speed** setting for players who want slower bullets they can see drop.

**Decided.** The proposed velocities, keeping the **Bullet speed** setting.

### 4. Sights zeroed at a set distance

**Change.** Each gun is zeroed at a set distance. When it fires, the mod works out the small upward angle that makes the bullet cross the line of sight at that distance, using the flight from part 1, and adds it to the shot's direction.

| Gun | Zero distance |
| --- | --- |
| Rifles | 100 m |
| Revolvers | 25 m |

- The angle depends on the muzzle velocity, the BC and the **Bullet speed** setting. It's cheap enough to work out again for each shot.
- It ignores wind and the cold, as a real zero does, so those still move the bullet off the point of aim.
- Hip-fire shots get the same angle, so they still go where the camera points at the zero distance.
- Shots fired by other mods through the API aren't zeroed unless they ask (part 10).
- The bullet still starts at the front sight. Real bores sit a few centimetres below the sights, but at the game's ranges that hardly matters.

**Recommendation.** Do it, with the distances above and no setting. A zero setting is easy to add later if players want it.

**Decided.** Rifles zeroed at 100 m and revolvers at 25 m, with no setting.

### 5. Damage from the speed it hits at

**Change.** Replace the straight-line drop-off by distance with the bullet's energy when it hits, as a share of its energy at the muzzle: `(impact speed ÷ muzzle speed)²`.

For the hunting rifle, from the simulation:

| Distance | Now | Proposed |
| --- | --- | --- |
| 100 m | 82% | 80% |
| 200 m | 64% | 66% |
| 300 m | 46% | 53% |
| 500 m | 10% | 34% |

A revolver bullet keeps about 89% at 50 m.

**Gameplay.** Rifle damage stays about the same as now out to 200 m, and is higher beyond it. Speed now matters to damage, so the **Bullet speed** setting changes it too: slower bullets do less at range.

**Settings.**
- Remove **Drop-off distance**, since distance no longer sets damage.
- Replace **Long-range damage** with **Damage from speed**, from 0 to 1. At 1 damage follows the energy share fully, and at 0 there's no drop-off. It blends between the two.

**Recommendation.** Energy share, with the **Damage from speed** setting defaulting to 1.

**Decided.** Damage follows the energy share, with the settings as above.

### 6. Spread

**Change.**
- **Centre-weighted.** Shots bunch towards the point of aim, in a normal distribution, instead of landing anywhere in a circle with equal chance. Nothing lands further out than the worst spread.
- **Smaller.** **Worst spread** defaults to 3° instead of 10°. Today a shot with an accuracy of 0 can land 17 m off at 100 m, far beyond anything real.
- **A floor.** Every gun keeps its own small spread, even at full skill: 2 MOA (0.03°) for rifles and 5 MOA (0.08°) for revolvers. Today a level 5 shooter is perfectly accurate.
- **Skill still matters.** Rifle and Revolver skill, hip fire, standing and moving still set the spread as now, on top of the floor.

**Recommendation.** All four. The game's sway already makes aiming hard, so the random spread can be much smaller.

**Decided.** All four: centre-weighted, a worst spread of 3°, a floor for each gun, and skill still setting the spread.

### 7. Wind

**Change.** Drag works on the bullet's speed through the air rather than over the ground, so the game's wind pushes it sideways and, into a headwind, slows it more. The wind comes from `Wind.GetWindDirection()` and `Wind.GetSpeedMPH()`, read once when the bullet is fired.

Roughly, in a 10 m/s (22 mph) crosswind *(estimate)*: a rifle bullet moves about half a metre at 300 m, and a revolver bullet a few centimetres at 50 m.

**To check in game.** Whether the game's wind is already zero indoors (`Wind` calls `Weather.IsIndoorEnvironment`), and whether gusts make a wind read at one moment misleading.

**Recommendation.** Do it, with the next part under one setting.

### 8. The cold

**Change.**
- **Air density** follows the air temperature, so cold air adds drag. At −30 °C, air is about 19% denser than at 15 °C.
- **Powder** burns slower in the cold: about 0.5 m/s less muzzle velocity per degree below 15 °C *(estimate)*. That's about 20 m/s, or 3%, at −30 °C.
- The temperature is the air's, from `Weather`, without wind chill. Which of `GetCurrentTemperature` and `GetCurrentTemperatureWithoutHeatSources` gives that has to be checked in game.

Both effects are small, but they cost little once part 1 is done.

**Recommendation.** Do it. Put wind and the cold under one setting, **Weather affects bullets**, on by default.

**Decided.** Wind and the cold, under one setting, **Weather affects bullets**, on by default.

### 9. Ricochets

**Change.** A bullet that hits stone, metal, ice or water at a shallow angle, under about 10° from the surface, glances off instead of stopping. It keeps about 60% of its speed, heads off in a slightly random direction and can still hit something. It plays the impact effects where it glances off.

**Not planned.** Penetration through wood or brush, because the game doesn't tell the mod how thick anything is. Bullets passing through animals, because it rarely matters with one animal at a time.

**Recommendation.** Do ricochets last, after the rest is tuned. They depend on the game tagging surfaces with the right material, which needs checking in game.

**Decided.** Ricochets in this overhaul, done last. Penetration left out.

### 10. The API for other mods

Other mods fire projectiles through [`Projectiles.Fire`](../src/Api/Projectiles.cs) with a [`ProjectileShot`](../src/Api/ProjectileShot.cs), whose `Drag` is the rigidbody's drag. A change that breaks them would need a new major version, so:

- **Add `BallisticCoefficient`**, using the G1 table. While it's 0, the default, `Drag` works as before: a fixed share of the speed lost each second.
- **Add `ZeroDistance`**, which zeroes the shot as in part 4. 0, the default, fires straight along `Rotation` as now.
- **Wind and the cold** apply to every projectile, through the API or not, while the setting is on.
- **Damage** for API shots follows part 5, as `ProjectileHit.Damage` already reports.

**Recommendation.** Add both, so existing mods behave as before.

**Decided.** Add both properties, keeping `Drag` working.

### Settings

| Setting | Change |
| --- | --- |
| Bullet speed | Kept |
| Worst spread | Default 10° → 3° |
| Drop-off distance | Removed |
| Long-range damage | Replaced by **Damage from speed**, default 1 |
| Weather affects bullets | New, default on |

### Order of work

1. Flight in `src/Core` with drag, the new muzzle velocities and zeroing, then `ProjectileItem` using it (parts 1–4). These go together, because each changes how far the bullet drops.
2. Damage from speed (part 5).
3. Spread (part 6).
4. Wind and the cold (parts 7 and 8).
5. The API (part 10), alongside step 1 where it overlaps.
6. Ricochets (part 9).

Each step adds tests to `tests/AdaptiveArsenal.Tests`, updates [How it works](features.md) and adds a line to `CHANGELOG.md`, and is checked in game.

### Progress

All six steps are in the code, with tests, on the `1.2.0/projectiles-overhaul` branch. None of them has been checked in game yet.

Where the code differs from the plan above, after review:

- **Damage for other mods' shots.** Shots fired through the API with only linear `Drag` keep 1.1.0's falloff by distance, since linear drag barely slows them and damage by speed would hardly fall with range. Shots with a `BallisticCoefficient` follow part 5.
- **The weather for other mods' shots.** Rather than applying to every projectile, it follows `ProjectileShot.AffectedByWeather`, which defaults to on with a `BallisticCoefficient` and off with only `Drag`, so mods written for 1.1.0 fly exactly as before.
- **`ProjectileHit.Damage`** replaces `DamageAtDistance`, which stays as an obsolete alias, since its value now follows speed.
- **Damage uses the slowest speed** the bullet reached before it hit, so speed gravity adds late in a high arc doesn't raise it again.
- **Settings from 1.1.0 are updated once**, by `SettingsMigration` in `src/Core`: Long-range damage carries over to Damage from speed, and Worst spread moves from 10° to 3° only if every accuracy setting was left at its default.
- **The API's spread stays even**, through `Ballistics.EvenSpreadOffset`, since pellets should cover the whole spread. Only the mod's own guns get the centre-weighted spread.
- **Ricochets leave water out**, since the game has no water impact type to tell it apart. Bullets glance off rock, metal and ice, and surfaces without a material tag stop them, since what they are isn't known. A bullet glances off at most twice, and a ricochet's lost speed lowers its damage through damage by speed.

To check in game:

- Shots land on target at 100 m with a rifle and 25 m with a revolver.
- Bullets still hit animals, the ground and loose gear without a collider, and don't stop on the player.
- Bullets drift downwind, not upwind. If they drift upwind, turn the direction round in `ShotWeather.WindVelocity`.
- `Weather.GetCurrentTemperatureWithoutHeatSources` gives the air's temperature in °C, without fires or wind chill.
- A settings file saved by 1.1.0 is updated as described above.
- Bullets glance off rock at a shallow angle, and the ricochet's impact sound plays where it glanced off, from an object the game's audio didn't set up.
- With **Log shots** on, shots are logged with Complex Logger's levels all off. They're logged at its `Always` level, which is assumed to show whatever levels the player turned on.
- **Draw bullet paths** draws in the right colours. It needs the `Sprites/Default` or `Hidden/Internal-Colored` shader in the game's build. Without either it falls back to the trail's material, and the colours may not show.

The Debug settings help with these checks (see [Debugging](features.md#debugging)). **Draw bullet paths** shows the zero, the drift with the wind and each ricochet. **Log shots** logs the game's raw wind direction and temperature with every shot.
