# Building from source

You need the [.NET SDK](https://dotnet.microsoft.com/download) 10 or newer, which CI uses too. The game's assemblies come from NuGet, so you don't need the game to build.

```powershell
dotnet build -c Release
dotnet test --project tests/AdaptiveArsenal.Tests
```

The mod is `src/bin/Release/net6.0/AdaptiveArsenal.dll`.

To copy it into the game after every Debug build, set `TheLongDarkPath` to the game's folder, as an environment variable or in `Directory.Build.props`:

```powershell
$env:TheLongDarkPath = 'C:\Program Files (x86)\Steam\steamapps\common\TheLongDark'
dotnet build
```

## How it's laid out

- `src/Core`: the numbers behind each shot (accuracy, spread, muzzle velocity, damage) and the trail and animation curves. It's plain C#, with nothing from Unity, the game or MelonLoader, so it can be [tested](#testing) without the game.
- `src/Components/ProjectileItem.cs`: the bullet: its flight, trail and hits, using `src/Core` for the numbers.
- `src/Patches`: the [Harmony](https://harmony.pardeike.net) patches on the game: firing bullets instead of instant hits, revolver movement, and the HUD.
- `src/Animators`: the round animation on the HUD.
- `src/Utilities`: helpers for the game's objects: where a shot starts, borrowing materials, logging.
- `src/Properties/AssemblyInfo.cs`: what MelonLoader reads about the mod, including the MelonLoader version it needs and the mods it won't load with.
- `tests/AdaptiveArsenal.Tests`: unit tests for `src/Core`.
- `docs`: the pages linked from the README. `assets` has the Photoshop files for the banner and thumbnail.

Every type and member has an XML comment. Keep it that way: the build checks them (see [CI](#ci)).

## Versions

The version is `VersionPrefix` in [`src/AdaptiveArsenal.csproj`](../src/AdaptiveArsenal.csproj). It's the next release's version, so a release changes nothing but the changelog.

Nightly builds add a suffix with the date and commit, like `1.1.0-nightly.20261022+g1a2b3c4`. The game's console shows the version when the mod loads, so a bug report says exactly which build it came from.

## Packages

[`src/packages.lock.json`](../src/packages.lock.json) and the test project's lock file pin every NuGet package, including the ones they depend on. After changing a package version, run `dotnet restore AdaptiveArsenal.slnx` and commit the lock file with it.

The first two numbers of the `STBlade.Modding.TLD.Il2CppAssemblies.Windows` version are the game version its assemblies come from. So a new version of it usually means a game update: build against it, then check the mod in the game.

## Branches

- `develop` is where work happens. Every push to it publishes a [nightly build](#nightly-builds).
- `main` has the newest release. Merge `develop` into it to release.

## CI

[CI](../.github/workflows/ci.yml) runs on Windows for every push to `main` and every pull request. It checks:

- that `CHANGELOG.md` follows the format below
- formatting, with `dotnet format whitespace --verify-no-changes`. Run `dotnet format whitespace AdaptiveArsenal.slnx` to fix it.
- that each `packages.lock.json` matches its project
- that no package has a security advisory: restoring checks them all
- that a Release build works with warnings as errors, and shows the DLL's version and size. That includes the XML comments: public types and members need one, and every `<see cref="..."/>` must point at something that exists.
- that the [unit tests](#testing) pass

## Testing

Most of the mod can only be tested in the game. Everything it touches, down to Unity's `Vector3` and `Mathf`, calls into the running game through Il2CppInterop, so it can't run in a test.

The exception is `src/Core`: the maths behind each shot, with nothing from Unity, the game or MelonLoader. [`tests/AdaptiveArsenal.Tests`](../tests/AdaptiveArsenal.Tests) tests it with [xUnit](https://xunit.net):

```powershell
dotnet test --project tests/AdaptiveArsenal.Tests
```

The test project compiles the files in `src/Core` itself rather than referencing the mod. So if something in `src/Core` uses a game type, the tests stop building, and it has to move out. When you add a rule with numbers in it, like a new chance or duration, put the maths in `src/Core` and test it there, and keep the game-facing part in `src/Components` or `src/Patches`.

### In the game

Before a release, and after a game update, load a save with a rifle, a revolver and ammo, and check:

- Rifle and revolver shots leave a trail and hit where you aim, with more spread from the hip, standing and moving.
- Shooting an animal hurts it, counts towards your Rifle or Revolver skill, and a close shot does more than a far one.
- Bullets hitting the ground, rock and wood show the right impact effect and sound.
- The HUD plays the round animation after each shot, but not when you switch weapons.
- You can walk while aiming the revolver.
- The flare gun, bow and camera work as they do without the mod.
- The console shows the mod's version when the game starts, with no errors in `MelonLoader\Latest.log`.

Once a week, [Dependabot](../.github/dependabot.yml) opens a pull request against `develop` for NuGet packages and actions with a newer version, with the minor and patch updates together. Merge it when CI passes.

## Nightly builds

The [nightly workflow](../.github/workflows/nightly.yml) runs on every push to `develop`. It runs CI, builds the mod with the nightly version, and replaces the [`nightly`](https://github.com/Deaadman/adaptive-arsenal/releases/tag/nightly) pre-release with it. Its notes are the `## [Unreleased]` section of `CHANGELOG.md`, so testers see what's new, then a link to every commit since the latest release. To preview them, run `.\scripts\release-notes.ps1 -Version Unreleased -OutDir notes`.

## Releasing

The [release workflow](../.github/workflows/release.yml) runs when a version tag is pushed:

1. On `develop`, in [`CHANGELOG.md`](../CHANGELOG.md), rename `## [Unreleased]` to the new version and date, for example `## [1.1.0] - 2026-11-02`. Add a new empty `## [Unreleased]` above it, and update the links at the bottom.
2. Check `VersionPrefix` in `src/AdaptiveArsenal.csproj` is the same version. Commit, and merge `develop` into `main`.
3. Tag the commit on `main` and push the tag: `git tag 1.1.0`, then `git push origin 1.1.0`. Tags don't start with `v`.
4. Set `VersionPrefix` on `develop` to the next version, so nightlies after the release are numbered after it.

The workflow then runs CI, checks the tag matches `VersionPrefix`, builds the mod and publishes the release with `AdaptiveArsenal.dll`. It stops if `CHANGELOG.md` has no section for that version.

### Writing the changelog

`CHANGELOG.md` follows [Keep a Changelog 2.0.0](https://keepachangelog.com/en/2.0.0/), but without a `v` in front of versions. CI checks the format and says what to fix.

Each version's section is the release's description on GitHub, so write for players:

- Add each change under `## [Unreleased]` as you make it.
- One plain sentence per change, about what's different in the game. "Bullet trails fade out after they hit", not "Fix LineRenderer fade in OnCollisionEnter".
- Leave out changes players can't see, like refactors and CI.
- Use full `https://` links. Links like `docs/features.md` break on the release page.

The format rules:

- Use only these headings, in this order, and skip empty ones: `### Added`, `### Changed`, `### Deprecated`, `### Removed`, `### Fixed`, `### Security`.
- Every line under a heading starts with `- `. A long entry can continue on the next line if it's indented.
- A version can start with a short summary before its first heading.
- Start a breaking change with `**Breaking:**`.
- Add ` [YANKED]` after the date of a pulled release. The workflow won't publish it.

The links at the bottom make each heading clickable. `[Unreleased]` compares the newest release with `HEAD`, each version compares with the one before, and the first links to its tag.

To preview a release's notes, run `.\scripts\release-notes.ps1 -Version 1.1.0 -OutDir notes`. It writes the GitHub description to `notes/release-notes.md`. Add `-Check` instead to only check the format, like CI does. The script is shared with [Abalone](https://github.com/Deaadman/abalone): change it there, not here.
