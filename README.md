# Omamori

[![Latest release](https://img.shields.io/github/v/release/omamori-labs/Omamori?include_prereleases&style=flat-square)](https://github.com/omamori-labs/Omamori/releases)
[![MIT Licensed](https://img.shields.io/badge/license-MIT-brightgreen.svg?style=flat-square)](LICENSE)
![ci](https://github.com/omamori-labs/Omamori/workflows/ci/badge.svg)

A modding framework for osu!lazer.

Omamori is in alpha. The API is likely to heavily change between releases.

## For players

### Install with Torii

[Torii](https://github.com/omamori-labs/Torii) installs Omamori and manages your mods and modded instances for you, on Windows and Linux.

Get it from [Torii's releases](https://github.com/omamori-labs/Torii/releases). It's in alpha too, so expect big changes between releases.

### Install Omamori by hand

1. Download `omamori-<version>.zip` from the [latest release](https://github.com/omamori-labs/Omamori/releases).
2. Extract the zip into osu!'s data folder, so that `rulesets` and `omamori` sit next to your `files` folder.
3. Start osu!. There is now an **Omamori** section in the settings.

The data folder is where osu! keeps your beatmaps and scores, not where the game is installed.
| System | Default data folder |
|---|---|
| Windows | `%APPDATA%\osu` |
| macOS | `~/Library/Application Support/osu` |
| Linux | `~/.local/share/osu` |

- **Updating:** extract the new zip over the old one.

- **Uninstalling:** delete `rulesets/osu.Game.Rulesets.Omamori.dll` and the `omamori` folder.

### Install mods

A mod is a zip with one folder in it. Extract it into `omamori/mods` inside the data folder and restart osu!:

```
osu/
  omamori/
    mods/
      yourname.coolmod/
        mod.json
        CoolMod.dll
```

"Open mods folder" in the Omamori settings opens that folder for you.

### Manage mods

Settings → Omamori → "Manage mods" lists every installed mod. From there you can:

- turn a mod off immediately, or back on from the next time osu! starts
- change a mod's settings and key bindings
- see why a mod didn't load, and try it again after a crash

## For mod authors

You need the .NET 10 SDK and osu!lazer installed.

```sh
dotnet new install Omamori.Templates::<version>
dotnet new omamori-mod -n CoolMod
cd CoolMod
dotnet build -p:OmamoriDeploy=true
```

- While Omamori is in alpha, give the template version explicitly. It's the same as the release version.    
- `OmamoriDeploy` copies the mod into your `omamori/mods` folder after each build, so restarting osu! picks it up.
- A Release build also makes `<id>-<version>.zip`, ready to share.

The template is a working mod with a setting, a key binding, something on the main menu, and a notification after each play. Shortened:

```csharp
public class Mod : IMod
{
    public void Load(IModContext context)
    {
        var text = context.Settings.Text("text", "Greeting", "hello");

        context.Ui.AddToMainMenu(() => new Greeting(text));

        context.Events.OnPlayEnded(result =>
        {
            if (result.Score is { } score)
                context.Notifications.Post($"finished with {score.Rank}");
        });
    }
}
```

Everything a mod can reach is on `IModContext`:

| | |
|---|---|
| `Events` | screens, beatmaps, plays, live hits, music, user, skin, library, overlays, multiplayer rooms |
| `Ui` | drawables on the main menu, song select, results, the gameplay HUD, or over everything; toolbar and song select footer buttons; beatmap and score right-click menu items; skin editor components |
| `Settings` | toggles, numbers, text, choices, colours, files and key bindings, shown in the mod manager and saved for you |
| `Input` | run code when a key binding is pressed or released |
| `Notifications` | post notifications, errors and progress |
| `GamePlays` | cancel, start late or unsubmit a play; pause, seek, restart or exit the running one; stop it failing |
| `GameMods` | add your own mods to mod select |
| `Navigation` | select or play a beatmap, open an overlay, follow a link |
| `Beatmaps` | the selected beatmap's objects, timing points, breaks and difficulty |
| `Audio` | play sounds from your mod's folder, duck the music, control the track outside gameplay |
| `Presence` | set the Discord status |
| `Services` | offer objects to other mods, or use theirs |
| `Log`, `DataDirectory` | a log prefixed with your mod, and a folder that survives updates |

When a player turns your mod off, Omamori calls `IMod.Unload()`, then removes everything you registered and your patches. Use `Unload()` to stop anything else you started, like threads or timers.

More:

- [`mod.json` schema](schemas/mod.schema.json), with descriptions of every field
- [Build options, depending on another mod, and patching osu! with Harmony](src/Omamori.ModBuildConfig/README.md)
- Sample mods: [Now Playing](samples/Omamori.NowPlaying) and [Skip Intro](samples/Omamori.SkipIntro)

## Building Omamori

You need the .NET 10 SDK. The build downloads its own copy of osu!lazer into `sandbox/lazer` and never touches your real install.

```sh
./build.ps1                                   # Windows: set up the sandbox, build, run the tests
./build.sh                                    # Linux and macOS
./build.ps1 --target=Deploy --mods samples    # install the build and the sample mods into the sandboxs
```

Then start `sandbox/lazer/osu!.exe` to try it.

| Target | What it does |
|---|---|
| `Test` (default) | builds everything and runs the tests |
| `CI` | tests, packs the NuGet packages, and builds a mod from the packed template |
| `Deploy` | installs the build into the sandbox, plus the mods under `--mods <dir>` |
| `Smoke` | deploys, starts the game, prints Omamori's log lines and closes it |

The osu!lazer version to build against is in [`build/lazer-version.txt`](build/lazer-version.txt).

## License

[MIT](LICENSE). Third-party libraries are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
