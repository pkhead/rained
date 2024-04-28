# Rained
![Fancy Logoe](rained-logo.png)

Another Rain World level editor. Currently in development.

Please note that the "main" branch is ahead of the latest release. If you want to access the repository
at the time of a certain release, use the Git tags system.

## Features
- Ease of use (hopefully)
- Undo/redo everything
- [Drizzle](https://github.com/SlimeCubed/Drizzle/tree/community) level rendering with a preview
- Dark Mode for the Miros Birds

Read [this document](dist/README.md) for more information on how to use Rained.

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

## Contributing
Report bugs and other complaints by creating an issue or pinging @chromosoze in the Rain World Discord server. DM requests also work, but it's likely
that it'll take me a while to notice them as I don't pinged for it.

Pull requests are welcome.
