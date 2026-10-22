# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/2.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!--
A version's section is its GitHub release description, so write it for
players: one plain sentence each, about what changed for them.

- Add entries under "## [Unreleased]" as you go, grouped under Added,
  Changed, Deprecated, Removed, Fixed or Security.
- To release, rename it to "## [1.1.0] - YYYY-MM-DD" (no v), add a fresh
  "## [Unreleased]" above it, and update the links at the bottom.

See docs/development.md, "Releasing". CI checks this file's format.
-->

## [Unreleased]

### Added

- **Breaking:** The mod logs through [Complex Logger](https://github.com/Arkhorse/Complex-Logger), which it now needs, along with [ModSettings](https://github.com/DigitalzombieTLD/ModSettings). Turn on its debug levels when reporting a bug.
- The log warns when a weapon has no bullet trail or muzzle velocity, or fires the game's own shots instead of the mod's.
- Bullets knock loose items around when they hit them, as the game's own bullets do.
- A settings page in Options, Mod Settings, to turn real bullets, bullet trails and walking while aiming a revolver on or off, and to tune accuracy, bullet speed, trail length and damage drop-off.
- **Breaking:** The mod now needs [ModData](https://github.com/dommrogers/ModData/releases/latest), to keep what it remembers about each weapon in your save.
- Weapons can wear skins, which change how a gun looks in your hands, on the ground and in the inventory. Each gun keeps its own skin in your save. Skins go in `Mods/AdaptiveArsenal/Skins`.
- Rifles you carry freeze in the cold and thaw in the warm, and the more frozen a rifle is, the more likely it is to jam. The inventory shows how frozen each rifle is.
- **Breaking:** The HUD no longer shows how many rounds are loaded in your rifle or revolver, or how many more you're carrying, so you have to keep count. Turn on **Show ammo count** in the mod's settings to bring it back.

### Changed

- Each rifle and revolver does its own damage, as the game sets it, instead of every gun doing the same.
- Revolver hits have the game's own chance of a critical hit for revolvers, instead of the rifle's.
- Shots spread evenly in a circle around where you aim, instead of in a square that reached further out at the corners.
- The limited-movement icon still shows while aiming a rifle, and is only hidden for the revolver, which you can walk with.
- [How it works](https://github.com/Deaadman/adaptive-arsenal/blob/develop/docs/features.md) lists muzzle velocities in metres per second. Bullets fly at the same speeds as before.

### Removed

- The mod no longer needs Localization Utilities.

### Fixed

- Bullets still in the air when the game saves no longer turn into loose rounds of ammo when you load.
- Bullet holes appear where the bullet hit, instead of somewhere else on the object or not at all.
- Bullet impact sounds come from where the bullet hit, so they get quieter with distance.
- Bullet trails fade out after the bullet hits something, instead of vanishing at once.
- Bullets still fly at full speed if their trail can't be made.
- Bullets that hit nothing, like shots into the sky, are cleaned up after 10 seconds.
- Hip-fire spread is the same whichever way you face, instead of changing as you look up, down or around.
- Shooting an animal in a spot without body-part damage still hurts it, instead of the hit being lost to an error.

## [1.0.9] - 2026-01-05

### Changed

- Updated for The Long Dark 2.51.

## [1.0.8] - 2025-01-11

### Added

- The Trader's (Bunker) rifle has its own muzzle velocity, the highest of any rifle.

### Fixed

- Rifle and revolver hits count towards the right wildlife hits statistic.

## [1.0.7] - 2024-12-17

### Added

- The mod won't load alongside [Target Practice & Master Hunter](https://github.com/RomainDeschampsFR/TargetPracticeAndMasterHunter), which it doesn't work with.

### Changed

- Shots are a little more accurate.

### Fixed

- A cougar you shoot at swipes at you, rather than attacking straight away.

## [1.0.6] - 2024-12-12

### Changed

- Updated for The Long Dark 2.35.

### Fixed

- You can take photos with the camera again.

## [1.0.5] - 2024-07-30

### Added

- You can move while aiming any revolver.
- How accurate a shot is depends on your skill and on whether you're standing, crouching or moving. At skill level 5, rifles and revolvers are fully accurate.
- Firing a weapon plays an animation of the round on the HUD.

### Changed

- Simpler muzzle velocity and damage drop-off.
- Bullet trails cost less performance.

## [1.0.4] - 2024-06-29

### Changed

- Updated for The Long Dark 2.31.

### Fixed

- A jammed gun can't fire endless bullets.
- Bullets disappear as soon as they hit something, so they can't be picked back up.

## [1.0.3] - 2024-06-27

### Changed

- Bullets are faster, so they feel more like real bullets.
- Updated for The Long Dark 2.29.

### Removed

- Wind no longer changes where bullets go.

### Fixed

- Bullets leave from the right height when aiming.
- Revolver bullets no longer veer to the left when firing from the hip.

## [1.0.2] - 2024-02-08

### Fixed

- The mod runs much faster: well over twice as fast as 1.0.1.

## [1.0.1] - 2024-01-29

### Changed

- Bullets are slightly faster.

### Fixed

- The mod works with MelonLoader 0.6.1, not just 0.6.2.

## [1.0.0] - 2024-01-15

### Added

- The first release.

[Unreleased]: https://github.com/Deaadman/adaptive-arsenal/compare/1.0.9...HEAD
[1.0.9]: https://github.com/Deaadman/adaptive-arsenal/compare/1.0.8...1.0.9
[1.0.8]: https://github.com/Deaadman/adaptive-arsenal/compare/1.0.7...1.0.8
[1.0.7]: https://github.com/Deaadman/adaptive-arsenal/compare/1.0.6...1.0.7
[1.0.6]: https://github.com/Deaadman/adaptive-arsenal/compare/1.0.5...1.0.6
[1.0.5]: https://github.com/Deaadman/adaptive-arsenal/compare/1.0.4...1.0.5
[1.0.4]: https://github.com/Deaadman/adaptive-arsenal/compare/1.0.3...1.0.4
[1.0.3]: https://github.com/Deaadman/adaptive-arsenal/compare/1.0.2...1.0.3
[1.0.2]: https://github.com/Deaadman/adaptive-arsenal/compare/1.0.1...1.0.2
[1.0.1]: https://github.com/Deaadman/adaptive-arsenal/compare/1.0.0...1.0.1
[1.0.0]: https://github.com/Deaadman/adaptive-arsenal/releases/tag/1.0.0
