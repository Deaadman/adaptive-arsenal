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

At a score of 0, a shot can land up to 10° away from where you aimed. At 100 or more, it lands exactly where you aimed. So at skill level 5, rifles and revolvers are fully accurate.

## Muzzle velocity

Each weapon fires its bullets at its own speed. Faster bullets reach the target sooner and drop less on the way.

| Weapon | Game ID | Muzzle velocity |
| --- | --- | --- |
| Trader's (Bunker) rifle | `GEAR_Rifle_Traders` | 1100 |
| Curator's rifle | `GEAR_Rifle_Curators` | 1000 |
| Hunting rifle | `GEAR_Rifle` | 800 |
| Barbara's rifle | `GEAR_Rifle_Barbs` | 800 |
| Vaughn's rifle | `GEAR_Rifle_Vaughns` | 700 |
| Revolver | `GEAR_RevolverFancy` | 600 |
| Revolver | `GEAR_Revolver`, `GEAR_RevolverGreen` | 400 |
| Revolver | `GEAR_RevolverStubNosed` | 300 |

These are the mod's own values, not metres per second.

## Damage drop-off

A bullet does the most damage close up. Its damage falls steadily with distance, to a tenth at 500 metres. Where it hits still matters, as it does without the mod, and a hit can still kill outright.

Hits count towards your Rifle or Revolver skill and the wildlife hits statistic, as they do without the mod.

## Revolver movement

Without the mod, aiming a revolver stops you moving. With it, you can walk while you aim.
