namespace RainEd.Autotile;
using System.Numerics;

enum AutotileType
{
    Path, Rect
}

struct PathSegment(int x, int y)
{
    public bool Left = false;
    public bool Right = false;
    public bool Up = false;
    public bool Down = false;
    public int X = x;
    public int Y = y;
}

record ConfigOption
{
    public readonly string ID;
    public readonly string Name;
    public bool Value;

    public ConfigOption(string id, string name, bool defaultValue)
    {
        ID = id;
        Name = name;
        Value = defaultValue;
    }
}

abstract class Autotile
{
    public bool IsReady = true;

    public string Name;
    public int PathThickness;
    public int SegmentLength;
    public AutotileType Type;

    public Dictionary<string, ConfigOption> Options = [];

    public Autotile()
    {
        Name = "(unnamed)";
        Type = AutotileType.Rect;
        PathThickness = 1;
        SegmentLength = 1;
    }

    public Autotile(string name) : this()
    {
        Name = name;
    }

    public void AddOption(string id, string name, bool defaultValue)
    {
        Options.Add(id, new ConfigOption(id, name, defaultValue));
    }

    public bool TryGetOption(string id, out ConfigOption? data)
    {
        return Options.TryGetValue(id, out data);
        /*if (Options.TryGetValue(id, out ConfigOption? data))
        {
            return data.Value;
        }
        else
        {
            throw new LuaException($"option '{id}' does not exist");
        }*/
    }

    /// <summary>
    /// Run the rectangle autotiler.
    /// </summary>
    /// <param name="layer">The layer to autotile.</param>
    /// <param name="rectMin">The position of the top-left corner of the rectangle.</param>
    /// <param name="rectMax">The position of the bottom-right corner of the rectangle.</param>
    /// <param name="force">If the autotiler should force-place</param>
    /// <param name="geometry">If the autotiler should place geometry.</param>
    public abstract void TileRect(int layer, Vector2i rectMin, Vector2i rectMax, bool force, bool geometry);

    /// <summary>
    /// Run the path autotiler.
    /// </summary>
    /// <param name="layer">The layer to autotile.</param>
    /// <param name="pathSegments">An array of path segments.</param>
    /// <param name="force">If the autotiler should force-place.</param>
    /// <param name="geometry">If the autotiler should place geometry.</param>
    public abstract void TilePath(int layer, PathSegment[] pathSegments, bool force, bool geometry);

    public abstract string[] MissingTiles { get; }
}

class AutotileCatalog
{
    public readonly List<string> AutotileCategories = ["Misc"];
    private readonly List<List<Autotile>> Autotiles = [[]];

    /// <summary>
    /// Adds the given autotile to the catalog.
    /// </summary>
    /// <param name="autotile">The autotile to add.</param>
    /// <param name="category">The category to add it in.</param>
    public void AddAutotile(Autotile autotile, string category = "Misc")
    {   
        var catIndex = AutotileCategories.IndexOf(category);
        if (catIndex == -1)
        {
            catIndex = AutotileCategories.Count;
            AutotileCategories.Add(category);
            Autotiles.Add([]);
        }

        Autotiles[catIndex].Add(autotile);
    }

    public List<Autotile> GetAutotilesInCategory(string category)
        => Autotiles[AutotileCategories.IndexOf(category)];

    public List<Autotile> GetAutotilesInCategory(int index)
        => Autotiles[index];

}