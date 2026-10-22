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

Each weapon fires its bullets at its own speed. Faster bullets reach the target sooner and drop less on the way.

| Weapon | Game ID | Muzzle velocity (m/s) |
| --- | --- | --- |
| Trader's (Bunker) rifle | `GEAR_Rifle_Traders` | 550 |
| Curator's rifle | `GEAR_Rifle_Curators` | 500 |
| Hunting rifle | `GEAR_Rifle` | 400 |
| Barbara's rifle | `GEAR_Rifle_Barbs` | 400 |
| Vaughn's rifle | `GEAR_Rifle_Vaughns` | 350 |
| Revolver | `GEAR_RevolverFancy` | 300 |
| Revolver | `GEAR_Revolver`, `GEAR_RevolverGreen` | 200 |
| Revolver | `GEAR_RevolverStubNosed` | 150 |

## Damage drop-off

A bullet does its weapon's full damage close up, the same as the game gives that weapon. Its damage falls steadily with distance, to a tenth at 500 metres. Where it hits still matters, as it does without the mod, and a hit can still kill outright, with the game's own chance for that weapon.

Hits count towards your Rifle or Revolver skill and the wildlife hits statistic, as they do without the mod. Bullets also knock loose items around, as the game's own bullets do.

## Revolver movement

Without the mod, aiming a revolver stops you moving. With it, you can walk while you aim, and the HUD no longer shows the limited-movement icon for it. Rifles still stop you moving while you aim.

## Frozen rifles

Rifles you carry freeze in the cold. Below 0°C they freeze, faster the colder it is: at -20°C or colder, a rifle freezes solid in 6 in-game hours. Above 0°C they thaw, taking about 2 hours at 10°C, slower just above freezing and up to three times as fast by a fire. Indoors and near a fire count, since the game's temperature includes them. A rifle left on the ground or in a container stays as it was until you pick it up.

The inventory shows how frozen a rifle is after its name, like frozen clothing. A frozen rifle is more likely to jam: a frozen-solid one has an extra 25% chance on every shot, on top of the game's own chance for its condition, and a partly frozen one gets part of that. Revolvers don't freeze.

## Ammo count

The HUD doesn't show how many rounds are loaded in your rifle or revolver, or how many more you're carrying, so you have to keep count. It still shows the icon for the type of ammo the weapon takes.

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
| Frozen rifles | On | Off, rifles don't freeze, and the frozen rifle settings below are hidden. |
| Hours to freeze | 6 | In-game hours for a rifle to freeze solid at -20°C or colder. |
| Hours to thaw | 2 | In-game hours for a frozen-solid rifle to thaw at 10°C. |
| Frozen jam chance | 25% | The extra chance that a frozen-solid rifle jams on each shot. |
| Walk while aiming revolver | On | Lets you walk while aiming a revolver. |
| Show ammo count | Off | Shows the loaded and carried rounds on the HUD, with the spent round animation. |
