# Rained
![Fancy Logoe](rained-logo.png)

Another Rain World level editor. Currently in development.

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

## Contributing
Report bugs and other complaints by creating an issue or pinging @chromosoze in the Rain World Discord server.

Pull requests are welcome.
