# Rained
![Fancy Logoe](rained-logo.png)

Another Rain World level editor. Currently in development.

Includes an integrated [Drizzle](https://github.com/SlimeCubed/Drizzle/tree/community) for level rendering.

## Building
### .NET CLI
Clone with Git:
```bash
git clone --recursive https://github.com/pkhead/rained
cd rained
```

Set up Drizzle
```bash
cd Drizzle
dotnet run --project Drizzle.Transpiler
```

Back to the root directory, build and run Rained
```bash
dotnet build
dotnet run --project src/Rained/Rained.csproj
```

## Contributing
Pull requests are welcome.