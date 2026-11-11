# Rifles

How a real bolt-action rifle works, compared with the game's rifles. The game's hunting rifle looks like a Lee-Enfield, so that's the reference here.

[Overview](README.md) · [Revolvers](revolvers.md) · [All guns](general.md)

## What the game does now

What the game does is read from `refs/game`, which is mostly stubs, so parts of it are guesses. Those are marked *(guess)*.

- The bolt cycles by itself after each shot, at the gun's firing rate (`GunItem.m_FiringRateSeconds`). *(guess)*
- The magazine is one list of rounds (`GunItem.m_Clip`, up to `m_ClipSize`). There's no separate round in the chamber.
- Rounds load one at a time, each with its own animation (`vp_FPSShooter.OnBulletLoaded`), and a reload can be cancelled (`vp_FPSWeapon.CancelReload`).
- Spent casings drop at the player's feet (`GunItem.HandleShellDischarge`).
- Misfires and jams are one failure. A `MisfireTable` gives a chance of failure from condition, and the player clears it with an unjam animation (`vp_FPSWeapon.UnJam`). *(The table's input may be the gun's condition or the round's.)*
- There's no safety.
- The sights are fixed.

## How a real rifle differs

### Working the bolt

On a real rifle, cycling the bolt is a separate action from firing, and the shooter chooses when to do it.

- **Staying still.** After a shot, you can leave the bolt closed and stay quiet and still, so you don't spook what you're hunting, then cycle when it's safe.
- **Staying on target.** A practised shooter cycles with their eye still in the sights and their cheek still on the stock. A new shooter lifts their head and has to find the target again.
- **Short-stroking.** If the bolt isn't pulled fully back, the spent case doesn't eject or the next round doesn't feed. The next trigger pull then fires nothing, or the bolt won't close.
- **Extraction comes from the bolt.** The empty case comes out when you open the bolt, not when the shot fires.

### The chamber and the magazine

The chamber holds one round, separate from the magazine.

- A rifle holds a full magazine **plus one** in the chamber.
- With a full magazine and an empty chamber, pulling the trigger only clicks. You have to cycle the bolt first.
- Whether a round is chambered is something the shooter keeps track of, and something they choose: carrying with a round chambered is quicker but less safe.

### Loading

- A Lee-Enfield loads through the top of the action: 5 rounds at once from a stripper clip, or one round at a time.
- The magazine can be topped up at any time, without emptying it first.
- Some rifles have a **magazine cutoff**, which stops the magazine feeding. The shooter loads single rounds by hand and keeps the magazine full in reserve.

### Unloading

Unloading means cycling the bolt once for each round, with each one coming out of the action. In the snow, you'd catch them or pick them up.

### Misfires and jams

These are different failures, cleared in different ways.

| Failure | What happens | What the shooter does |
| --- | --- | --- |
| Misfire | The primer doesn't fire, so the trigger clicks and nothing happens | Keeps the gun pointed safely for a few seconds in case the round goes off late (a hangfire), then cycles the bolt and loses that round |
| Failure to feed | The next round doesn't go into the chamber, or jams halfway | Opens the bolt and clears the round by hand |
| Failure to extract | The spent case stays in the chamber | Pulls it out, sometimes with a tool or cleaning rod |

### Safety

Real rifles have a safety that blocks the trigger or the firing pin. Putting it on and taking it off is part of carrying and aiming the gun. Forgetting it off risks an accidental shot. Forgetting it on means a click when it matters.

### Sights

Many rifles like this have a rear sight with a range setting, a ladder or a slider marked in hundreds of yards. Setting it for the distance lifts the muzzle so the bullet lands on the aim point. Without it, the shooter aims high by eye.

## Ideas for the mod

- Make cycling the bolt its own action, and let players hold it.
- Track a round in the chamber, separate from the magazine.
- Let rounds be added to a partly full magazine.
- Split misfires (lose the round, possible hangfire) from jams (clear by hand).
- A safety that players can toggle.
- A rear sight that can be set for distance.
