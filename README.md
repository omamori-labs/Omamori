# Omamori

A modding framework for osu!lazer. It loads mods into the game, gives them a stable API to build on, and turns off a mod that crashes instead of letting it take osu! down.

Omamori is in alpha. The API can still change between releases, and each release is built for one osu!lazer version. On a different version it still starts, warns you, and turns off only the features that no longer fit.

## For players

### Install Omamori

1. Download `omamori-<version>.zip` from the [latest release](https://github.com/omamori-labs/Omamori/releases).
2. Close osu!.
3. Extract the zip into osu!'s data folder, so that `rulesets` and `omamori` sit next to your `files` folder.
4. Start osu!. There is now an **Omamori** section in the settings.

The data folder is where osu! keeps your beatmaps and scores, not where the game is installed. In osu!, Settings → General → "Open osu! folder" takes you there. By default it is:

| System | Data folder |
|---|---|
| Windows | `%APPDATA%\osu` |
| macOS | `~/Library/Application Support/osu` |
| Linux | `~/.local/share/osu` |

To update, extract the new zip over the old one. To uninstall, delete `rulesets/osu.Game.Rulesets.Omamori.dll` and the `omamori` folder.

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

- turn a mod on or off (it applies the next time osu! starts)
- change a mod's settings and key bindings
- see why a mod didn't load, and try it again after a crash

### What mods can and can't do

- Mods get read-only information about the game: the current beatmap, plays, scores, and so on. They can add their own interface, settings, key bindings and notifications.
- Mods that change gameplay are not allowed.
- A mod marked **Patches osu!** changes the game's own code. If osu! acts up after an update, try turning those off first.
- A mod that crashes is turned off for the session and osu! keeps running. If it crashes two sessions in a row, it stays off until it updates or you try it again.
- Crashes caused by Omamori or a mod are not reported to the osu! team.

## For mod authors

You need the .NET 10 SDK and osu!lazer installed.

```sh
dotnet new install Omamori.Templates::<version>
dotnet new omamori-mod -n CoolMod
cd CoolMod
dotnet build -p:OmamoriDeploy=true
```

While Omamori is in alpha, give the template version explicitly; it is the same as the release version.

`OmamoriDeploy` copies the mod into your `omamori/mods` folder after each build, so restarting osu! picks it up. A Release build also makes `<id>-<version>.zip`, ready to share.

The template is a working mod: a setting, a key binding, something on the main menu, and a notification after each play. Shortened, it looks like this:

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
| `Ui` | add drawables to the main menu, song select, results, the gameplay HUD, or over everything |
| `Settings` | toggles, numbers, text, choices, colours, files and key bindings, shown in the mod manager and saved for you |
| `Input` | run code when a key binding is pressed or released |
| `Notifications` | post notifications, errors and progress |
| `Presence` | set the Discord status |
| `Services` | offer objects to other mods, or use theirs |
| `Log`, `DataDirectory` | a log prefixed with your mod, and a folder that survives updates |

More:

- [`mod.json` schema](schemas/mod.schema.json), with descriptions of every field
- [Build options, depending on another mod, and patching osu! with Harmony](src/Omamori.ModBuildConfig/README.md)
- Sample mods: [Now Playing](samples/Omamori.NowPlaying) and [Skip Intro](samples/Omamori.SkipIntro)

## Building Omamori

You need the .NET 10 SDK. The build downloads its own copy of osu!lazer into `sandbox/lazer`, so it never touches your real install.

```sh
./build.ps1                                   # Windows: set up the sandbox, build, run the tests
./build.sh                                    # Linux and macOS
./build.ps1 --target=Deploy --mods samples    # install the build and the sample mods into the sandbox
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
