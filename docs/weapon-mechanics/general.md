# All guns

How real firearms behave in ways that apply to both rifles and revolvers, compared with the game.

[Overview](README.md) · [Rifles](rifles.md) · [Revolvers](revolvers.md)

## What the game does now

What the game does is read from `refs/game`, which is mostly stubs, so parts of it are guesses. Those are marked *(guess)*.

- **Sway.** Aiming sways more as you tire (`GunItem.m_SwayValueZeroFatigue`, `m_SwayValueMaxFatigue`) and the longer you hold your aim. Breathing moves the sights, cold makes your hands shake and your teeth chatter, and kneeling steadies you (`vp_FPSWeapon.m_KneelingBonusPitch`, `m_ColdShakeAngle`). When aiming stamina runs out, the gun drops.
- **Recoil.** Each shot kicks up and sideways by a random amount within a set range (`GunItem.m_PitchRecoilMin` to `m_PitchRecoilMax`, and the same for yaw).
- **Timings.** You have to wait after starting to aim before you can fire (`m_FireDelayOnAim`), and after reloading (`m_FireDelayAfterReload`).
- **Condition.** Guns lose condition, which raises the chance of a misfire. A cleaning kit restores it (`Cleanable`). I'm guessing condition drops with every shot. *(guess)*
- **Ammo.** Each round has its own condition. Some guns take more than one type of ammo, and switching unloads the old type (`GunItem.PressSwitchAmmo`).
- **Hip fire.** Some guns allow it (`m_AllowHipFire`).

## How real guns differ

### The cold

This is the biggest gap for a game set in a frozen wilderness. At the moment the cold affects the shooter but not the gun.

- **Oil thickens.** In deep cold, lubricant turns to grease. Bolts and hammers move slowly, and the firing pin can hit too lightly to fire the round.
- **Condensation freezes.** Carrying a cold gun into a warm room covers it in moisture inside and out. Taking it back into the cold freezes that moisture, which can lock the action. Real hunters leave their rifles in the porch or a cold room to avoid this.
- **Snow in the barrel.** A fall can pack snow into the muzzle. Firing with the barrel blocked can bulge or burst it.
- **Weaker powder.** Cold powder burns slower, so bullets leave the barrel slower.
- **Gloves.** Thick mittens make the trigger hard to feel and loading fiddly.

### What wears a gun out

A well-kept gun barely wears over a few hundred shots. In reality, guns fail mostly through neglect, not use:

- rust from damp, snow or condensation
- dirt and fouling built up from firing without cleaning
- damage from being dropped or knocked

Ammo is tough too, but rounds that get wet or corroded are more likely to fail.

### Recoil

Real recoil is mostly the same every shot: straight back, then the muzzle rises. How far it moves depends on how the shooter holds the gun and their stance more than on chance. Shooters learn to:

- hold the gun firmly against the shoulder, or in a firm grip for a revolver
- follow through, keeping the sights on target through the shot
- not flinch in expectation of the recoil, which pushes the muzzle down just before the shot

### The trigger

How the trigger is pulled is itself a cause of misses. Jerking it pulls the muzzle off target, and a smooth squeeze doesn't. Trigger weight matters too: a heavy pull is harder to keep steady.

### Timings

A real gun fires the instant the trigger is pulled. There's no wait after raising it or reloading, except the time the shooter takes to settle the sights. A rushed shot is just a less accurate one.

### Sights

Iron sights mean lining up the rear sight, the front sight and the target, and the eye can only focus on one of them. The front sight is sharp and the target blurs, which makes small or distant targets harder to see. Bad light, such as dusk or a blizzard, makes the sights hard to see at all.

## Ideas for the mod

- Make the cold affect the gun: slower actions, light strikes, frozen actions after warm-to-cold, snow in the barrel.
- Wear guns through neglect and weather rather than by shot count.
- Make recoil consistent, with the player's stance and steadiness deciding how much it moves.
- Let players fire as soon as they want, with accuracy depending on how settled the aim is.
