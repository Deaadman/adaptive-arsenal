# Revolvers

How a real revolver works, compared with the game's revolvers.

[Overview](README.md) · [Rifles](rifles.md) · [All guns](general.md)

## What the game does now

What the game does is read from `refs/game`, which is mostly stubs, so parts of it are guesses. Those are marked *(guess)*.

- The hammer seems to be cocked when you aim and let down when you stop, since there's an "uncock" sound (`GunItem.m_UncockAudio`, `MaybeUncockAudio`). So every aimed shot gets the light single-action trigger. *(guess)*
- The cylinder is one list of rounds (`GunItem.m_Clip`). Spent casings stay in it until you reload (`NumSpentCasingsInClip`), as they do in a real revolver.
- Rounds load one at a time, each with its own animation.
- A misfire or jam is cleared with an unjam animation, the same as on a rifle. *(It's not clear whether a revolver can just fire the next chamber instead.)*
- The revolver can be fired during a struggle with an animal (`GunItem.StruggleRevolverFire`).
- There's no choice between single and double action.

## How a real revolver differs

### Single action and double action

There are two ways to fire a revolver, and many can do both.

| | Single action | Double action |
| --- | --- | --- |
| How | Thumb the hammer back, then pull the trigger | Pull the trigger, which cocks and drops the hammer in one go |
| Trigger | Short and light | Long and heavy |
| Accuracy | Better: the light pull barely moves the gun | Worse: the heavy pull pulls the muzzle off target |
| Speed | Slower: you cock before each shot | Faster: just keep pulling |

Some revolvers are single action only, so the hammer has to be cocked for every shot.

### Misfires cost only one pull

Each time the hammer is cocked, the cylinder turns to the next chamber. After a dud round, the shooter just pulls again and fires the next one. The dud stays in its chamber until the revolver is unloaded.

This is the main reason revolvers were seen as reliable. A misfire on a rifle means working the bolt. On a revolver it means one more pull.

### Loading and unloading

How a revolver loads depends on its design.

- **Swing-out cylinder.** The cylinder swings out to the side. One push on the ejector rod throws out every case at once, and then all six chambers are open to load. A speed loader puts all six in at once.
- **Loading gate.** A small gate on the side opens onto one chamber at a time. The shooter turns the cylinder to each chamber in turn, pushes the case out with the ejector rod, and loads a round. It's slow, but rounds can be topped up one at a time.
- **Break-top.** The frame hinges open and throws out every case at once, like a swing-out cylinder.

### Carrying safely

Older single-action revolvers have no safety, and the firing pin rests on the round under the hammer. A knock to the hammer can fire it. They were usually carried with the hammer on an empty chamber, so they held five rounds in practice, not six.

## Ideas for the mod

- Let players choose between cocking the hammer (accurate, slower) and firing double action (fast, less accurate).
- On a misfire, let the next pull fire the next chamber, and keep the dud in the cylinder until reloading.
- Load to match the revolver's design: all at once for a swing-out cylinder, one by one for a loading gate.
