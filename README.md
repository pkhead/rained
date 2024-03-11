# Rained
![Fancy Logoe](rained-logo.png)

Another Rain World level editor. Currently in development.

## Features
- Ease of use (hopefully)
- Undo/redo everything
- [Drizzle](https://github.com/SlimeCubed/Drizzle/tree/community) level rendering with a preview
- Dark Mode for the Miros Birds

Some usability hints, as I don't yet know a better place to put this:
- In the effects editor, you can drag effect slots up and down in the active effects list.
- You can also delete effect slots by right-clicking on them.
- In the prop editor's freeform warp mode, the outline colors for each prop mean different things about the way they're moved/rotated/scaled:
    - Blue: The scale axis is determined by the prop's rotation - this will keep rectangles rectangles.
    - White: In contrast to blue, the scale axis is not determined by its rotation - it will be distorted if it's rotated.
    - Green: You can move its endpoints. This applies to all rope-type props, and, once added, long-type props.
- If you load in a rope-type prop that has been warped from another editor, you can't transform it until you press "Reset Transform"
- If you scale multiple props, it will force proportional scaling.

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
