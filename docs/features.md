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

## Manual bolt

Without the mod, a rifle cycles its bolt by itself after every shot. With it, the bolt stays closed on the spent case until you pull the trigger again, which cycles it. The pull after that fires. Keep the trigger held after a shot to stay still with the bolt closed. Lowering the rifle, putting it away or reloading cycles the bolt too.

Press **V** to work the bolt without firing. After a shot, it cycles the bolt, the same as pulling the trigger again. Otherwise, it ejects the round in the chamber unfired and chambers the next one, clearing the round the same way the game clears a jammed one. It does nothing while the safety is on. It works even with **Manual bolt** turned off.

A rifle also holds one more round, in the chamber, on top of its magazine. The hunting rifle holds 11 instead of 10. The HUD shows at most the magazine's rounds.

## Safety

Rifles have a safety. Press **B** to put it on or off, and the HUD says which. While it's on, pulling the trigger does nothing, and the bolt can't be worked: the rifle doesn't fire, and its bolt doesn't cycle. Each time you take a rifle out, its safety is on, so take it off before you need to shoot. Revolvers have no safety.

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
| Manual bolt | On | Cycle a rifle's bolt by pulling the trigger again after each shot, with one more round in the chamber. The extra round applies from the next time you load a save. |
| Cycle bolt key | V | Works a rifle's bolt without firing. |
| Safety | On | Gives rifles a safety. While it's on, the trigger does nothing and the bolt can't be worked. |
| Safety key | B | Puts the safety on or off. |
| Safety on when equipped | On | Puts a rifle's safety on each time you take it out. Off, it's off each time. |
| Walk while aiming revolver | On | Lets you walk while aiming a revolver. |
| Show ammo count | On | Shows the loaded and carried rounds on the HUD, with the spent round animation. Off, you keep count yourself. |
