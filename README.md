# Rained
![Fancy Logoe](rained-logo.png)

Another Rain World level editor. Currently in development.

Please note that the "main" branch is ahead of the latest release. If you want to access the repository
at the time of a certain release, use the Git tags system.

## Features
- Ease of use (hopefully)
- Undo/redo everything
- Re-envisioned prop editor
- Tile graphics and palette previews
- [Drizzle](https://github.com/SlimeCubed/Drizzle/tree/community) level rendering with a preview
- Dark Mode for the Miros Birds

Read [this document](dist/README.md) for information on how to use Rained.

## Building
**NOTE**: Due to the Raylib-cs library, on Windows, Rained requires the Microsoft Visual Studio C++ runtime package to run. Thus, if Rained fails to open a window on launch, it's probably because you have it missing. The package can be installed [here](https://aka.ms/vs/17/release/vc_redist.x64.exe).

### .NET CLI
Clone with Git:
```bash
git clone --recursive https://github.com/pkhead/rained
cd rained
```

Set up Drizzle
```bash
cd src/Drizzle
dotnet run --project Drizzle.Transpiler
```

Back to the root directory, build and run Rained
```bash
dotnet build
dotnet run --project src/Rained/Rained.csproj
```
Upon first startup, you can configure where your Data folder is located. If you chose to download and install it, Rained will download and extract [this repository](https://github.com/SlimeCubed/Drizzle.Data/tree/community).

## Contributing
Report bugs and other complaints by creating an issue or pinging @chromosoze in the Rain World Discord server. DM requests also work, but it's likely
that it'll take me a while to notice them as I don't pinged for it.

Pull requests are welcome.

### The "nightly" tag
The "nightly" tag really only exists so that I'm able to create nightly GitHub releases. It's a bit annoying. I wouldn't recommend interacting with it.

Since the action deletes and re-creates the "nightly" tag on every release, in order to update the tag
on your clone (not that you would want to, I suppose), you would have to run the following Git commands:
```bash
git tag -d nightly # delete the nightly tag on your clone
git fetch origin tag nightly # fetch the nightly tag from origin
# running `git fetch` or `git pull` itself after deleting the tag should also work.
```