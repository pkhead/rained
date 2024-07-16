# Rained
![Fancy Logoe](rained-logo.png)

Another Rain World level editor.

Please note that the "main" branch is ahead of the latest release. If you want to access the repository
at the time of a certain release, use the Git tags system.

Don't hestiate to report any bugs, complaints, and suggestions by creating an issue on GitHub, or pinging @chromosoze in either the main Rain World Discord server or one of both Rain World modding servers. I also accept DMs, but as I don't get pinged for DM requests, it would take a while for me to respond.

**NOTE**: On Windows, Rained requires the Microsoft Visual Studio C++ runtime package to run. Thus, if Rained fails to open a window on launch, it's probably because you have it missing. The package can be installed [here](https://aka.ms/vs/17/release/vc_redist.x64.exe).

## Features
- Ease of use (hopefully)
- Undo/redo everything
- Re-envisioned prop editor
- Highly customizable UI
- Asset graphics and palette previews
- [Drizzle](https://github.com/SlimeCubed/Drizzle/tree/community) level rendering with a preview

Read [this document](dist/README.md) for information on how to use Rained.

## Building
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

## Shaders
Shader compilation requires the shaderc tool from the [bgfx](https://github.com/bkaradzic/bgfx) library. You will either need it on your PATH
or you will need an environment variable named BGFX_SHADERC set to the shaderc executable.

Once you have it set up, run the following commands to build shaders:
```bash
dotnet tool restore # run on first time
dotnet cake --target="Build Shaders"
```

This process isn't required to build Rained, as the shaderc build output is saved in the repository. However, this process is required if you want to modify the shader code.

## Contributing
Report bugs and other complaints by creating an issue or pinging @chromosoze on a Rain World modding Discord server. DM requests also work, but it's likely that it'll take me a while to notice them as I don't pinged for it.

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