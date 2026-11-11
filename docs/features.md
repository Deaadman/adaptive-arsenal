# How it works

Adaptive Arsenal changes the rifles and revolvers. The flare gun, bow and other weapons work as they do without the mod.

## Real bullets

Without the mod, a shot hits the instant you fire. With it, each shot is a bullet that flies through the world. It takes time to reach a far-off target, and it stops at the first thing it hits.

A trail follows each bullet, up to 200 metres behind it, so you can see where it went.

## Accuracy

Each shot lands a little away from where you aimed. How far depends on an accuracy score, from 0 to 100:

- **Rifles** start from your Rifle skill.
- **Revolvers** start from your Revolver skill (40, 50, 60, 80 or 100 for levels 1 to 5), plus the revolver's own accuracy.

Then the score is multiplied:

| | Multiplier |
| --- | --- |
| Every shot | × 1.2 |
| Firing from the hip | × 0.7 |
| Standing (not crouching) | × 0.8 |
| Moving | × 0.9 |

At a score of 0, a shot can land up to 10° away from where you aimed, in any direction. At 100 or more, it lands exactly where you aimed. So at skill level 5, rifles and revolvers are fully accurate.

## Muzzle velocity

Each weapon fires its bullets at about the speed of a real gun like it. Faster bullets reach the target sooner and drop less on the way.

| Weapon | Game ID | Muzzle velocity (m/s) |
| --- | --- | --- |
| Trader's (Bunker) rifle | `GEAR_Rifle_Traders` | 820 |
| Curator's rifle | `GEAR_Rifle_Curators` | 780 |
| Hunting rifle | `GEAR_Rifle` | 740 |
| Barbara's rifle | `GEAR_Rifle_Barbs` | 740 |
| Vaughn's rifle | `GEAR_Rifle_Vaughns` | 700 |
| Revolver | `GEAR_RevolverFancy` | 330 |
| Revolver | `GEAR_Revolver`, `GEAR_RevolverGreen` | 270 |
| Revolver | `GEAR_RevolverStubNosed` | 240 |

## Drag

The air slows bullets as it does real ones: much more at high speed, and most of all just above the speed of sound. A rifle bullet keeps a little over half its speed at 500 metres. Revolver bullets are blunter, so they slow more for their speed, but they're slow to start with and keep most of it over a revolver's range.

## Zeroed sights

Each gun's sights are set so its bullets cross the line of sight at a set distance, as a real gun's are:

| Gun | Zero distance |
| --- | --- |
| Rifles | 100 m |
| Revolvers | 25 m |

The bullet leaves the barrel aimed a little above the sights, rises to meet the line of sight at the zero distance, then falls below it. A rifle lands within about 10 cm of where you aimed out to 150 metres. Further out, aim high.

## Damage drop-off

A bullet does its weapon's full damage close up, the same as the game gives that weapon. Its damage falls steadily with distance, to a tenth at 500 metres. Where it hits still matters, as it does without the mod, and a hit can still kill outright, with the game's own chance for that weapon.

Hits count towards your Rifle or Revolver skill and the wildlife hits statistic, as they do without the mod. Bullets also knock loose items around, as the game's own bullets do.

## Revolver movement

Without the mod, aiming a revolver stops you moving. With it, you can walk while you aim, and the HUD no longer shows the limited-movement icon for it. Rifles still stop you moving while you aim.

## Ammo count

The HUD shows how many rounds are loaded in your rifle or revolver, and how many more you're carrying, as the game does, and each shot plays an animation of the spent round. With **Show ammo count** off, it hides them, so you have to keep count. It still shows the icon for the type of ammo the weapon takes.

## Settings

Everything above is how the mod plays by default. To change it, open **Options**, then **Mod Settings**, then **Adaptive Arsenal**. The settings are saved in `Mods\AdaptiveArsenal.json`.

| Setting | Default | What it does |
| --- | --- | --- |
| Real bullets | On | Off, rifles and revolvers fire the game's own instant shots, and the bullet settings below are hidden. |
| Bullet trails | On | Shows the trail behind each bullet. |
| Trail length | 200 m | How far behind the bullet its trail reaches. |
| Bullet speed | × 1 | Multiplies every weapon's muzzle velocity. |
| Base accuracy | × 1.2 | Multiplies the accuracy of every shot. |
| Hip fire, Standing, Moving | × 0.7, × 0.8, × 0.9 | Multiply accuracy in those situations. 1 means no penalty. |
| Worst spread | 10° | How far the least accurate shot can land from where you aimed. |
| Drop-off distance | 500 m | How far a bullet flies before its damage has fallen as far as it will. |
| Long-range damage | 0.1 | How much of its damage a bullet still does at that distance. 1 means no drop-off. |
| Walk while aiming revolver | On | Lets you walk while aiming a revolver. |
| Show ammo count | On | Shows the loaded and carried rounds on the HUD, with the spent round animation. Off, you keep count yourself. |
