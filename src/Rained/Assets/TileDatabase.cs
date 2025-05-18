using System.Numerics;
using Raylib_cs;

namespace Rained.Assets;

public enum TileType : byte
{
    VoxelStruct = 0,
    VoxelStructRockType = 1,
    VoxelStructRandomDisplaceVertical = 2,
    VoxelStructRandomDisplaceHorizontal = 3,
    Box = 4,
    VoxelStructSandType = 5,
    VoxelStructSeamlessHorizontal,
    VoxelStructSeamlessVertical
}

class Tile
{
    public readonly string Name;
    public readonly TileCategory Category;
    public readonly TileType Type;
    public readonly int Width;
    public readonly int Height;
    public readonly sbyte[,] Requirements;
    public readonly sbyte[,] Requirements2;
    public readonly bool HasSecondLayer;
    public readonly int BfTiles = 0;
    public readonly bool CanBeProp;

    /// <summary>
    /// The number of voxel slices in the image.
    /// </summary>
    public readonly int LayerCount;

    /// <summary>
    /// The depth of each voxel slice, interpreted from repeatL.
    /// </summary>
    public readonly int[] LayerDepths;

    /// <summary>
    /// The depth size of the tile. Either 10 or 20.
    /// </summary>
    public readonly int LayerDepth;

    public readonly int VariationCount;

    public readonly int CenterX;
    public readonly int CenterY;

    public readonly int ImageYOffset;
    public readonly int ImageRowCount;

    public readonly string[] Tags;

    // that's way too many parameters
    public Tile(
        string name,
        DrizzleConfiguration drizzleConfig, // needed for the setting that controls if certain tiles can be loaded as props
        TileCategory category,
        TileType type,
        int width, int height,
        int bfTiles,
        List<int>? repeatL,
        List<int> specs, List<int>? specs2,
        int rnd,
        string[] tags
    )
    {
        Name = name;
        Type = type;
        Width = width;
        Height = height;
        BfTiles = bfTiles;
        Category = category;
        CanBeProp = false;
        LayerCount = 1;
        VariationCount = rnd;
        Tags = tags;

        CenterX = (int)MathF.Ceiling((float)Width / 2) - 1;
        CenterY = (int)MathF.Ceiling((float)Height / 2) - 1;

        Requirements = new sbyte[width, height];
        Requirements2 = new sbyte[width, height];
        HasSecondLayer = false;
        LayerDepth = 10;

        // fill requirements table
        {
            int i = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Requirements[x,y] = (sbyte) specs[i];
                    Requirements2[x,y] = -1;
                    i++;
                }
            }

            if (specs2 is not null)
            {
                LayerDepth = 20;
                HasSecondLayer = true;
                
                i = 0;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Requirements2[x, y] = (sbyte) specs2[i];
                        i++;
                    }
                }
            }
        }

        // get parameters required for the retrieval of the Y location of preview image
        ImageRowCount = height + bfTiles * 2;
        ImageYOffset = 1;
        switch (type)
        {
            case TileType.VoxelStruct:
            case TileType.VoxelStructRandomDisplaceHorizontal:
            case TileType.VoxelStructRandomDisplaceVertical:
            case TileType.VoxelStructSeamlessHorizontal:
            case TileType.VoxelStructSeamlessVertical:
                LayerCount = repeatL!.Count;
                ImageRowCount *= LayerCount;

                // fill LayerDepths list
                LayerDepths = new int[LayerCount];
                int depth = 0;
                for (int i = 0; i < LayerCount; i++)
                {
                    LayerDepths[i] = depth;
                    depth += repeatL![i];
                }

                if (type == TileType.VoxelStruct || drizzleConfig.VoxelStructRandomDisplaceForTilesAsProps)
                    CanBeProp = true;

                break;
            
            case TileType.VoxelStructRockType:
            case TileType.VoxelStructSandType:
                LayerDepths = [0];
                break;
            
            case TileType.Box:
                ImageRowCount = height * width + height + bfTiles * 2;
                ImageYOffset = 0;
                LayerDepths = [0];
                break;
        }

        if (Tags.Contains("notProp"))
            CanBeProp = false;
    }
}

class TileCategory
{
    public string Name;
    public int Index;
    public Color Color;
    public List<Tile> Tiles = new();

    public TileCategory(string name, Lingo.Color color)
    {
        Name = name;
        Color = new Color(color.R, color.G, color.B, 255);
    }
}

class TileDatabase
{
    public readonly List<TileCategory> Categories;
    private readonly Dictionary<string, Tile> stringToTile = new();

    /// <summary>
    /// True if there were any errors while loading.
    /// </summary>
    public bool HasErrors { get; private set; } = false;

    public TileDatabase()
    {
        var drizzleConfig = DrizzleConfiguration.LoadConfiguration(Path.Combine(RainEd.Instance.AssetDataPath, "editorConfig.txt"));
        Categories = [];
        
        var lingoParser = new Lingo.LingoParser();

        TileCategory? curGroup = null;
        int groupIndex = 0;

        // helper function to create error string with line information
        static string ErrorString(int lineNo, string msg)
        {
            if (lineNo == -1)
                return "[EMBEDDED]: " + msg;
            else
                return "Line " + lineNo + ": " + msg;
        }

        void ProcessLine(string line, int lineNo)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            
            // read header
            if (line[0] == '-')
            {
                if (lingoParser.Read(line[1..]) is not Lingo.LinearList header)
                {
                    Log.UserLogger.Warning(ErrorString(lineNo, "Malformed category header, ignoring."));
                    return;
                }

                curGroup = new TileCategory((string) header[0], (Lingo.Color) header[1])
                {
                    Index = groupIndex
                };

                groupIndex++;
                Categories.Add(curGroup);
            }
            else
            {
                if (curGroup is null) throw new Exception(ErrorString(lineNo, "The first category header is missing"));

                var parsedLine = lingoParser.Read(line, out Lingo.ParseException? parseErr);
                if (parseErr is not null)
                {
                    HasErrors = true;
                    Log.UserLogger.Error(ErrorString(lineNo, parseErr.Message + " (line ignored)"));
                    return;
                }

                if (parsedLine is null)
                {
                    HasErrors = true;
                    Log.UserLogger.Error(ErrorString(lineNo, "Malformed tile init (line ignored)"));
                    return;
                }
                
                var tileInit = (Lingo.PropertyList) parsedLine;

                object? tempValue = null;
                var name = (string) tileInit["nm"];
                var tp = (string) tileInit["tp"];
                var size = (Vector2) tileInit["sz"];
                var specsData = (Lingo.LinearList) tileInit["specs"];
                var bfTiles = Lingo.LingoNumber.AsInt(tileInit["bfTiles"]);
                Lingo.LinearList? specs2Data = null;
                Lingo.LinearList? repeatLayerList =
                    tileInit.TryGetValue("repeatL", out tempValue) ? (Lingo.LinearList) tempValue : null;
                int rnd =
                    tileInit.TryGetValue("rnd", out tempValue) ? Lingo.LingoNumber.AsInt(tempValue) : 1;
                
                if (tileInit.TryGetValue("specs2", out tempValue) && tempValue is Lingo.LinearList specs2List)
                {
                    specs2Data = specs2List;
                }

                List<int>? repeatL = repeatLayerList?.Cast<int>().ToList();
                List<int> specs = specsData.Cast<int>().ToList();
                List<int>? specs2 = specs2Data?.Cast<int>().ToList();
                List<string> tags = ((Lingo.LinearList)tileInit["tags"]).Cast<string>().ToList();

                TileType tileType = tp switch
                {
                    "voxelStruct" => TileType.VoxelStruct,
                    "voxelStructRockType" => TileType.VoxelStructRockType,
                    "voxelStructSandType" => TileType.VoxelStructSandType,
                    "voxelStructRandomDisplaceHorizontal" => TileType.VoxelStructRandomDisplaceHorizontal,
                    "voxelStructRandomDisplaceVertical" => TileType.VoxelStructRandomDisplaceVertical,
                    "box" => TileType.Box,
                    "voxelStructSeamlessHorizontal" => TileType.VoxelStructSeamlessHorizontal,
                    "voxelStructSeamlessVertical" => TileType.VoxelStructSeamlessVertical,
                    _ => throw new Exception($"Invalid tile type '{tp}'")
                };

                try {
                    var tileData = new Tile(
                        name: name,
                        drizzleConfig: drizzleConfig,
                        category: curGroup,
                        type: tileType,
                        width: (int) size.X,
                        height: (int) size.Y,
                        bfTiles: bfTiles,
                        repeatL: repeatL,
                        specs: specs,
                        specs2: specs2,
                        rnd: rnd,
                        tags: [..tags]
                    );

                    curGroup.Tiles.Add(tileData);
                    stringToTile.TryAdd(name, tileData);
                }
                catch (Exception e)
                {
                    Log.UserLogger.Warning(ErrorString(lineNo, "Could not add tile {Name}: {ErrorMessage}"), name, e.Message);
                }
            }
        }

        // read Init.txt
        int lineNo = 1;
        foreach (var line in File.ReadLines(Path.Combine(RainEd.Instance.AssetDataPath, "Graphics", "Init.txt")))
        {
            ProcessLine(line, lineNo++);
        }

        // read internal extra tiles
        if (DrizzleCast.GetFileName("Drought Needed Init.txt", out string? initPath))
        {
            using var reader = File.OpenText(initPath);
            string? line2;
            while ((line2 = reader.ReadLine()) is not null)
            {
                ProcessLine(line2, -1);
            }
        }
        else
        {
            Log.UserLogger.Error("Could not read cast Drought Needed Init.txt");
        }

        // purge empty categories
        for (int i = Categories.Count - 1; i >= 0; i--)
        {
            if (Categories[i].Tiles.Count == 0)
            {
                Log.UserLogger.Warning("{Category} was empty", Categories[i].Name);
                Categories.RemoveAt(i);
            }
        }
    }

    public bool HasTile(string name) => stringToTile.ContainsKey(name);
    public Tile GetTileFromName(string name) => stringToTile[name];
}