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

Unity's rigidbody drag takes the same share of the speed off every second, whatever the speed: about 10% a second at 0.1. So a real rifle bullet slows hard at first, losing roughly 30% of its speed over its first 500 m, while ours barely slow at the speeds where real ones slow most. And every bullet slows the same way, whatever its shape or weight.

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

What a real bullet does depends on its energy when it hits, which goes with the square of its speed, and on how the bullet is built. A rifle bullet still has roughly half its energy at 500 m, so the mod's fall to 10% is much steeper than reality.

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

## Ideas for the mod

The ones that would make the most difference come first:

1. **Drag that grows with speed**, worked out in `src/Core` from a ballistic coefficient for each weapon, instead of the rigidbody's drag.
2. **Damage from the speed it hits at**, instead of the distance it travelled.
3. **Sights zeroed at a set distance**, so the bullet rises to meet the line of sight.
4. **Real muzzle velocities**, or closer to them, once drag and zeroing keep long shots fair.
5. **A smaller, centre-weighted spread**, leaving most of the error to sway.
6. **Wind** from the game's own wind.
7. **Ricochets and penetration**, by surface and angle.
8. **The cold** slowing bullets.
9. **Turning the bullet to face along its path.**
